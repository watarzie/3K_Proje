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

        public async Task<Ceki> CekiYukleAsync(int projeId, Stream excelDosya, string dosyaAdi)
        {
            // 1. Dosyayı kaydet
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", projeId.ToString());
            Directory.CreateDirectory(uploadsDir);
            var dosyaYolu = Path.Combine(uploadsDir, dosyaAdi);

            using (var fileStream = new FileStream(dosyaYolu, FileMode.Create))
            {
                await excelDosya.CopyToAsync(fileStream);
            }

            // 2. Çeki kaydı oluştur
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var ceki = new Ceki
            {
                ProjeId = projeId,
                OrijinalDosyaYolu = dosyaYolu
            };
            await cekiRepo.AddAsync(ceki);
            await _unitOfWork.SaveChangesAsync();

            // 3. Excel dosyasını oku
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = new List<CekiSatiri>();

            excelDosya.Position = 0; // Stream'i başa sar
            using (var workbook = new XLWorkbook(excelDosya))
            {
                var worksheet = workbook.Worksheets.First();
                var rows = worksheet.RowsUsed().Skip(1); // İlk satır başlık

                int siraNo = 1;
                foreach (var row in rows)
                {
                    var satir = new CekiSatiri
                    {
                        CekiId = ceki.Id,
                        SiraNo = siraNo++,
                        BarkodNo = row.Cell(1).GetString().Trim(),
                        Aciklama = row.Cell(2).GetString().Trim(),
                        CekideGecenSandikNo = row.Cell(3).GetString().Trim(),
                        IstenenAdet = (int)(row.Cell(4).IsEmpty() ? 0 : row.Cell(4).GetDouble()),
                        Birim = row.Cell(5).GetString().Trim(),
                        Remarks = row.Cell(6).GetString().Trim(),
                        Durum = UrunDurum.Bekliyor,
                        FiiliSandikNo = row.Cell(3).GetString().Trim() // Başlangıçta fiili = çekideki
                    };
                    satirlar.Add(satir);
                    await cekiSatiriRepo.AddAsync(satir);
                }
            }
            await _unitOfWork.SaveChangesAsync();

            // 4. Benzersiz sandık numaralarını grupla ve sandık kayıtları oluştur
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var benzersizSandiklar = satirlar.GroupBy(s => s.CekideGecenSandikNo);

            foreach (var grup in benzersizSandiklar)
            {
                if (string.IsNullOrWhiteSpace(grup.Key)) continue;

                // Sandık kaydı oluştur
                var sandik = new Sandik
                {
                    ProjeId = projeId,
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
