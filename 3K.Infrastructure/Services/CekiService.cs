using ClosedXML.Excel;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            using (var excelStream = new MemoryStream(dosyaBytes))
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
                    proje = new Proje { ProjeNo = fbNo, FBNo = fbNo, Durum = "Hazırlanıyor" };
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
                    if (string.IsNullOrWhiteSpace(birim)) birim = "ADET";

                    var remarks = row.Cell(13).GetString().Trim();

                    var satir = new CekiSatiri
                    {
                        CekiId = ceki.Id,
                        SiraNo = siraNo,
                        BarkodNo = barkod,
                        Aciklama = tanim,
                        CekideGecenSandikNo = koliNo,
                        IstenenAdet = istenenAdet,
                        Birim = birim,
                        Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks,
                        Durum = "Bekliyor",
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
                        Durum = "Hazırlanıyor"
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
    }
}