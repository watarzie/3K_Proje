using ClosedXML.Excel;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// İş akışı 2: Çeki yükleme — Excel okunur, satırlar parse edilir,
    /// aynı sandık numaralı satırlar gruplanır, sandık kayıtları otomatik oluşturulur.
    /// Kural: Aynı sandık numarası 100 satırda geçiyorsa 1 sandıktır, 100 değil.
    /// </summary>
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
            // 1. Stream'i MemoryStream'e kopyala (IFormFile stream'i seek desteklemeyebilir)
            using var memoryStream = new MemoryStream();
            await excelDosya.CopyToAsync(memoryStream);
            var dosyaBytes = memoryStream.ToArray();

            // 2. Excel header bilgilerini parse edip FB No'yu al (Proje Adı)
            string fbNo = string.Empty;
            Proje proje = null;
            var projeRepo = _unitOfWork.GetRepository<Proje>();

            using (var headerStream = new MemoryStream(dosyaBytes))
            using (var headerWorkbook = new XLWorkbook(headerStream))
            {
                // Gelişmiş isim normalizasyonu
                string NormalizeName(string name)
                {
                    if (string.IsNullOrWhiteSpace(name)) return "";
                    return name.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"))
                        .Replace("Ç", "C").Replace("Ş", "S").Replace("İ", "I").Replace("Ö", "O").Replace("Ü", "U").Replace("Ğ", "G");
                }

                IXLWorksheet headerSheet = null;
                var allSheets = headerWorkbook.Worksheets.ToList();

                foreach (var ws in allSheets)
                {
                    if (NormalizeName(ws.Name) == "CIKTI SAYFASI")
                    {
                        headerSheet = ws;
                        break;
                    }
                }

                if (headerSheet == null)
                {
                    foreach (var ws in allSheets)
                    {
                        var nName = NormalizeName(ws.Name);
                        if (nName.Contains("CIKTI") && !nName.Contains("YDK") && !nName.Contains("YEDEK"))
                        {
                            headerSheet = ws;
                            break;
                        }
                    }
                }

                if (headerSheet == null)
                    throw new Exception("Excel dosyasında 'ÇIKTI SAYFASI' bulunamadı! Lütfen ilgili çeki için ÇIKTI SAYFASI içeren şablonu yüklediğinizden emin olun.");

                // FB NO değerini dinamik bul (Genelde C1'de 'FB NO' yazar, değer yanındaki D1 veya E1 hücresindedir)
                fbNo = string.Empty;
                for (int r = 1; r <= 8; r++)
                {
                    for (int c = 1; c <= 8; c++)
                    {
                        var val = headerSheet.Cell(r, c).GetString().ToUpper().Trim();
                        if (val.Contains("FB NO") || val.Contains("SERIAL NO"))
                        {
                            fbNo = headerSheet.Cell(r, c + 1).GetString().Trim();
                            if (string.IsNullOrWhiteSpace(fbNo))
                                fbNo = headerSheet.Cell(r, c + 2).GetString().Trim();
                            break;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(fbNo)) break;
                }

                if (string.IsNullOrWhiteSpace(fbNo))
                    throw new Exception("Excel şablonunda FB NO (Proje Adı) bulunamadı. Lütfen 'FB NO' hücresinin hemen yanında bir değer olduğünden emin olun.");

                // Projeyi kontrol et, yoksa oluştur
                proje = await _context.Projeler.FirstOrDefaultAsync(p => p.FBNo == fbNo || p.ProjeNo == fbNo);

                if (proje == null)
                {
                    proje = new Proje
                    {
                        ProjeNo = fbNo,
                        FBNo = fbNo,
                        Durum = ProjeDurum.Hazirlaniyor
                    };
                    await projeRepo.AddAsync(proje);
                    await _unitOfWork.SaveChangesAsync(); // Proje Id almak için
                }

                var musteri = headerSheet.Cell(2, 6).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(musteri)) proje.Musteri = musteri;

                var lokasyon = headerSheet.Cell(4, 6).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(lokasyon)) proje.Lokasyon = lokasyon;

                // Ölçü Resmi No, Nakil Ölçü, Montaj
                for (int c = 10; c <= 16; c++)
                {
                    var val = headerSheet.Cell(4, c).GetString().Trim();
                    if (val.StartsWith("T0") || val.StartsWith("T1")) { proje.OlcuResmiNo = val; break; }
                }
                for (int c = 10; c <= 16; c++)
                {
                    var val = headerSheet.Cell(3, c).GetString().Trim();
                    if (val.StartsWith("T0") || val.StartsWith("T1")) { proje.NakilOlcuResmiNo = val; break; }
                }
                for (int c = 10; c <= 16; c++)
                {
                    var val = headerSheet.Cell(2, c).GetString().Trim();
                    if (val.StartsWith("T0") || val.StartsWith("T1")) { proje.SonMontajResmiNo = val; break; }
                }

                projeRepo.Update(proje);
                await _unitOfWork.SaveChangesAsync();
            }

            // 3. Dosyayı diske kaydet (orijinal şablon saklanır — iş akışı 9'da PDF için lazım)
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", proje.Id.ToString());
            Directory.CreateDirectory(uploadsDir);
            var dosyaYolu = Path.Combine(uploadsDir, dosyaAdi);
            await File.WriteAllBytesAsync(dosyaYolu, dosyaBytes);

            // 4. Çeki kaydı oluştur
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var ceki = new Ceki
            {
                ProjeId = proje.Id,
                OrijinalDosyaYolu = dosyaYolu
            };
            await cekiRepo.AddAsync(ceki);
            await _unitOfWork.SaveChangesAsync();

            // 4. Excel dosyasını oku (MemoryStream'den yeni bir kopya ile)
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = new List<CekiSatiri>();

            using (var excelStream = new MemoryStream(dosyaBytes))
            using (var workbook = new XLWorkbook(excelStream))
            {
                // Gelişmiş isim normalizasyonu
                string NormalizeName(string name)
                {
                    if (string.IsNullOrWhiteSpace(name)) return "";
                    return name.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"))
                        .Replace("Ç", "C").Replace("Ş", "S").Replace("İ", "I").Replace("Ö", "O").Replace("Ü", "U").Replace("Ğ", "G");
                }

                IXLWorksheet worksheet = null;
                var allSheets = workbook.Worksheets.ToList();

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
                    throw new Exception("Excel dosyasında verilerin okunabileceği 'ÇIKTI SAYFASI' bulunamadı. Yedek sayfalar yerine direkt ÇIKTI SAYFASI içeren orijinal şablon gereklidir.");

                // Çıktı sayfası başlık satırını atlamak için A sütununda sayı olan ilk satırı araştırırız.
                int baslangicSatir = 6; // Tahmini
                for (int r = 1; r <= 20; r++)
                {
                    var cellVal = worksheet.Cell(r, 1).GetString().Trim(); // A(1) Sütunu kontrol edilir
                    if (int.TryParse(cellVal, out _))
                    {
                        baslangicSatir = r;
                        break;
                    }
                }

                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? baslangicSatir;

                // MAPPING -- ÇIKTI SAYFASI FORMATI
                // A(1) = SIRA NO
                // B(2) = ÖLÇÜ R.P NO (İstenmiyor)
                // C(3) = BARKOD NO
                // D(4) = TANIM (Aciklama)
                // E(5) = KOLİ NO (CekideGecenSandikNo)
                // F(6) = MİKTAR (IstenenAdet)
                // G(7) = BİRİM
                // M(13) = AÇIKLAMA / REMARKS

                for (int r = baslangicSatir; r <= lastRow; r++)
                {
                    var row = worksheet.Row(r);

                    if (row.IsEmpty()) continue;

                    var barkod = row.Cell(3).GetString().Trim();   // C(3)
                    var tanim = row.Cell(4).GetString().Trim();    // D(4)
                    
                    if (string.IsNullOrWhiteSpace(tanim) && string.IsNullOrWhiteSpace(barkod))
                        continue;

                    // Sıra No — A(1)
                    int siraNo = satirlar.Count + 1;
                    var siraNoStr = row.Cell(1).GetString().Trim(); 
                    if (int.TryParse(siraNoStr, out int parsedSira))
                        siraNo = parsedSira;

                    var koliNo = row.Cell(5).GetString().Trim();         // E(5)

                    // Miktar / IstenenAdet — F(6)
                    int istenenAdet = 0;
                    try
                    {
                        var miktarStr = row.Cell(6).GetString().Trim();  
                        if (!string.IsNullOrWhiteSpace(miktarStr) && double.TryParse(miktarStr, out double m))
                            istenenAdet = (int)m;
                    }
                    catch { }

                    var birim = row.Cell(7).GetString().Trim();          // G(7)
                    if (string.IsNullOrWhiteSpace(birim)) birim = "ADET";

                    // Açıklama/Remarks — M(13)
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
                        Durum = UrunDurum.Bekliyor,
                        FiiliSandikNo = koliNo // Başlangıçta fiili = çekideki
                    };
                    satirlar.Add(satir);
                    await cekiSatiriRepo.AddAsync(satir);
                }
            }
            await _unitOfWork.SaveChangesAsync();

            // 5. Benzersiz sandık numaralarını grupla ve sandık kayıtları oluştur
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var benzersizSandiklar = satirlar.GroupBy(s => s.CekideGecenSandikNo);

            foreach (var grup in benzersizSandiklar)
            {
                if (string.IsNullOrWhiteSpace(grup.Key)) continue;

                // Sandık kaydı oluştur
                var sandik = new Sandik
                {
                    ProjeId = proje.Id,
                    SandikNo = grup.Key,
                    Durum = SandikDurum.Hazirlaniyor
                };
                await sandikRepo.AddAsync(sandik);
                await _unitOfWork.SaveChangesAsync();

                // Her sandığa ait ürünler için SandikIcerik oluştur
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
