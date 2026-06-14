using ClosedXML.Excel;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
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
        private readonly IDurumHesaplaService _durumHesaplaService;

        public CekiService(
            IUnitOfWork unitOfWork,
            AppDbContext context,
            IHareketService hareketService,
            IDurumHesaplaService durumHesaplaService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _hareketService = hareketService;
            _durumHesaplaService = durumHesaplaService;
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
                var pozNoKolon = BulCiktiPozNoKolonu(worksheet, baslangicSatir - 1);

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

                    var olcuResmiPozNo = pozNoKolon.HasValue
                        ? row.Cell(pozNoKolon.Value).GetString().Trim()
                        : string.Empty;
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
                        OlcuResmiPozNo = string.IsNullOrWhiteSpace(olcuResmiPozNo) ? null : olcuResmiPozNo,
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
                        AdIngilizce = sandikBilgisi?.SandikIsmiIngilizce,
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

        public async Task<CekiRevizyonOnizlemeSonuc> CekiRevizyonOnizleAsync(Stream excelDosya, string dosyaAdi)
        {
            using var memoryStream = new MemoryStream();
            await excelDosya.CopyToAsync(memoryStream);
            var dosyaBytes = memoryStream.ToArray();
            var cleanedBytes = SanitizePictureNames(dosyaBytes);

            CiktiImportBilgisi import;
            using (var excelStream = new MemoryStream(cleanedBytes))
            using (var workbook = new XLWorkbook(excelStream))
            {
                import = OkuCiktiImportBilgisi(workbook);
            }

            if (string.IsNullOrWhiteSpace(import.FbNo))
                throw new Exception("Revizyon Excel dosyasında FB NO (Proje Adı) bulunamadı.");

            var proje = await _context.Projeler
                .FirstOrDefaultAsync(p => p.FBNo == import.FbNo || p.ProjeNo == import.FbNo);

            if (proje == null)
                throw new Exception($"Revizyon dosyasındaki proje ({import.FbNo}) sistemde bulunamadı. Önce ana çeki yüklenmiş olmalı.");

            var anaCeki = await _context.Cekiler
                .Where(c => c.ProjeId == proje.Id && c.CekiTipiId == (int)CekiTipi.Normal)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (anaCeki == null)
                throw new Exception($"{proje.ProjeNo} projesi için revize edilecek ana çeki bulunamadı.");

            var anaSatirlar = await _context.CekiSatirlari
                .Include(s => s.SandikIcerikleri)
                .ThenInclude(i => i.Sandik)
                .Where(s => s.CekiId == anaCeki.Id)
                .ToListAsync();

            return await RevizyonOnizlemeOlusturAsync(import, dosyaAdi, proje, anaCeki, anaSatirlar);
        }

        public async Task<CekiRevizyonSonuc> CekiRevizyonYukleAsync(Stream excelDosya, string dosyaAdi, int kullaniciId)
        {
            using var memoryStream = new MemoryStream();
            await excelDosya.CopyToAsync(memoryStream);
            var dosyaBytes = memoryStream.ToArray();
            var cleanedBytes = SanitizePictureNames(dosyaBytes);

            CiktiImportBilgisi import;
            using (var excelStream = new MemoryStream(cleanedBytes))
            using (var workbook = new XLWorkbook(excelStream))
            {
                import = OkuCiktiImportBilgisi(workbook);
            }

            if (string.IsNullOrWhiteSpace(import.FbNo))
                throw new Exception("Revizyon Excel dosyasında FB NO (Proje Adı) bulunamadı.");

            var revizyonSatirlari = import.Satirlar
                .Where(s => !string.IsNullOrWhiteSpace(s.CheckKodu))
                .ToList();

            if (revizyonSatirlari.Count == 0)
                throw new Exception("Revizyon Excel dosyasında CHECK/KONTROL kolonunda A, U veya D işareti bulunamadı.");

            var proje = await _context.Projeler
                .FirstOrDefaultAsync(p => p.FBNo == import.FbNo || p.ProjeNo == import.FbNo);

            if (proje == null)
                throw new Exception($"Revizyon dosyasındaki proje ({import.FbNo}) sistemde bulunamadı. Önce ana çeki yüklenmiş olmalı.");

            var anaCeki = await _context.Cekiler
                .Where(c => c.ProjeId == proje.Id && c.CekiTipiId == (int)CekiTipi.Normal)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();

            if (anaCeki == null)
                throw new Exception($"{proje.ProjeNo} projesi için revize edilecek ana çeki bulunamadı.");

            var anaSatirlar = await _context.CekiSatirlari
                .Include(s => s.SandikIcerikleri)
                .ThenInclude(i => i.Sandik)
                .Where(s => s.CekiId == anaCeki.Id)
                .ToListAsync();

            var siraGruplari = anaSatirlar
                .GroupBy(s => s.SiraNo)
                .ToDictionary(g => g.Key, g => g.ToList());

            var sandiklar = await _context.Sandiklar
                .Where(s => s.ProjeId == proje.Id)
                .ToListAsync();

            var sandikCache = sandiklar
                .GroupBy(s => NormalizeKoliNo(s.SandikNo), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            using var transaction = await _context.Database.BeginTransactionAsync();

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", proje.Id.ToString(), "Revizyonlar");
            Directory.CreateDirectory(uploadsDir);
            var dosyaYolu = Path.Combine(uploadsDir, $"{TurkeyTime.Now:yyyyMMdd_HHmmss}_{CleanFileName(dosyaAdi)}");
            await File.WriteAllBytesAsync(dosyaYolu, dosyaBytes);

            var revizyonCeki = new Ceki
            {
                ProjeId = proje.Id,
                OrijinalDosyaYolu = dosyaYolu,
                YuklemeTarihi = TurkeyTime.Now,
                CekiTipiId = (int)CekiTipi.Revizyon,
                KaynakCekiId = anaCeki.Id,
                Aciklama = $"Revizyon dosyası: {dosyaAdi}"
            };

            _context.Cekiler.Add(revizyonCeki);
            await _context.SaveChangesAsync();

            var sonuc = new CekiRevizyonSonuc
            {
                ProjeId = proje.Id,
                ProjeNo = proje.ProjeNo,
                AnaCekiId = anaCeki.Id,
                RevizyonCekiId = revizyonCeki.Id
            };

            var silinecekSatirlar = new List<CekiSatiri>();
            var guncellenecekler = new List<(CekiSatiri Satir, CiktiSatirImportBilgisi RevizyonSatiri)>();
            var eklenecekler = new List<CiktiSatirImportBilgisi>();

            foreach (var revizyonSatiri in revizyonSatirlari)
            {
                var kod = revizyonSatiri.CheckKodu;

                if (kod is "A" or "U" && !revizyonSatiri.VeriSatiriMi)
                    throw new Exception($"Revizyon satırı boş: Excel satırı {revizyonSatiri.ExcelSatirNo}, CHECK={kod}.");

                if (kod is "A" or "U" && revizyonSatiri.IstenenAdet <= 0)
                    throw new Exception($"Revizyon satırında miktar geçersiz: Excel satırı {revizyonSatiri.ExcelSatirNo}, CHECK={kod}.");

                if (kod == "A")
                {
                    eklenecekler.Add(revizyonSatiri);
                    continue;
                }

                var mevcutSatir = EslesenAnaSatiriBul(revizyonSatiri, siraGruplari, anaSatirlar);
                if (mevcutSatir == null)
                    throw new Exception($"Revizyon {kod} satırı ana çekide bulunamadı. Excel satırı: {revizyonSatiri.ExcelSatirNo}, Sıra No: {revizyonSatiri.SiraNo}.");

                if (kod == "U")
                {
                    guncellenecekler.Add((mevcutSatir, revizyonSatiri));
                }
                else if (kod == "D")
                {
                    silinecekSatirlar.Add(mevcutSatir);
                }
            }

            if (silinecekSatirlar.Count > 0)
                sonuc.SilinenSatirSayisi = await RevizyonSatirlariniSilAsync(proje.Id, silinecekSatirlar.DistinctBy(s => s.Id).ToList(), kullaniciId);

            foreach (var item in guncellenecekler)
            {
                if (CekiSatiriIslemGormusMu(item.Satir))
                    await RevizyonSatiriIslemleriniGeriAlAsync(proje.Id, item.Satir, kullaniciId, $"Revizyon U satırı uygulanmadan önce otomatik geri alındı. Excel satırı: {item.RevizyonSatiri.ExcelSatirNo}");

                await RevizyonSatiriniGuncelleAsync(proje.Id, item.Satir, item.RevizyonSatiri, sandikCache, import.SandikBilgileri, kullaniciId);
                sonuc.GuncellenenSatirSayisi++;
            }

            foreach (var revizyonSatiri in eklenecekler)
            {
                await RevizyonSatiriEkleAsync(proje.Id, anaCeki.Id, revizyonSatiri, sandikCache, import.SandikBilgileri, kullaniciId);
                sonuc.EklenenSatirSayisi++;
            }

            _context.HareketGecmisleri.Add(new HareketGecmisi
            {
                ProjeId = proje.Id,
                ReferansTipi = "Ceki",
                ReferansId = anaCeki.Id.ToString(),
                ReferansMetni = $"Çeki Revizyonu - {dosyaAdi}",
                Islem = "Çeki Revizyonu Uygulandı",
                KullaniciId = kullaniciId,
                Tarih = TurkeyTime.Now,
                EskiDeger = anaCeki.Id.ToString(),
                YeniDeger = revizyonCeki.Id.ToString(),
                Aciklama = $"Eklenen: {sonuc.EklenenSatirSayisi}, Güncellenen: {sonuc.GuncellenenSatirSayisi}, Silinen: {sonuc.SilinenSatirSayisi}"
            });

            await _context.SaveChangesAsync();
            await BosSandiklariTemizleVeDurumlariGuncelleAsync(proje.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            sonuc.Mesaj = $"{proje.ProjeNo} revizyonu uygulandı. Eklenen: {sonuc.EklenenSatirSayisi}, Güncellenen: {sonuc.GuncellenenSatirSayisi}, Silinen: {sonuc.SilinenSatirSayisi}.";
            return sonuc;
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

        private async Task<CekiRevizyonOnizlemeSonuc> RevizyonOnizlemeOlusturAsync(
            CiktiImportBilgisi import,
            string dosyaAdi,
            Proje proje,
            Ceki anaCeki,
            List<CekiSatiri> anaSatirlar)
        {
            var revizyonSatirlari = import.Satirlar
                .Where(s => !string.IsNullOrWhiteSpace(s.CheckKodu))
                .ToList();

            if (revizyonSatirlari.Count == 0)
                throw new Exception("Revizyon Excel dosyasında CHECK/KONTROL kolonunda A, U veya D işareti bulunamadı.");

            var siraGruplari = anaSatirlar
                .GroupBy(s => s.SiraNo)
                .ToDictionary(g => g.Key, g => g.ToList());

            var sonuc = new CekiRevizyonOnizlemeSonuc
            {
                ProjeId = proje.Id,
                ProjeNo = proje.ProjeNo,
                AnaCekiId = anaCeki.Id,
                DosyaAdi = dosyaAdi,
                ToplamIsaretliSatirSayisi = revizyonSatirlari.Count
            };

            foreach (var revizyonSatiri in revizyonSatirlari)
            {
                var satir = new CekiRevizyonOnizlemeSatiri
                {
                    ExcelSatirNo = revizyonSatiri.ExcelSatirNo,
                    CheckKodu = revizyonSatiri.CheckKodu,
                    IslemTipi = revizyonSatiri.CheckKodu switch
                    {
                        "A" => "Eklenecek",
                        "U" => "Güncellenecek",
                        "D" => "Silinecek",
                        _ => "Bilinmiyor"
                    },
                    YeniSiraNo = revizyonSatiri.SiraNo,
                    BarkodNo = revizyonSatiri.BarkodNo,
                    PozNo = revizyonSatiri.OlcuResmiPozNo,
                    Tanim = revizyonSatiri.Aciklama,
                    YeniKoliNo = revizyonSatiri.KoliNo,
                    YeniIstenenAdet = revizyonSatiri.IstenenAdet
                };

                if (revizyonSatiri.CheckKodu is "A" or "U" && !revizyonSatiri.VeriSatiriMi)
                {
                    RevizyonSatirRiskEkle(satir, "Engel", "CHECK işaretli satır boş görünüyor.");
                    sonuc.Satirlar.Add(satir);
                    continue;
                }

                if (revizyonSatiri.CheckKodu is "A" or "U" && revizyonSatiri.IstenenAdet <= 0)
                {
                    RevizyonSatirRiskEkle(satir, "Engel", "Miktar sıfır veya negatif olamaz.");
                    sonuc.Satirlar.Add(satir);
                    continue;
                }

                if (revizyonSatiri.CheckKodu == "A")
                {
                    sonuc.EklenenSatirSayisi++;
                    satir.Mesaj = "Yeni çeki satırı olarak eklenecek.";

                    if (RevizyonBenzerSatirVarMi(revizyonSatiri, anaSatirlar))
                        RevizyonSatirRiskEkle(satir, "Uyarı", "Ana çekide aynı barkod/poz/sandık ile benzer bir satır var. Yine de CHECK=A olduğu için yeni satır olarak ele alınacak.");

                    sonuc.Satirlar.Add(satir);
                    continue;
                }

                var mevcutSatir = EslesenAnaSatiriBul(revizyonSatiri, siraGruplari, anaSatirlar);
                if (mevcutSatir == null)
                {
                    RevizyonSatirRiskEkle(satir, "Engel", $"Ana çekide eşleşen satır bulunamadı. Sıra No: {revizyonSatiri.SiraNo}");
                    sonuc.Satirlar.Add(satir);
                    continue;
                }

                RevizyonEskiSatirBilgileriniDoldur(satir, mevcutSatir);

                if (revizyonSatiri.CheckKodu == "U")
                {
                    sonuc.GuncellenenSatirSayisi++;
                    RevizyonDegisiklikleriDoldur(satir, mevcutSatir, revizyonSatiri);

                    if (satir.Degisiklikler.Count == 0)
                        satir.Degisiklikler.Add("Ana veride değişiklik yok.");

                    if (satir.IslemGormusMu)
                    {
                        var geriAlEngeli = await RevizyonOtomatikGeriAlEngelMesajiAsync(mevcutSatir);
                        if (!string.IsNullOrWhiteSpace(geriAlEngeli))
                            RevizyonSatirRiskEkle(satir, "Engel", geriAlEngeli);
                        else
                            RevizyonSatirRiskEkle(satir, "Uyarı", "Satır işlem görmüş. Revizyon uygulanırken önce Grid/3K/stok/proje hareketleri otomatik geri alınacak, sonra ana veri güncellenecek.");
                    }

                    if (!string.Equals(NormalizeKoliNo(mevcutSatir.CekideGecenSandikNo), NormalizeKoliNo(revizyonSatiri.KoliNo), StringComparison.OrdinalIgnoreCase)
                        && mevcutSatir.SandikIcerikleri.Any()
                        && !RevizyonIcerikleriTasinabilirMi(mevcutSatir.SandikIcerikleri.ToList()))
                    {
                        RevizyonSatirRiskEkle(satir, "Uyarı", "Sandık değişiyor fakat satırda işlem/konum bilgisi var. Planlanan sandık güncellenir, mevcut operasyon izi korunur.");
                    }

                    if (string.IsNullOrWhiteSpace(satir.Mesaj))
                        satir.Mesaj = "Ana çeki satırı güncellenecek.";
                }
                else if (revizyonSatiri.CheckKodu == "D")
                {
                    sonuc.SilinecekSatirSayisi++;
                    satir.Mesaj = "Ana çekiden silinecek.";

                    if (satir.IslemGormusMu)
                        RevizyonSatirRiskEkle(satir, "Uyarı", "Satır işlem görmüş. Revizyon uygulanırken geri alınabilir hareketler otomatik temizlenecek ve satır silinecek.");

                    if (mevcutSatir.SandikIcerikleri.Any(i => i.Sandik?.DurumId == (int)SandikDurum.Sevkedildi))
                        RevizyonSatirRiskEkle(satir, "Engel", "Satır sevk edilmiş sandıkta. Silmek için önce ilgili sandığın sevk kilidini açın.");

                    if (await RevizyonDisariGidenAktifTransferVarMiAsync(mevcutSatir.Id))
                        RevizyonSatirRiskEkle(satir, "Engel", "Satır başka bir proje/satıra kaynak olarak verilmiş. Silmek için önce hedef projedeki karşılama/transfer geri alınmalı.");
                }

                sonuc.Satirlar.Add(satir);
            }

            sonuc.RiskliSatirSayisi = sonuc.Satirlar.Count(s => s.RiskSeviyesi != "Güvenli");
            sonuc.EngelliSatirSayisi = sonuc.Satirlar.Count(s => !s.UygulanabilirMi);
            sonuc.UygulanabilirMi = sonuc.EngelliSatirSayisi == 0;
            sonuc.Mesaj = sonuc.UygulanabilirMi
                ? $"{proje.ProjeNo} revizyonu ön izleme hazır. Eklenen: {sonuc.EklenenSatirSayisi}, Güncellenecek: {sonuc.GuncellenenSatirSayisi}, Silinecek: {sonuc.SilinecekSatirSayisi}."
                : $"{proje.ProjeNo} revizyonunda {sonuc.EngelliSatirSayisi} engelli satır var. Engeller giderilmeden revizyon uygulanamaz.";

            if (sonuc.RiskliSatirSayisi > 0)
                sonuc.Uyarilar.Add($"{sonuc.RiskliSatirSayisi} satırda uyarı/engel var. Uygulamadan önce detayları kontrol edin.");

            return sonuc;
        }

        private static void RevizyonEskiSatirBilgileriniDoldur(CekiRevizyonOnizlemeSatiri onizlemeSatiri, CekiSatiri mevcutSatir)
        {
            onizlemeSatiri.MevcutCekiSatiriId = mevcutSatir.Id;
            onizlemeSatiri.EskiSiraNo = mevcutSatir.SiraNo;
            onizlemeSatiri.EskiKoliNo = mevcutSatir.CekideGecenSandikNo;
            onizlemeSatiri.EskiIstenenAdet = mevcutSatir.IstenenAdet;
            onizlemeSatiri.IslemGormusMu = CekiSatiriIslemGormusMu(mevcutSatir);
            onizlemeSatiri.IslemGorenAdet = CekiSatiriIslemMiktari(mevcutSatir);

            if (string.IsNullOrWhiteSpace(onizlemeSatiri.BarkodNo))
                onizlemeSatiri.BarkodNo = mevcutSatir.BarkodNo;
            if (string.IsNullOrWhiteSpace(onizlemeSatiri.PozNo))
                onizlemeSatiri.PozNo = mevcutSatir.OlcuResmiPozNo;
            if (string.IsNullOrWhiteSpace(onizlemeSatiri.Tanim))
                onizlemeSatiri.Tanim = mevcutSatir.Aciklama;
        }

        private static void RevizyonDegisiklikleriDoldur(CekiRevizyonOnizlemeSatiri satir, CekiSatiri mevcutSatir, CiktiSatirImportBilgisi revizyonSatiri)
        {
            RevizyonDegisiklikEkle(satir, "Sıra No", mevcutSatir.SiraNo, revizyonSatiri.SiraNo);
            RevizyonDegisiklikEkle(satir, "Barkod", mevcutSatir.BarkodNo, revizyonSatiri.BarkodNo);
            RevizyonDegisiklikEkle(satir, "Poz No", mevcutSatir.OlcuResmiPozNo, revizyonSatiri.OlcuResmiPozNo);
            RevizyonDegisiklikEkle(satir, "Tanım", mevcutSatir.Aciklama, revizyonSatiri.Aciklama);
            RevizyonDegisiklikEkle(satir, "Sandık", mevcutSatir.CekideGecenSandikNo, revizyonSatiri.KoliNo);
            RevizyonDegisiklikEkle(satir, "Miktar", mevcutSatir.IstenenAdet, revizyonSatiri.IstenenAdet);
            RevizyonDegisiklikEkle(satir, "Birim", mevcutSatir.BirimId, revizyonSatiri.BirimId);
            RevizyonDegisiklikEkle(satir, "Açıklama", mevcutSatir.Remarks, revizyonSatiri.Remarks);
        }

        private static void RevizyonDegisiklikEkle(CekiRevizyonOnizlemeSatiri satir, string alan, int eskiDeger, int yeniDeger)
        {
            if (eskiDeger != yeniDeger)
                satir.Degisiklikler.Add($"{alan}: {eskiDeger.ToString(CultureInfo.InvariantCulture)} → {yeniDeger.ToString(CultureInfo.InvariantCulture)}");
        }

        private static void RevizyonDegisiklikEkle(CekiRevizyonOnizlemeSatiri satir, string alan, decimal eskiDeger, decimal yeniDeger)
        {
            if (eskiDeger != yeniDeger)
                satir.Degisiklikler.Add($"{alan}: {FormatRevizyonDecimal(eskiDeger)} → {FormatRevizyonDecimal(yeniDeger)}");
        }

        private static void RevizyonDegisiklikEkle(CekiRevizyonOnizlemeSatiri satir, string alan, string? eskiDeger, string? yeniDeger)
        {
            var eski = (eskiDeger ?? string.Empty).Trim();
            var yeni = (yeniDeger ?? string.Empty).Trim();
            if (!string.Equals(eski, yeni, StringComparison.OrdinalIgnoreCase))
                satir.Degisiklikler.Add($"{alan}: {DisplayValue(eski)} → {DisplayValue(yeni)}");
        }

        private static string DisplayValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        private static string FormatRevizyonDecimal(decimal value)
        {
            return decimal.Truncate(value) == value
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private static void RevizyonSatirRiskEkle(CekiRevizyonOnizlemeSatiri satir, string riskSeviyesi, string mesaj)
        {
            if (riskSeviyesi == "Engel")
            {
                satir.RiskSeviyesi = "Engel";
                satir.UygulanabilirMi = false;
            }
            else if (satir.RiskSeviyesi != "Engel")
            {
                satir.RiskSeviyesi = "Uyarı";
            }

            satir.Uyarilar.Add(mesaj);
            if (string.IsNullOrWhiteSpace(satir.Mesaj))
                satir.Mesaj = mesaj;
        }

        private static bool CekiSatiriIslemGormusMu(CekiSatiri satir)
        {
            return satir.GridDurumuId != (int)GridDurum.Bekliyor
                || satir.GridGelenAdet > 0
                || satir.TrafoSevkAdet > 0
                || (satir.GridSevkMiktari ?? 0) > 0
                || satir.UcKDurumuId != (int)UcKDurum.Bekliyor
                || satir.GelenMiktar > 0
                || satir.StokKarsilanan > 0
                || satir.ProjeKarsilanan > 0
                || satir.ProjeGonderilen > 0
                || satir.TedarikciKarsilanan > 0
                || satir.HataliMiktar > 0
                || satir.GeriGonderilenMiktar > 0
                || satir.SandikIcerikleri.Any(i => i.KonulanAdet > 0 || i.EksikAdet > 0 || i.StokKarsilanan > 0 || i.ProjeKarsilanan > 0 || i.TedarikciKarsilanan > 0);
        }

        private static decimal CekiSatiriIslemMiktari(CekiSatiri satir)
        {
            var gridMiktari = Math.Max(satir.GridGelenAdet, satir.GridSevkMiktari ?? 0) + satir.TrafoSevkAdet;
            var ucKMiktari = satir.GelenMiktar + satir.StokKarsilanan + satir.ProjeKarsilanan + satir.TedarikciKarsilanan - satir.ProjeGonderilen;
            var sandikMiktari = satir.SandikIcerikleri.Sum(i => i.KonulanAdet + i.StokKarsilanan + i.ProjeKarsilanan + i.TedarikciKarsilanan);
            return Math.Max(Math.Max(gridMiktari, ucKMiktari), sandikMiktari);
        }

        private static bool RevizyonBenzerSatirVarMi(CiktiSatirImportBilgisi revizyonSatiri, List<CekiSatiri> anaSatirlar)
        {
            var barkod = NormalizeTextKey(revizyonSatiri.BarkodNo);
            if (string.IsNullOrWhiteSpace(barkod))
                return false;

            var poz = NormalizeTextKey(revizyonSatiri.OlcuResmiPozNo);
            var koli = NormalizeKoliNo(revizyonSatiri.KoliNo);

            return anaSatirlar.Any(s =>
                NormalizeTextKey(s.BarkodNo) == barkod &&
                NormalizeTextKey(s.OlcuResmiPozNo) == poz &&
                NormalizeKoliNo(s.CekideGecenSandikNo) == koli);
        }

        private async Task<bool> RevizyonDisariGidenAktifTransferVarMiAsync(int cekiSatiriId)
        {
            return await _context.ProjeTransferleri.AnyAsync(t =>
                t.KaynakCekiSatiriId == cekiSatiriId &&
                t.DurumId == (int)ProjeTransferDurum.Aktif &&
                (!t.HedefCekiSatiriId.HasValue || t.HedefCekiSatiriId.Value != cekiSatiriId));
        }

        private async Task<string?> RevizyonOtomatikGeriAlEngelMesajiAsync(CekiSatiri satir)
        {
            if (satir.SandikIcerikleri.Any(i => i.Sandik?.DurumId == (int)SandikDurum.Sevkedildi))
                return "Satır sevk edilmiş sandıkta. Otomatik geri alma yapılamaz; önce ilgili sandığın sevk kilidini açın.";

            if (await RevizyonDisariGidenAktifTransferVarMiAsync(satir.Id))
                return "Satır başka bir proje/satıra kaynak olarak verilmiş. Otomatik geri alma için önce hedef projedeki karşılama/transfer geri alınmalı.";

            return null;
        }

        private async Task RevizyonSatiriIslemleriniGeriAlAsync(int projeId, CekiSatiri satir, int kullaniciId, string aciklama)
        {
            var engel = await RevizyonOtomatikGeriAlEngelMesajiAsync(satir);
            if (!string.IsNullOrWhiteSpace(engel))
                throw new Exception($"Revizyon satırı otomatik geri alınamadı. Sıra No: {satir.SiraNo}. {engel}");

            var transferler = await _context.ProjeTransferleri
                .Where(t => t.DurumId == (int)ProjeTransferDurum.Aktif &&
                    (t.KaynakCekiSatiriId == satir.Id ||
                     (t.HedefCekiSatiriId.HasValue && t.HedefCekiSatiriId.Value == satir.Id)))
                .ToListAsync();

            var gelenTransferler = transferler
                .Where(t => t.HedefCekiSatiriId.HasValue && t.HedefCekiSatiriId.Value == satir.Id)
                .ToList();

            if (gelenTransferler.Any())
            {
                var kaynakIdler = gelenTransferler.Select(t => t.KaynakCekiSatiriId).Distinct().ToList();
                var kaynakSatirlar = await _context.CekiSatirlari
                    .Where(s => kaynakIdler.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id);

                foreach (var transfer in gelenTransferler)
                {
                    if (kaynakSatirlar.TryGetValue(transfer.KaynakCekiSatiriId, out var kaynakSatir))
                    {
                        kaynakSatir.ProjeGonderilen = Math.Max(kaynakSatir.ProjeGonderilen - transfer.Miktar, 0);
                        kaynakSatir.DurumId = _durumHesaplaService.HesaplaGenelDurum(kaynakSatir.GridDurumuId, kaynakSatir.UcKDurumuId);
                        _durumHesaplaService.HesaplaKalanVeDurum(kaynakSatir);
                        _context.CekiSatirlari.Update(kaynakSatir);
                    }

                    transfer.DurumId = (int)ProjeTransferDurum.GeriAlindi;
                    transfer.IptalTarihi = TurkeyTime.Now;
                    transfer.IptalAciklama = "Revizyon uygulaması sırasında satır otomatik geri alındı.";
                    _context.ProjeTransferleri.Update(transfer);
                }
            }

            var stokHareketleri = await _context.StokHareketleri
                .Where(h => h.CekiSatiriId == satir.Id)
                .ToListAsync();
            await RevizyonStokHareketleriniGeriAlAsync(stokHareketleri);

            satir.GridDurumuId = (int)GridDurum.Gelmedi;
            satir.GridGelenAdet = 0;
            satir.TrafoSevkAdet = 0;
            satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi;
            satir.GridSevkMiktari = 0;
            satir.YenidenSevkGerekliAdet = 0;
            satir.GridSevkTarihi = null;
            satir.GridAciklama = null;
            satir.GridPersonelId = null;

            satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
            satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
            satir.GelenMiktar = 0;
            satir.TeslimTarihi = null;
            satir.KaynakHedefProjeNo = null;
            satir.UcKAciklama = null;
            satir.KarsilananMiktar = 0;
            satir.StokKarsilanan = 0;
            satir.ProjeKarsilanan = 0;
            satir.ProjeGonderilen = 0;
            satir.TedarikciKarsilanan = 0;
            satir.HataliMiktar = 0;
            satir.GeriGonderilenMiktar = 0;
            satir.GeriGonderilmeSebebiId = null;
            satir.KaynakProjeId = null;
            satir.KaliteDurumId = null;
            satir.SurecDurumId = null;
            satir.PaketleyenId = null;
            satir.KontrolEdenId = null;

            satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
            _durumHesaplaService.HesaplaKalanVeDurum(satir);

            var icerikler = await _context.SandikIcerikleri
                .Where(i => i.CekiSatiriId == satir.Id)
                .ToListAsync();

            foreach (var icerik in icerikler)
            {
                icerik.KonulanAdet = 0;
                icerik.EksikAdet = 0;
                icerik.StokKarsilanan = 0;
                icerik.ProjeKarsilanan = 0;
                icerik.TedarikciKarsilanan = 0;
                _context.SandikIcerikleri.Update(icerik);
            }

            _context.CekiSatirlari.Update(satir);
            _context.HareketGecmisleri.Add(new HareketGecmisi
            {
                ProjeId = projeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                ReferansMetni = $"Sıra: {satir.SiraNo} - {satir.Aciklama}",
                Islem = "Revizyon Öncesi Otomatik Geri Al",
                KullaniciId = kullaniciId,
                Tarih = TurkeyTime.Now,
                YeniDeger = "Bekliyor",
                Aciklama = aciklama
            });
        }

        private async Task RevizyonSatiriEkleAsync(
            int projeId,
            int anaCekiId,
            CiktiSatirImportBilgisi revizyonSatiri,
            Dictionary<string, Sandik> sandikCache,
            Dictionary<string, SandikImportBilgisi> sandikBilgileri,
            int kullaniciId)
        {
            var sandik = await GetOrCreateSandikAsync(projeId, revizyonSatiri.KoliNo, sandikCache, sandikBilgileri);

            var satir = new CekiSatiri
            {
                CekiId = anaCekiId,
                SiraNo = revizyonSatiri.SiraNo,
                BarkodNo = revizyonSatiri.BarkodNo,
                OlcuResmiPozNo = string.IsNullOrWhiteSpace(revizyonSatiri.OlcuResmiPozNo) ? null : revizyonSatiri.OlcuResmiPozNo,
                Aciklama = revizyonSatiri.Aciklama,
                CekideGecenSandikNo = revizyonSatiri.KoliNo,
                FiiliSandikNo = revizyonSatiri.KoliNo,
                IstenenAdet = revizyonSatiri.IstenenAdet,
                BirimId = revizyonSatiri.BirimId,
                Remarks = string.IsNullOrWhiteSpace(revizyonSatiri.Remarks) ? null : revizyonSatiri.Remarks,
                DurumId = (int)UrunDurum.Bekliyor,
                GridDurumuId = (int)GridDurum.Gelmedi
            };

            _context.CekiSatirlari.Add(satir);
            _context.SandikIcerikleri.Add(new SandikIcerik
            {
                Sandik = sandik,
                CekiSatiri = satir,
                KonulanAdet = 0,
                EksikAdet = 0
            });

            _context.HareketGecmisleri.Add(new HareketGecmisi
            {
                ProjeId = projeId,
                ReferansTipi = "CekiSatiri",
                ReferansMetni = $"Sıra: {satir.SiraNo} - {satir.Aciklama}",
                Islem = "Revizyon Satırı Eklendi",
                KullaniciId = kullaniciId,
                Tarih = TurkeyTime.Now,
                YeniDeger = RevizyonSatirOzeti(revizyonSatiri),
                Aciklama = $"Revizyon A satırı eklendi. Excel satırı: {revizyonSatiri.ExcelSatirNo}"
            });
        }

        private async Task RevizyonSatiriniGuncelleAsync(
            int projeId,
            CekiSatiri mevcutSatir,
            CiktiSatirImportBilgisi revizyonSatiri,
            Dictionary<string, Sandik> sandikCache,
            Dictionary<string, SandikImportBilgisi> sandikBilgileri,
            int kullaniciId)
        {
            var eskiOzet = CekiSatiriOzeti(mevcutSatir);
            var eskiKoliNo = NormalizeKoliNo(mevcutSatir.CekideGecenSandikNo);
            var eskiFiiliKoliNo = NormalizeKoliNo(mevcutSatir.FiiliSandikNo);
            var yeniKoliNo = NormalizeKoliNo(revizyonSatiri.KoliNo);

            var fiiliPlanlaAyni = string.IsNullOrWhiteSpace(eskiFiiliKoliNo) ||
                string.Equals(eskiFiiliKoliNo, eskiKoliNo, StringComparison.OrdinalIgnoreCase);

            var hedefSandik = await GetOrCreateSandikAsync(projeId, yeniKoliNo, sandikCache, sandikBilgileri);

            mevcutSatir.SiraNo = revizyonSatiri.SiraNo;
            mevcutSatir.BarkodNo = revizyonSatiri.BarkodNo;
            mevcutSatir.OlcuResmiPozNo = string.IsNullOrWhiteSpace(revizyonSatiri.OlcuResmiPozNo) ? null : revizyonSatiri.OlcuResmiPozNo;
            mevcutSatir.Aciklama = revizyonSatiri.Aciklama;
            mevcutSatir.CekideGecenSandikNo = yeniKoliNo;
            mevcutSatir.IstenenAdet = revizyonSatiri.IstenenAdet;
            mevcutSatir.BirimId = revizyonSatiri.BirimId;
            mevcutSatir.Remarks = string.IsNullOrWhiteSpace(revizyonSatiri.Remarks) ? null : revizyonSatiri.Remarks;

            if (fiiliPlanlaAyni)
                mevcutSatir.FiiliSandikNo = yeniKoliNo;

            mevcutSatir.DurumId = _durumHesaplaService.HesaplaGenelDurum(mevcutSatir.GridDurumuId, mevcutSatir.UcKDurumuId);
            _durumHesaplaService.HesaplaKalanVeDurum(mevcutSatir);

            var icerikler = await _context.SandikIcerikleri
                .Include(i => i.Sandik)
                .Where(i => i.CekiSatiriId == mevcutSatir.Id)
                .ToListAsync();

            if (icerikler.Count == 0)
            {
                _context.SandikIcerikleri.Add(new SandikIcerik
                {
                    Sandik = hedefSandik,
                    CekiSatiriId = mevcutSatir.Id,
                    KonulanAdet = 0,
                    EksikAdet = 0
                });
            }
            else if (!string.Equals(eskiKoliNo, yeniKoliNo, StringComparison.OrdinalIgnoreCase) &&
                fiiliPlanlaAyni &&
                RevizyonIcerikleriTasinabilirMi(icerikler))
            {
                foreach (var icerik in icerikler)
                    icerik.SandikId = hedefSandik.Id;
            }

            _context.CekiSatirlari.Update(mevcutSatir);
            _context.HareketGecmisleri.Add(new HareketGecmisi
            {
                ProjeId = projeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = mevcutSatir.Id.ToString(),
                ReferansMetni = $"Sıra: {mevcutSatir.SiraNo} - {mevcutSatir.Aciklama}",
                Islem = "Revizyon Satırı Güncellendi",
                KullaniciId = kullaniciId,
                Tarih = TurkeyTime.Now,
                EskiDeger = eskiOzet,
                YeniDeger = CekiSatiriOzeti(mevcutSatir),
                Aciklama = $"Revizyon U satırı uygulandı. Excel satırı: {revizyonSatiri.ExcelSatirNo}"
            });
        }

        private async Task<int> RevizyonSatirlariniSilAsync(int projeId, List<CekiSatiri> satirlar, int kullaniciId)
        {
            var idler = satirlar.Select(s => s.Id).Distinct().ToList();
            if (idler.Count == 0)
                return 0;

            var kilitliIcerikler = await _context.SandikIcerikleri
                .Include(i => i.Sandik)
                .Where(i => i.CekiSatiriId.HasValue &&
                    idler.Contains(i.CekiSatiriId.Value) &&
                    i.Sandik.DurumId == (int)SandikDurum.Sevkedildi)
                .ToListAsync();

            if (kilitliIcerikler.Any())
                throw new Exception($"Revizyon D satırlarından {kilitliIcerikler.Count} tanesi sevk edilmiş sandıkta. Silmek için önce ilgili sandıkların sevk kilidini açın.");

            var transferler = await _context.ProjeTransferleri
                .Where(t => idler.Contains(t.KaynakCekiSatiriId) ||
                    (t.HedefCekiSatiriId.HasValue && idler.Contains(t.HedefCekiSatiriId.Value)))
                .ToListAsync();

            var aktifTransferler = transferler
                .Where(t => t.DurumId == (int)ProjeTransferDurum.Aktif)
                .ToList();

            var disariGidenTransferVar = aktifTransferler.Any(t =>
                idler.Contains(t.KaynakCekiSatiriId) &&
                (!t.HedefCekiSatiriId.HasValue || !idler.Contains(t.HedefCekiSatiriId.Value)));

            if (disariGidenTransferVar)
                throw new Exception("Revizyon D satırlarından bazıları başka bir proje/satıra kaynak olarak verilmiş. Silmek için önce hedef projedeki karşılama/transfer geri alınmalı.");

            var kaynakSatirIdler = aktifTransferler
                .Where(t => t.HedefCekiSatiriId.HasValue &&
                    idler.Contains(t.HedefCekiSatiriId.Value) &&
                    !idler.Contains(t.KaynakCekiSatiriId))
                .Select(t => t.KaynakCekiSatiriId)
                .Distinct()
                .ToList();

            var kaynakSatirlar = kaynakSatirIdler.Count == 0
                ? new Dictionary<int, CekiSatiri>()
                : await _context.CekiSatirlari
                    .Where(s => kaynakSatirIdler.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id);

            foreach (var transfer in aktifTransferler.Where(t => t.HedefCekiSatiriId.HasValue && idler.Contains(t.HedefCekiSatiriId.Value)))
            {
                if (kaynakSatirlar.TryGetValue(transfer.KaynakCekiSatiriId, out var kaynakSatir))
                {
                    kaynakSatir.ProjeGonderilen = Math.Max(kaynakSatir.ProjeGonderilen - transfer.Miktar, 0);
                    kaynakSatir.DurumId = _durumHesaplaService.HesaplaGenelDurum(kaynakSatir.GridDurumuId, kaynakSatir.UcKDurumuId);
                    _durumHesaplaService.HesaplaKalanVeDurum(kaynakSatir);
                    _context.CekiSatirlari.Update(kaynakSatir);
                }

                transfer.DurumId = (int)ProjeTransferDurum.GeriAlindi;
                transfer.IptalTarihi = TurkeyTime.Now;
                transfer.IptalAciklama = "Revizyon D satırı silindiği için transfer pasife alındı.";
                transfer.HedefCekiSatiriId = null;
                _context.ProjeTransferleri.Update(transfer);
            }

            foreach (var transfer in transferler.Where(t => idler.Contains(t.KaynakCekiSatiriId)))
                _context.ProjeTransferleri.Remove(transfer);

            var stokHareketleri = await _context.StokHareketleri
                .Where(h => idler.Contains(h.CekiSatiriId))
                .ToListAsync();
            await RevizyonStokHareketleriniGeriAlAsync(stokHareketleri);

            var icerikler = await _context.SandikIcerikleri
                .Where(i => i.CekiSatiriId.HasValue && idler.Contains(i.CekiSatiriId.Value))
                .ToListAsync();
            _context.SandikIcerikleri.RemoveRange(icerikler);

            foreach (var satir in satirlar)
            {
                _context.HareketGecmisleri.Add(new HareketGecmisi
                {
                    ProjeId = projeId,
                    ReferansTipi = "CekiSatiri",
                    ReferansId = satir.Id.ToString(),
                    ReferansMetni = $"Sıra: {satir.SiraNo} - {satir.Aciklama}",
                    Islem = "Revizyon Satırı Silindi",
                    KullaniciId = kullaniciId,
                    Tarih = TurkeyTime.Now,
                    EskiDeger = CekiSatiriOzeti(satir),
                    Aciklama = $"Revizyon D satırı silindi. Sıra: {satir.SiraNo}, Barkod: {satir.BarkodNo}, Sandık: {satir.CekideGecenSandikNo}"
                });
                _context.CekiSatirlari.Remove(satir);
            }

            await _context.SaveChangesAsync();
            return satirlar.Count;
        }

        private async Task RevizyonStokHareketleriniGeriAlAsync(List<StokHareketi> stokHareketleri)
        {
            if (stokHareketleri.Count == 0)
                return;

            var stokIdler = stokHareketleri.Select(h => h.StokKaydiId).Distinct().ToList();
            var stoklar = await _context.StokKayitlari
                .Where(s => stokIdler.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            foreach (var grup in stokHareketleri.GroupBy(h => h.StokKaydiId))
            {
                if (!stoklar.TryGetValue(grup.Key, out var stok))
                    continue;

                stok.Miktar += grup.Sum(h => Math.Abs(h.Miktar));
                if (stok.Miktar > 0)
                    stok.DurumId = (int)StokDurum.Aktif;

                _context.StokKayitlari.Update(stok);
            }

            _context.StokHareketleri.RemoveRange(stokHareketleri);
        }

        private async Task BosSandiklariTemizleVeDurumlariGuncelleAsync(int projeId)
        {
            var sandiklar = await _context.Sandiklar
                .Include(s => s.SandikIcerikleri)
                .ThenInclude(i => i.CekiSatiri)
                .Where(s => s.ProjeId == projeId)
                .ToListAsync();

            foreach (var sandik in sandiklar)
            {
                if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                    continue;

                if (sandik.SandikIcerikleri.Count == 0)
                {
                    _context.Sandiklar.Remove(sandik);
                    continue;
                }

                var cekiIcerikleri = sandik.SandikIcerikleri.Where(i => i.CekiSatiriId.HasValue).ToList();
                var hepsiTamamlandi = cekiIcerikleri.Count > 0 &&
                    cekiIcerikleri.All(i => i.CekiSatiri?.DurumId == (int)UrunDurum.Tamamlandi);
                var enAzBiriKonuldu = sandik.SandikIcerikleri.Any(i => i.KonulanAdet > 0);

                sandik.DurumId = hepsiTamamlandi
                    ? (int)SandikDurum.Kapandi
                    : enAzBiriKonuldu
                        ? (int)SandikDurum.Hazirlaniyor
                        : (int)SandikDurum.Bos;
            }
        }

        private async Task<Sandik> GetOrCreateSandikAsync(
            int projeId,
            string koliNo,
            Dictionary<string, Sandik> sandikCache,
            Dictionary<string, SandikImportBilgisi> sandikBilgileri)
        {
            var normalizedKoliNo = NormalizeKoliNo(koliNo);
            if (string.IsNullOrWhiteSpace(normalizedKoliNo))
                throw new Exception("Revizyon satırında koli/sandık no boş olamaz.");

            if (sandikCache.TryGetValue(normalizedKoliNo, out var mevcutSandik))
            {
                SandikBilgisiniUygula(mevcutSandik, sandikBilgileri.GetValueOrDefault(normalizedKoliNo));
                return mevcutSandik;
            }

            sandikBilgileri.TryGetValue(normalizedKoliNo, out var bilgi);
            var sandik = new Sandik
            {
                ProjeId = projeId,
                SandikNo = normalizedKoliNo,
                Ad = bilgi?.SandikIsmi,
                AdIngilizce = bilgi?.SandikIsmiIngilizce,
                DurumId = (int)SandikDurum.Hazirlaniyor,
                En = bilgi?.En,
                Boy = bilgi?.Boy,
                Yukseklik = bilgi?.Yukseklik,
                NetKg = bilgi?.NetKg,
                GrossKg = bilgi?.GrossKg
            };

            _context.Sandiklar.Add(sandik);
            sandikCache[normalizedKoliNo] = sandik;
            return sandik;
        }

        private static void SandikBilgisiniUygula(Sandik sandik, SandikImportBilgisi? bilgi)
        {
            if (bilgi == null)
                return;

            if (!string.IsNullOrWhiteSpace(bilgi.SandikIsmi))
                sandik.Ad = bilgi.SandikIsmi;
            if (!string.IsNullOrWhiteSpace(bilgi.SandikIsmiIngilizce))
                sandik.AdIngilizce = bilgi.SandikIsmiIngilizce;
            sandik.En = bilgi.En ?? sandik.En;
            sandik.Boy = bilgi.Boy ?? sandik.Boy;
            sandik.Yukseklik = bilgi.Yukseklik ?? sandik.Yukseklik;
            sandik.NetKg = bilgi.NetKg ?? sandik.NetKg;
            sandik.GrossKg = bilgi.GrossKg ?? sandik.GrossKg;
        }

        private static bool RevizyonIcerikleriTasinabilirMi(List<SandikIcerik> icerikler)
        {
            return icerikler.All(i =>
                i.Sandik.DurumId != (int)SandikDurum.Sevkedildi &&
                i.KonulanAdet == 0 &&
                i.EksikAdet == 0 &&
                i.StokKarsilanan == 0 &&
                i.ProjeKarsilanan == 0 &&
                i.TedarikciKarsilanan == 0);
        }

        private static CekiSatiri? EslesenAnaSatiriBul(
            CiktiSatirImportBilgisi revizyonSatiri,
            Dictionary<int, List<CekiSatiri>> siraGruplari,
            List<CekiSatiri> anaSatirlar)
        {
            var barkod = NormalizeTextKey(revizyonSatiri.BarkodNo);
            var poz = NormalizeTextKey(revizyonSatiri.OlcuResmiPozNo);
            var koli = NormalizeKoliNo(revizyonSatiri.KoliNo);

            var adaylar = new List<CekiSatiri>();

            if (!string.IsNullOrWhiteSpace(barkod))
            {
                adaylar = anaSatirlar
                    .Where(s =>
                        NormalizeTextKey(s.BarkodNo) == barkod &&
                        NormalizeTextKey(s.OlcuResmiPozNo) == poz &&
                        NormalizeKoliNo(s.CekideGecenSandikNo) == koli)
                    .ToList();

                if (adaylar.Count == 1)
                    return adaylar[0];

                adaylar = anaSatirlar
                    .Where(s =>
                        NormalizeTextKey(s.BarkodNo) == barkod &&
                        NormalizeTextKey(s.OlcuResmiPozNo) == poz)
                    .ToList();

                if (adaylar.Count == 1)
                    return adaylar[0];

                adaylar = anaSatirlar
                    .Where(s => NormalizeTextKey(s.BarkodNo) == barkod)
                    .ToList();

                if (adaylar.Count == 1)
                    return adaylar[0];
            }

            if (siraGruplari.TryGetValue(revizyonSatiri.SiraNo, out var siraEslesmeleri) && siraEslesmeleri.Count == 1)
                return siraEslesmeleri[0];

            return adaylar.Count == 1 ? adaylar[0] : null;
        }

        private static CiktiImportBilgisi OkuCiktiImportBilgisi(IXLWorkbook workbook)
        {
            var worksheet = BulCiktiSayfasi(workbook)
                ?? throw new Exception("Excel dosyasında 'ÇIKTI SAYFASI' bulunamadı.");

            var baslik = OkuCiktiBaslikBilgileri(worksheet);
            var baslangicSatir = BulCiktiBaslangicSatiri(worksheet);
            var headerRow = baslangicSatir - 1;
            var pozNoKolon = BulCiktiPozNoKolonu(worksheet, headerRow);
            var checkKolon = BulCiktiCheckKolonu(worksheet, headerRow);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? baslangicSatir;

            var satirlar = new List<CiktiSatirImportBilgisi>();
            for (var r = baslangicSatir; r <= lastRow; r++)
            {
                var checkKodu = ReadRevisionCheckCode(worksheet.Cell(r, checkKolon));
                var barkod = worksheet.Cell(r, 3).GetString().Trim();
                var tanim = worksheet.Cell(r, 4).GetString().Trim();
                var hasData = !string.IsNullOrWhiteSpace(barkod) || !string.IsNullOrWhiteSpace(tanim);

                if (!hasData && string.IsNullOrWhiteSpace(checkKodu))
                    continue;

                var siraNo = TryReadInt(worksheet.Cell(r, 1)) ?? Math.Max(1, r - headerRow);
                var miktar = ReadNullableDecimal(worksheet.Cell(r, 6)) ?? 0;

                satirlar.Add(new CiktiSatirImportBilgisi
                {
                    ExcelSatirNo = r,
                    SiraNo = siraNo,
                    OlcuResmiPozNo = pozNoKolon.HasValue ? worksheet.Cell(r, pozNoKolon.Value).GetString().Trim() : string.Empty,
                    BarkodNo = barkod,
                    Aciklama = tanim,
                    KoliNo = NormalizeKoliNo(worksheet.Cell(r, 5).GetString().Trim()),
                    IstenenAdet = miktar,
                    BirimId = ParseBirimToId(worksheet.Cell(r, 7).GetString().Trim()),
                    Remarks = worksheet.Cell(r, 14).GetString().Trim(),
                    CheckKodu = checkKodu,
                    VeriSatiriMi = hasData
                });
            }

            return new CiktiImportBilgisi
            {
                FbNo = baslik.FbNo,
                Musteri = baslik.Musteri,
                Lokasyon = baslik.Lokasyon,
                Satirlar = satirlar,
                SandikBilgileri = OkuCekiListesiSandikBilgileri(workbook)
            };
        }

        private static IXLWorksheet? BulCiktiSayfasi(IXLWorkbook workbook)
        {
            return workbook.Worksheets.FirstOrDefault(ws => NormalizeExcelText(ws.Name) == "CIKTI SAYFASI")
                ?? workbook.Worksheets.FirstOrDefault(ws =>
                {
                    var name = NormalizeExcelText(ws.Name);
                    return name.Contains("CIKTI") && !name.Contains("YDK") && !name.Contains("YEDEK");
                });
        }

        private static CiktiBaslikBilgisi OkuCiktiBaslikBilgileri(IXLWorksheet worksheet)
        {
            string fbNo = string.Empty;
            for (int r = 1; r <= 10; r++)
            {
                for (int c = 1; c <= 8; c++)
                {
                    var val = NormalizeExcelText(worksheet.Cell(r, c).GetString());
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

            return new CiktiBaslikBilgisi
            {
                FbNo = fbNo,
                Musteri = worksheet.Cell(2, 6).GetString().Trim(),
                Lokasyon = worksheet.Cell(4, 6).GetString().Trim()
            };
        }

        private static int BulCiktiBaslangicSatiri(IXLWorksheet worksheet)
        {
            for (int r = 1; r <= 20; r++)
            {
                if (TryReadInt(worksheet.Cell(r, 1)).HasValue)
                    return r;
            }

            return 6;
        }

        private static int BulCiktiCheckKolonu(IXLWorksheet worksheet, int headerRow)
        {
            if (headerRow <= 0)
                return 11;

            var lastColumn = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, 80);
            for (int c = 1; c <= lastColumn; c++)
            {
                var header = NormalizeExcelText(worksheet.Cell(headerRow, c).GetString());
                if (header.Contains("CHECK") || header.Contains("KONTROL"))
                    return c;
            }

            return 11;
        }

        private static string ReadRevisionCheckCode(IXLCell cell)
        {
            var value = NormalizeExcelText(cell.GetString());
            return value switch
            {
                "A" => "A",
                "U" => "U",
                "D" => "D",
                _ => string.Empty
            };
        }

        private static int? TryReadInt(IXLCell cell)
        {
            if (cell.TryGetValue<int>(out var intValue))
                return intValue;

            var text = cell.GetString().Trim();
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
                return (int)decimalValue;

            return null;
        }

        private static string CekiSatiriOzeti(CekiSatiri satir)
        {
            return $"Sıra:{satir.SiraNo}; Poz:{satir.OlcuResmiPozNo}; Barkod:{satir.BarkodNo}; Tanım:{satir.Aciklama}; Koli:{satir.CekideGecenSandikNo}; Miktar:{satir.IstenenAdet}; Birim:{satir.BirimId}; Açıklama:{satir.Remarks}";
        }

        private static string RevizyonSatirOzeti(CiktiSatirImportBilgisi satir)
        {
            return $"Sıra:{satir.SiraNo}; Poz:{satir.OlcuResmiPozNo}; Barkod:{satir.BarkodNo}; Tanım:{satir.Aciklama}; Koli:{satir.KoliNo}; Miktar:{satir.IstenenAdet}; Birim:{satir.BirimId}; Açıklama:{satir.Remarks}";
        }

        private static string NormalizeTextKey(string? value)
        {
            return NormalizeExcelText(value).Replace(" ", string.Empty);
        }

        private static string CleanFileName(string fileName)
        {
            var safeName = Path.GetFileName(fileName);
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(invalidChar, '_');
            return string.IsNullOrWhiteSpace(safeName) ? "revizyon.xlsx" : safeName;
        }

        private sealed class CiktiImportBilgisi
        {
            public string FbNo { get; set; } = string.Empty;
            public string? Musteri { get; set; }
            public string? Lokasyon { get; set; }
            public List<CiktiSatirImportBilgisi> Satirlar { get; set; } = new();
            public Dictionary<string, SandikImportBilgisi> SandikBilgileri { get; set; } = new();
        }

        private sealed class CiktiBaslikBilgisi
        {
            public string FbNo { get; set; } = string.Empty;
            public string? Musteri { get; set; }
            public string? Lokasyon { get; set; }
        }

        private sealed class CiktiSatirImportBilgisi
        {
            public int ExcelSatirNo { get; set; }
            public int SiraNo { get; set; }
            public string? OlcuResmiPozNo { get; set; }
            public string BarkodNo { get; set; } = string.Empty;
            public string Aciklama { get; set; } = string.Empty;
            public string KoliNo { get; set; } = string.Empty;
            public decimal IstenenAdet { get; set; }
            public int BirimId { get; set; }
            public string? Remarks { get; set; }
            public string CheckKodu { get; set; } = string.Empty;
            public bool VeriSatiriMi { get; set; }
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
                        En = enMm,
                        Boy = boyMm,
                        Yukseklik = yukseklikMm,
                        NetKg = netKg,
                        GrossKg = grossKg
                    };
                }
            }

            foreach (var item in OkuPackingListSandikIngilizceAdlari(workbook))
            {
                if (sonuc.TryGetValue(item.Key, out var bilgi))
                {
                    bilgi.SandikIsmiIngilizce = item.Value;
                }
                else
                {
                    sonuc[item.Key] = new SandikImportBilgisi
                    {
                        SandikNo = item.Key,
                        SandikIsmiIngilizce = item.Value
                    };
                }
            }

            return sonuc;
        }

        private static Dictionary<string, string> OkuPackingListSandikIngilizceAdlari(IXLWorkbook workbook)
        {
            var sonuc = new Dictionary<string, string>();
            var worksheet = workbook.Worksheets.FirstOrDefault(ws => NormalizeExcelText(ws.Name).Contains("PACKING LIST"));
            var usedRange = worksheet?.RangeUsed();

            if (worksheet == null || usedRange == null)
                return sonuc;

            if (!TryFindPackingListColumns(worksheet, usedRange, out var headerRow, out var caseNumberColumn, out var caseDescriptionColumn))
                return sonuc;

            var lastRow = usedRange.LastRow().RowNumber();
            for (var row = headerRow + 1; row <= lastRow; row++)
            {
                var caseNumber = worksheet.Cell(row, caseNumberColumn).GetString().Trim();
                var caseDescription = worksheet.Cell(row, caseDescriptionColumn).GetString().Trim();

                if (string.IsNullOrWhiteSpace(caseNumber) || string.IsNullOrWhiteSpace(caseDescription))
                    continue;

                var sandikNo = ExtractPackingListSandikNo(caseNumber);
                if (string.IsNullOrWhiteSpace(sandikNo))
                    continue;

                sonuc[sandikNo] = caseDescription;
            }

            return sonuc;
        }

        private static bool TryFindPackingListColumns(
            IXLWorksheet worksheet,
            IXLRange usedRange,
            out int headerRow,
            out int caseNumberColumn,
            out int caseDescriptionColumn)
        {
            headerRow = 0;
            caseNumberColumn = 0;
            caseDescriptionColumn = 0;

            var firstRow = usedRange.FirstRow().RowNumber();
            var lastRow = Math.Min(usedRange.LastRow().RowNumber(), firstRow + 80);
            var firstColumn = usedRange.FirstColumn().ColumnNumber();
            var lastColumn = usedRange.LastColumn().ColumnNumber();

            for (var row = firstRow; row <= lastRow; row++)
            {
                for (var column = firstColumn; column <= lastColumn; column++)
                {
                    var header = NormalizeExcelText(worksheet.Cell(row, column).GetString());
                    if (string.IsNullOrWhiteSpace(header))
                        continue;

                    if (caseNumberColumn == 0 && header.Contains("CASE") && (header.Contains("NUMBER") || header.Contains("NO")))
                    {
                        caseNumberColumn = column;
                        headerRow = Math.Max(headerRow, row);
                    }

                    if (caseDescriptionColumn == 0
                        && (header.Contains("CONTENT")
                            || header.Contains("CASE DESCRIPTION")
                            || header.Contains("CASE NAME")))
                    {
                        caseDescriptionColumn = column;
                        headerRow = Math.Max(headerRow, row);
                    }
                }

                if (caseNumberColumn > 0 && caseDescriptionColumn > 0)
                    return true;
            }

            return false;
        }

        private static string ExtractPackingListSandikNo(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var text = value.Trim();
            var trailingDigits = new string(text.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());

            return string.IsNullOrWhiteSpace(trailingDigits)
                ? NormalizeKoliNo(text)
                : NormalizeKoliNo(trailingDigits);
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

        private static int? BulCiktiPozNoKolonu(IXLWorksheet worksheet, int headerRow)
        {
            if (headerRow <= 0)
                return null;

            var lastColumn = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, 80);
            int? fallbackColumn = null;

            for (int c = 4; c <= lastColumn; c++)
            {
                var text = NormalizeExcelText(worksheet.Cell(headerRow, c).GetString());
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var isPozNo = (text.Contains("POZ") || text.Contains("POS")) && text.Contains("NO");
                if (!isPozNo)
                    continue;

                fallbackColumn = c;
                if (!text.Contains("OLCU") && !text.Contains("DIMENSIONAL") && !text.Contains("DRW"))
                    return c;
            }

            return fallbackColumn;
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

        /// <summary>
        /// Eski dönüşüm metodu — artık kullanılmıyor. Değerler çekideki gibi mm olarak saklanır.
        /// </summary>
        // private static decimal? MmToCm(decimal? value) => value.HasValue ? Math.Round(value.Value / 10m, 2) : null;

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
            public string? SandikIsmiIngilizce { get; set; }
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
