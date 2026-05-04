using ClosedXML.Excel;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace _3K.Infrastructure.Services
{
    public class CekiService : ICekiService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
        private readonly IHareketService _hareketService;

        public CekiService(IUnitOfWork unitOfWork, AppDbContext context, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _hareketService = hareketService;
        }

        public async Task<Ceki> CekiYukleAsync(Stream excelDosya, string dosyaAdi)
        {
            // 1. Stream'i MemoryStream'e kopyala
            using var memoryStream = new MemoryStream();
            await excelDosya.CopyToAsync(memoryStream);
            var dosyaBytes = memoryStream.ToArray();

            Proje? proje = null;
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            string dosyaYolu = string.Empty;
            Ceki? ceki = null;

            // Dosyayı hafızaya sadece 1 KERE alıyoruz
            // ClosedXML resim adlarında özel karakter kabul etmiyor — önce temizle
            var cleanedBytes = SanitizePictureNames(dosyaBytes);
            using (var excelStream = new MemoryStream(cleanedBytes))
            using (var workbook = new XLWorkbook(excelStream))
            {
                string NormalizeName(string name)
                {
                    if (string.IsNullOrWhiteSpace(name)) return "";
                    return name.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"))
                        .Replace("Ç", "C").Replace("Ş", "S").Replace("İ", "I").Replace("Ö", "O").Replace("Ü", "U").Replace("Ğ", "G");
                }

                IXLWorksheet? worksheet = null;
                var allSheets = workbook.Worksheets.ToList();

                // Çıktı sayfasını bul
                foreach (var ws in allSheets)
                {
                    if (NormalizeName(ws.Name) == "CIKTI SAYFASI")
                    {
                        worksheet = ws;
                        break;
                    }
                }

                if (worksheet == null)
                {
                    foreach (var ws in allSheets)
                    {
                        var nName = NormalizeName(ws.Name);
                        if (nName.Contains("CIKTI") && !nName.Contains("YDK") && !nName.Contains("YEDEK"))
                        {
                            worksheet = ws;
                            break;
                        }
                    }
                }

                if (worksheet == null)
                    throw new Exception("Excel dosyasında 'ÇIKTI SAYFASI' bulunamadı!");

                // --- 2. BAŞLIK BİLGİLERİNİ OKUMA ---
                string fbNo = string.Empty;
                for (int r = 1; r <= 10; r++)
                {
                    for (int c = 1; c <= 8; c++)
                    {
                        var val = worksheet.Cell(r, c).GetString().ToUpper().Trim();
                        if (val.Contains("FB NO") || val.Contains("SERIAL NO"))
                        {
                            fbNo = worksheet.Cell(r, c + 1).GetString().Trim();
                            if (string.IsNullOrWhiteSpace(fbNo))
                                fbNo = worksheet.Cell(r, c + 2).GetString().Trim();
                            break;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(fbNo)) break;
                }

                if (string.IsNullOrWhiteSpace(fbNo))
                    throw new Exception("Excel şablonunda FB NO (Proje Adı) bulunamadı.");

                proje = await _context.Projeler.FirstOrDefaultAsync(p => p.FBNo == fbNo || p.ProjeNo == fbNo) as Proje;

                if (proje == null)
                {
                    proje = new Proje { ProjeNo = fbNo, FBNo = fbNo, DurumId = (int)ProjeDurum.Hazirlaniyor };
                    await projeRepo.AddAsync(proje);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    // Proje bulundu, çeki tekrar yüklemeyi engelle
                    var cekiVarMi = await _context.Cekiler.AnyAsync(c => c.ProjeId == proje.Id);
                    if (cekiVarMi)
                        throw new Exception($"Bu projeye ({proje.ProjeNo}) ait çeki listesi daha önce yüklenmiş! Aynı proje iki kez yüklenemez.");
                }

                var musteri = worksheet.Cell(2, 6).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(musteri)) proje.Musteri = musteri;

                var lokasyon = worksheet.Cell(4, 6).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(lokasyon)) proje.Lokasyon = lokasyon;

                for (int c = 10; c <= 16; c++)
                {
                    var val = worksheet.Cell(4, c).GetString().Trim();
                    if (val.StartsWith("T0") || val.StartsWith("T1")) { proje.OlcuResmiNo = val; break; }
                }
                for (int c = 10; c <= 16; c++)
                {
                    var val = worksheet.Cell(3, c).GetString().Trim();
                    if (val.StartsWith("T0") || val.StartsWith("T1")) { proje.NakilOlcuResmiNo = val; break; }
                }
                for (int c = 10; c <= 16; c++)
                {
                    var val = worksheet.Cell(2, c).GetString().Trim();
                    if (val.StartsWith("T0") || val.StartsWith("T1")) { proje.SonMontajResmiNo = val; break; }
                }

                projeRepo.Update(proje);
                await _unitOfWork.SaveChangesAsync();

                // --- 3. DOSYAYI KAYDETME ---
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", proje.Id.ToString());
                Directory.CreateDirectory(uploadsDir);
                dosyaYolu = Path.Combine(uploadsDir, dosyaAdi);
                await File.WriteAllBytesAsync(dosyaYolu, dosyaBytes);

                // --- 4. ÇEKİ KAYDI ---
                var cekiRepo = _unitOfWork.GetRepository<Ceki>();
                ceki = new Ceki { ProjeId = proje.Id, OrijinalDosyaYolu = dosyaYolu };
                await cekiRepo.AddAsync(ceki);
                await _unitOfWork.SaveChangesAsync();

                // --- 5. SATIRLARI OKUMA (MİLYON SATIR KORUMALI) ---
                var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
                var satirlar = new List<CekiSatiri>();

                // Sandık ismi eşleştirmesi: koliNo → sandıkIsmi
                var sandikIsimleri = new Dictionary<string, string>();

                int baslangicSatir = 6;
                for (int r = 1; r <= 20; r++)
                {
                    var cellVal = worksheet.Cell(r, 1).GetString().Trim();
                    if (int.TryParse(cellVal, out _))
                    {
                        baslangicSatir = r;
                        break;
                    }
                }

                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? baslangicSatir;

                int ardisikBosSatirSayisi = 0;
                const int MAX_BOS_SATIR_TOLERANSI = 15; // 15 boş satırda okumayı kes

                for (int r = baslangicSatir; r <= lastRow; r++)
                {
                    var row = worksheet.Row(r);

                    if (row.IsEmpty())
                    {
                        ardisikBosSatirSayisi++;
                        if (ardisikBosSatirSayisi > MAX_BOS_SATIR_TOLERANSI) break;
                        continue;
                    }

                    var barkod = row.Cell(3).GetString().Trim();
                    var tanim = row.Cell(4).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(tanim) && string.IsNullOrWhiteSpace(barkod))
                    {
                        ardisikBosSatirSayisi++;
                        if (ardisikBosSatirSayisi > MAX_BOS_SATIR_TOLERANSI) break;
                        continue;
                    }

                    ardisikBosSatirSayisi = 0;

                    int siraNo = satirlar.Count + 1;
                    var siraNoStr = row.Cell(1).GetString().Trim();
                    if (int.TryParse(siraNoStr, out int parsedSira)) siraNo = parsedSira;

                    var koliNo = row.Cell(5).GetString().Trim();

                    int istenenAdet = 0;
                    try
                    {
                        var miktarStr = row.Cell(6).GetString().Trim();
                        if (!string.IsNullOrWhiteSpace(miktarStr) && double.TryParse(miktarStr, out double m))
                            istenenAdet = (int)m;
                    }
                    catch { }

                    var birim = row.Cell(7).GetString().Trim();
                    int birimId = ParseBirimToId(birim);

                    var remarks = row.Cell(13).GetString().Trim();

                    // SANDIK İSMİ — son anlamlı sütundan oku (sütun 14+)
                    var sandikIsmi = string.Empty;
                    for (int c = 14; c <= 20; c++)
                    {
                        var hdr = worksheet.Cell(baslangicSatir - 1, c).GetString().Trim().ToUpper();
                        if (hdr.Contains("SANDIK") && (hdr.Contains("İSMİ") || hdr.Contains("ISMI") || hdr.Contains("DESCRIPTION")))
                        {
                            sandikIsmi = row.Cell(c).GetString().Trim();
                            break;
                        }
                        // Başlığı bulamazsak son sütunu kontrol et
                        var val = row.Cell(c).GetString().Trim();
                        if (!string.IsNullOrWhiteSpace(val) && c >= 14)
                        {
                            sandikIsmi = val;
                        }
                    }

                    // Sandık ismi eşleştirmesi — ilk görülen ismi kaydet
                    if (!string.IsNullOrWhiteSpace(koliNo) && !string.IsNullOrWhiteSpace(sandikIsmi))
                    {
                        if (!sandikIsimleri.ContainsKey(koliNo))
                            sandikIsimleri[koliNo] = sandikIsmi;
                    }

                    var satir = new CekiSatiri
                    {
                        CekiId = ceki.Id,
                        SiraNo = siraNo,
                        BarkodNo = barkod,
                        Aciklama = tanim,
                        CekideGecenSandikNo = koliNo,
                        IstenenAdet = istenenAdet,
                        BirimId = birimId,
                        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks,
                        DurumId = (int)UrunDurum.Bekliyor,
                        // Madde 4: İlk yükleme durumu — Grid durumu "Gelmedi" olarak başlar
                        GridDurumuId = (int)GridDurum.Gelmedi,
                        FiiliSandikNo = koliNo
                    };
                    satirlar.Add(satir);
                    await cekiSatiriRepo.AddAsync(satir);
                }

                await _unitOfWork.SaveChangesAsync();

                // --- 6. SANDIK GRUPLAMA VE OLUŞTURMA ---
                var sandikRepo = _unitOfWork.GetRepository<Sandik>();
                var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
                var benzersizSandiklar = satirlar.GroupBy(s => s.CekideGecenSandikNo);

                foreach (var grup in benzersizSandiklar)
                {
                    if (string.IsNullOrWhiteSpace(grup.Key)) continue;

                    var sandik = new Sandik
                    {
                        ProjeId = proje.Id,
                        SandikNo = grup.Key,
                        Ad = sandikIsimleri.TryGetValue(grup.Key, out var isim) ? isim : null,
                        DurumId = (int)SandikDurum.Hazirlaniyor
                    };
                    await sandikRepo.AddAsync(sandik);
                    await _unitOfWork.SaveChangesAsync();

                    foreach (var satir in grup)
                    {
                        var icerik = new SandikIcerik
                        {
                            SandikId = sandik.Id,
                            CekiSatiriId = satir.Id,
                            KonulanAdet = 0,
                            EksikAdet = 0
                        };
                        await sandikIcerikRepo.AddAsync(icerik);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }

            return ceki;
        }

        public async Task<IEnumerable<CekiSatiri>> GetCekiSatirlariAsync(int cekiId)
        {
            return await _context.CekiSatirlari
                .Include(cs => cs.Paketleyen)
                .Include(cs => cs.KontrolEden)
                .Include(cs => cs.SandikIcerikleri)
                .Where(cs => cs.CekiId == cekiId)
                .OrderBy(cs => cs.SiraNo)
                .ToListAsync();
        }

        public async Task<Ceki?> GetCekiByIdAsync(int cekiId)
        {
            return await _context.Cekiler
                .Include(c => c.CekiSatirlari)
                .FirstOrDefaultAsync(c => c.Id == cekiId);
        }

        public async Task<IEnumerable<Ceki>> GetProjeCekileriAsync(int projeId)
        {
            return await _context.Cekiler
                .Where(c => c.ProjeId == projeId)
                .OrderByDescending(c => c.YuklemeTarihi)
                .ToListAsync();
        }

        /// <summary>
        /// ClosedXML resim adlarında :\/?*[] karakterlerini kabul etmiyor.
        /// OpenXML ile resim adlarını temizler, sorunsuz yükleme sağlar.
        /// </summary>
        private static byte[] SanitizePictureNames(byte[] fileBytes)
        {
            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };

            using var stream = new MemoryStream();
            stream.Write(fileBytes, 0, fileBytes.Length);
            stream.Position = 0;

            try
            {
                using var doc = SpreadsheetDocument.Open(stream, true);
                var workbookPart = doc.WorkbookPart;
                if (workbookPart == null) return fileBytes;

                foreach (var wsPart in workbookPart.WorksheetParts)
                {
                    var drawingsPart = wsPart.DrawingsPart;
                    if (drawingsPart == null) continue;

                    var wsDr = drawingsPart.WorksheetDrawing;
                    if (wsDr == null) continue;

                    bool changed = false;
                    foreach (var anchor in wsDr.Descendants<TwoCellAnchor>())
                    {
                        var pic = anchor.Descendants<DocumentFormat.OpenXml.Drawing.Spreadsheet.Picture>().FirstOrDefault();
                        if (pic?.NonVisualPictureProperties?.NonVisualDrawingProperties != null)
                        {
                            var nvProps = pic.NonVisualPictureProperties.NonVisualDrawingProperties;
                            var name = nvProps.Name?.Value;
                            if (!string.IsNullOrEmpty(name) && name.IndexOfAny(invalidChars) >= 0)
                            {
                                foreach (var ch in invalidChars)
                                    name = name.Replace(ch, '_');
                                nvProps.Name = name;
                                changed = true;
                            }
                        }
                    }

                    // OneCellAnchor'daki resimleri de temizle
                    foreach (var anchor in wsDr.Descendants<OneCellAnchor>())
                    {
                        var pic = anchor.Descendants<DocumentFormat.OpenXml.Drawing.Spreadsheet.Picture>().FirstOrDefault();
                        if (pic?.NonVisualPictureProperties?.NonVisualDrawingProperties != null)
                        {
                            var nvProps = pic.NonVisualPictureProperties.NonVisualDrawingProperties;
                            var name = nvProps.Name?.Value;
                            if (!string.IsNullOrEmpty(name) && name.IndexOfAny(invalidChars) >= 0)
                            {
                                foreach (var ch in invalidChars)
                                    name = name.Replace(ch, '_');
                                nvProps.Name = name;
                                changed = true;
                            }
                        }
                    }

                    if (changed) wsDr.Save();
                }

                doc.Save();
                stream.Position = 0;
                return stream.ToArray();
            }
            catch
            {
                // OpenXML ile açılamazsa orijinal byte'ları dön
                return fileBytes;
            }
        }

        /// <summary>
        /// Excel'deki serbest metin birim değerini Birim Enum Id'sine çevirir.
        /// </summary>
        private static int ParseBirimToId(string birimText)
        {
            if (string.IsNullOrWhiteSpace(birimText))
                return (int)Birim.Adet;

            var normalized = birimText.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"));
            return normalized switch
            {
                "ADET" or "AD" or "PCS" or "PÇ" or "PC" => (int)Birim.Adet,
                "SET" or "ST" => (int)Birim.Set,
                "METRE" or "MT" or "M" => (int)Birim.Metre,
                "KG" or "KILOGRAM" => (int)Birim.Kg,
                "LT" or "LITRE" or "LİTRE" => (int)Birim.Litre,
                "TAKIM" or "TK" or "TKM" => (int)Birim.Takim,
                "PAKET" or "PKT" or "PK" => (int)Birim.Paket,
                "TON" or "TN" => (int)Birim.Ton,
                "M2" or "METREKARE" or "M²" => (int)Birim.Metrekare,
                "M3" or "METREKÜP" or "METREKUP" or "M³" => (int)Birim.Metrekup,
                _ => (int)Birim.Adet
            };
        }
    }
}