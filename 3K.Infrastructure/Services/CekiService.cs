using ClosedXML.Excel;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using System.Globalization;
using System.Text;

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
                    if (NormalizeExcelText(ws.Name) == "CIKTI SAYFASI" || NormalizeName(ws.Name) == "CIKTI SAYFASI")
                    {
                        worksheet = ws;
                        break;
                    }
                }

                if (worksheet == null)
                {
                    foreach (var ws in allSheets)
                    {
                        var nName = NormalizeExcelText(ws.Name);
                        var legacyName = NormalizeName(ws.Name);
                        if ((nName.Contains("CIKTI") || legacyName.Contains("CIKTI")) && !nName.Contains("YDK") && !nName.Contains("YEDEK"))
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
                var sandikBilgileri = OkuCekiListesiSandikBilgileri(workbook);

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

                    var koliNo = NormalizeKoliNo(row.Cell(5).GetString().Trim());

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
                var benzersizSandiklar = satirlar.GroupBy(s => NormalizeKoliNo(s.CekideGecenSandikNo));

                foreach (var grup in benzersizSandiklar)
                {
                    if (string.IsNullOrWhiteSpace(grup.Key)) continue;

                    sandikBilgileri.TryGetValue(grup.Key, out var sandikBilgisi);

                    var sandik = new Sandik
                    {
                        ProjeId = proje.Id,
                        SandikNo = grup.Key,
                        Ad = sandikBilgisi?.SandikIsmi ?? (sandikIsimleri.TryGetValue(grup.Key, out var isim) ? isim : null),
                        DurumId = (int)SandikDurum.Hazirlaniyor,
                        En = sandikBilgisi?.En,
                        Boy = sandikBilgisi?.Boy,
                        Yukseklik = sandikBilgisi?.Yukseklik,
                        NetKg = sandikBilgisi?.NetKg,
                        GrossKg = sandikBilgisi?.GrossKg
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

        public async Task<Ceki> CekiManuelOlusturAsync(ManuelCekiOlusturModel model)
        {
            var projeNo = model.ProjeNo?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(projeNo))
                throw new Exception("Proje no zorunludur.");

            if (model.Satirlar == null || model.Satirlar.Count == 0)
                throw new Exception("En az bir ürün satırı girilmelidir.");

            model.Sandiklar ??= new List<ManuelSandikModel>();
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var fbNo = string.IsNullOrWhiteSpace(model.FBNo) ? projeNo : model.FBNo.Trim();
            var proje = await _context.Projeler.FirstOrDefaultAsync(p => p.FBNo == fbNo || p.ProjeNo == projeNo);

            if (proje == null)
            {
                proje = new Proje
                {
                    ProjeNo = projeNo,
                    FBNo = fbNo,
                    DurumId = (int)ProjeDurum.Hazirlaniyor,
                    ProjeTipiId = model.ProjeTipiId > 0 ? model.ProjeTipiId : (int)ProjeTipi.Normal
                };
                await projeRepo.AddAsync(proje);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var cekiVarMi = await _context.Cekiler.AnyAsync(c => c.ProjeId == proje.Id);
                if (cekiVarMi)
                    throw new Exception($"Bu projeye ({proje.ProjeNo}) ait çeki listesi daha önce oluşturulmuş! Aynı proje iki kez oluşturulamaz.");
            }

            ApplyManuelProjeBilgileri(proje, model);
            projeRepo.Update(proje);
            await _unitOfWork.SaveChangesAsync();

            var ceki = new Ceki
            {
                ProjeId = proje.Id,
                OrijinalDosyaYolu = string.Empty
            };
            await cekiRepo.AddAsync(ceki);
            await _unitOfWork.SaveChangesAsync();

            var satirlar = new List<CekiSatiri>();
            for (var i = 0; i < model.Satirlar.Count; i++)
            {
                var item = model.Satirlar[i];
                var sandikNo = NormalizeKoliNo(item.SandikNo);
                if (string.IsNullOrWhiteSpace(sandikNo))
                    throw new Exception($"{i + 1}. ürün satırında sandık no zorunludur.");

                if (string.IsNullOrWhiteSpace(item.Aciklama))
                    throw new Exception($"{i + 1}. ürün satırında açıklama zorunludur.");

                if (item.IstenenAdet <= 0)
                    throw new Exception($"{i + 1}. ürün satırında miktar 0'dan büyük olmalıdır.");

                var satir = new CekiSatiri
                {
                    CekiId = ceki.Id,
                    SiraNo = item.SiraNo.GetValueOrDefault(i + 1),
                    BarkodNo = item.BarkodNo?.Trim() ?? string.Empty,
                    Aciklama = item.Aciklama.Trim(),
                    CekideGecenSandikNo = sandikNo,
                    FiiliSandikNo = sandikNo,
                    IstenenAdet = item.IstenenAdet,
                    BirimId = item.BirimId.GetValueOrDefault(ParseBirimToId(item.Birim ?? string.Empty)),
                    Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim(),
                    DurumId = (int)UrunDurum.Bekliyor,
                    GridDurumuId = (int)GridDurum.Gelmedi,
                    GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi
                };

                satirlar.Add(satir);
                await cekiSatiriRepo.AddAsync(satir);
            }
            await _unitOfWork.SaveChangesAsync();

            var sandikBilgileri = model.Sandiklar
                .Where(s => !string.IsNullOrWhiteSpace(s.SandikNo))
                .GroupBy(s => NormalizeKoliNo(s.SandikNo))
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var grup in satirlar.GroupBy(s => NormalizeKoliNo(s.CekideGecenSandikNo)))
            {
                if (string.IsNullOrWhiteSpace(grup.Key)) continue;

                sandikBilgileri.TryGetValue(grup.Key, out var sandikBilgisi);
                var sandik = new Sandik
                {
                    ProjeId = proje.Id,
                    SandikNo = grup.Key,
                    Ad = string.IsNullOrWhiteSpace(sandikBilgisi?.Ad) ? null : sandikBilgisi.Ad.Trim(),
                    DurumId = (int)SandikDurum.Hazirlaniyor,
                    En = sandikBilgisi?.En,
                    Boy = sandikBilgisi?.Boy,
                    Yukseklik = sandikBilgisi?.Yukseklik,
                    NetKg = sandikBilgisi?.NetKg,
                    GrossKg = sandikBilgisi?.GrossKg
                };
                await sandikRepo.AddAsync(sandik);
                await _unitOfWork.SaveChangesAsync();

                foreach (var satir in grup)
                {
                    await sandikIcerikRepo.AddAsync(new SandikIcerik
                    {
                        SandikId = sandik.Id,
                        CekiSatiriId = satir.Id,
                        KonulanAdet = 0,
                        EksikAdet = 0
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
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

        private static void ApplyManuelProjeBilgileri(Proje proje, ManuelCekiOlusturModel model)
        {
            proje.ProjeNo = model.ProjeNo.Trim();
            proje.FBNo = string.IsNullOrWhiteSpace(model.FBNo) ? proje.ProjeNo : model.FBNo.Trim();
            proje.ProjeTipiId = model.ProjeTipiId > 0 ? model.ProjeTipiId : (int)ProjeTipi.Normal;

            if (!string.IsNullOrWhiteSpace(model.Musteri)) proje.Musteri = model.Musteri.Trim();
            if (!string.IsNullOrWhiteSpace(model.Lokasyon)) proje.Lokasyon = model.Lokasyon.Trim();
            if (!string.IsNullOrWhiteSpace(model.Guc)) proje.Guc = model.Guc.Trim();
            if (!string.IsNullOrWhiteSpace(model.Gerilim)) proje.Gerilim = model.Gerilim.Trim();
            if (!string.IsNullOrWhiteSpace(model.ProjeMuduru)) proje.ProjeMuduru = model.ProjeMuduru.Trim();
            if (!string.IsNullOrWhiteSpace(model.SorumluKisi)) proje.SorumluKisi = model.SorumluKisi.Trim();
            if (!string.IsNullOrWhiteSpace(model.OlcuResmiNo)) proje.OlcuResmiNo = model.OlcuResmiNo.Trim();
            if (!string.IsNullOrWhiteSpace(model.NakilOlcuResmiNo)) proje.NakilOlcuResmiNo = model.NakilOlcuResmiNo.Trim();
            if (!string.IsNullOrWhiteSpace(model.SonMontajResmiNo)) proje.SonMontajResmiNo = model.SonMontajResmiNo.Trim();
            if (model.PlanlananSevkTarihi.HasValue) proje.PlanlananSevkTarihi = model.PlanlananSevkTarihi;
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
        /// Reads case names and physical values from the packing list sheet.
        /// </summary>
        private static Dictionary<string, SandikImportBilgisi> OkuCekiListesiSandikBilgileri(IXLWorkbook workbook)
        {
            var sonuc = new Dictionary<string, SandikImportBilgisi>();
            var worksheet = workbook.Worksheets.FirstOrDefault(ws =>
            {
                var name = NormalizeExcelText(ws.Name);
                return name.Contains("CEKI") && name.Contains("LISTESI") && !name.Contains("CIKTI");
            }) ?? workbook.Worksheets.FirstOrDefault(ws => NormalizeExcelText(ws.Name).Contains("PACKING LIST"));

            if (worksheet == null)
                return sonuc;

            var baslikSatiri = BulCekiListesiBaslikSatiri(worksheet);
            if (baslikSatiri == null)
                return sonuc;

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? baslikSatiri.Value;
            for (int r = baslikSatiri.Value + 1; r <= lastRow; r++)
            {
                var koliNoRaw = worksheet.Cell(r, 1).GetString().Trim();
                if (string.IsNullOrWhiteSpace(koliNoRaw))
                    continue;

                var sandikIsmi = worksheet.Cell(r, 10).GetString().Trim();
                var ambalajCinsi = worksheet.Cell(r, 3).GetString().Trim();
                var netKg = ReadNullableDecimal(worksheet.Cell(r, 4));
                var grossKg = ReadNullableDecimal(worksheet.Cell(r, 5));
                var boyMm = ReadNullableDecimal(worksheet.Cell(r, 6));
                var enMm = ReadNullableDecimal(worksheet.Cell(r, 7));
                var yukseklikMm = ReadNullableDecimal(worksheet.Cell(r, 8));

                if (string.IsNullOrWhiteSpace(sandikIsmi)
                    && string.IsNullOrWhiteSpace(ambalajCinsi)
                    && netKg == null
                    && grossKg == null
                    && boyMm == null
                    && enMm == null
                    && yukseklikMm == null)
                {
                    continue;
                }

                foreach (var koliNo in ExpandKoliNo(koliNoRaw))
                {
                    if (sonuc.ContainsKey(koliNo))
                        continue;

                    sonuc[koliNo] = new SandikImportBilgisi
                    {
                        SandikNo = koliNo,
                        SandikIsmi = string.IsNullOrWhiteSpace(sandikIsmi) ? null : sandikIsmi,
                        AmbalajCinsi = string.IsNullOrWhiteSpace(ambalajCinsi) ? null : ambalajCinsi,
                        En = MmToCm(enMm),
                        Boy = MmToCm(boyMm),
                        Yukseklik = MmToCm(yukseklikMm),
                        NetKg = netKg,
                        GrossKg = grossKg
                    };
                }
            }

            return sonuc;
        }

        private static int? BulCekiListesiBaslikSatiri(IXLWorksheet worksheet)
        {
            var lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? 0, 100);
            var lastColumn = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, 20);

            for (int r = 1; r <= lastRow; r++)
            {
                var rowText = new StringBuilder();
                for (int c = 1; c <= lastColumn; c++)
                    rowText.Append(' ').Append(NormalizeExcelText(worksheet.Cell(r, c).GetString()));

                var text = rowText.ToString();
                if ((text.Contains("KOLI") || text.Contains("CASE"))
                    && (text.Contains("AMBALAJ") || text.Contains("PACKAGE"))
                    && (text.Contains("CINSI") || text.Contains("DESCRIPTION")))
                {
                    return r;
                }
            }

            return null;
        }

        private static IEnumerable<string> ExpandKoliNo(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                yield break;

            var text = value.Trim()
                .Replace('\u00A0', ' ')
                .Replace('\u2013', '-')
                .Replace('\u2014', '-');

            var parts = text.Split(new[] { ',', ';', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var normalizedPart = part.Trim();
                var rangeParts = normalizedPart.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (rangeParts.Length == 2
                    && int.TryParse(NormalizeKoliNo(rangeParts[0]), out var start)
                    && int.TryParse(NormalizeKoliNo(rangeParts[1]), out var end)
                    && start > 0
                    && end >= start
                    && end - start <= 500)
                {
                    for (var no = start; no <= end; no++)
                        yield return no.ToString(CultureInfo.InvariantCulture);

                    continue;
                }

                var normalized = NormalizeKoliNo(normalizedPart);
                if (!string.IsNullOrWhiteSpace(normalized))
                    yield return normalized;
            }
        }

        private static string NormalizeKoliNo(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var text = value.Trim()
                .Replace('\u00A0', ' ')
                .Replace(" ", string.Empty)
                .Replace('\u2013', '-')
                .Replace('\u2014', '-');

            if (!text.Contains(',') && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericValue)
                && numericValue == decimal.Truncate(numericValue))
            {
                return ((int)numericValue).ToString(CultureInfo.InvariantCulture);
            }

            return text;
        }

        private static decimal? ReadNullableDecimal(IXLCell cell)
        {
            var text = cell.GetString().Trim().Replace("\u00A0", string.Empty).Replace(" ", string.Empty);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out var trValue))
                return trValue;

            if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var invariantValue))
                return invariantValue;

            return null;
        }

        private static decimal? MmToCm(decimal? value)
        {
            return value.HasValue ? Math.Round(value.Value / 10m, 2) : null;
        }

        private static string NormalizeExcelText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Trim()
                .Replace('\u00A0', ' ')
                .ToUpper(new CultureInfo("tr-TR"))
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    builder.Append(ch);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private sealed class SandikImportBilgisi
        {
            public string SandikNo { get; set; } = string.Empty;
            public string? SandikIsmi { get; set; }
            public string? AmbalajCinsi { get; set; }
            public decimal? En { get; set; }
            public decimal? Boy { get; set; }
            public decimal? Yukseklik { get; set; }
            public decimal? NetKg { get; set; }
            public decimal? GrossKg { get; set; }
        }

        /// <summary>
        /// Sanitizes picture names that ClosedXML cannot read.
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
