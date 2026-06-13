using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    public class SandikService : ISandikService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public SandikService(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<IEnumerable<Sandik>> GetProjeSandiklariAsync(int projeId)
        {
            var sandiklar = await _context.Sandiklar
                .AsNoTracking()
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                .Where(s => s.ProjeId == projeId)
                .OrderBy(s => s.SandikNo)
                .ToListAsync();

            await CekiSatirlariniSandikIcerigineYansitAsync(projeId, sandiklar);

            return sandiklar;
        }

        public async Task<Sandik?> GetSandikDetayAsync(int sandikId)
        {
            return await _context.Sandiklar
                .AsNoTracking()
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                        .ThenInclude(cs => cs.Paketleyen)
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                        .ThenInclude(cs => cs.KontrolEden)
                .FirstOrDefaultAsync(s => s.Id == sandikId);
        }

        public async Task<Sandik?> GetSandikByNoAsync(int projeId, string sandikNo)
        {
            return await _context.Sandiklar
                .FirstOrDefaultAsync(s => s.ProjeId == projeId && s.SandikNo == sandikNo);
        }

        public async Task<Sandik> SandikOlusturAsync(int projeId, string sandikNo, string depoLokasyonu = "Belirsiz")
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = new Sandik
            {
                ProjeId = projeId,
                SandikNo = sandikNo,
                DurumId = (int)SandikDurum.Hazirlaniyor,
                DepoLokasyonId = (int)DepoLokasyon.Belirsiz
            };
            await sandikRepo.AddAsync(sandik);
            await _unitOfWork.SaveChangesAsync();
            return sandik;
        }

        public async Task<bool> SandikDegistirAsync(int cekiSatiriId, int yeniSandikId, int kullaniciId, int projeId)
        {
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();

            var eskiIcerikler = await sandikIcerikRepo.FindAsync(si => si.CekiSatiriId == cekiSatiriId);
            var eskiIcerik = eskiIcerikler.FirstOrDefault();

            if (eskiIcerik != null)
                sandikIcerikRepo.Remove(eskiIcerik);

            var yeniIcerik = new SandikIcerik
            {
                SandikId = yeniSandikId,
                CekiSatiriId = cekiSatiriId,
                KonulanAdet = eskiIcerik?.KonulanAdet ?? 0,
                EksikAdet = eskiIcerik?.EksikAdet ?? 0
            };
            await sandikIcerikRepo.AddAsync(yeniIcerik);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<SandikIcerik>> GetSandikIcerikAsync(int sandikId)
        {
            var sandik = await _context.Sandiklar
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sandikId);

            if (sandik == null)
                return Enumerable.Empty<SandikIcerik>();

            var icerikler = await _context.SandikIcerikleri
                .AsNoTracking()
                .Include(si => si.CekiSatiri)
                    .ThenInclude(cs => cs.Paketleyen)
                .Include(si => si.CekiSatiri)
                    .ThenInclude(cs => cs.KontrolEden)
                .Where(si => si.SandikId == sandikId)
                .ToListAsync();

            var projeIcerikliCekiSatiriIds = await _context.SandikIcerikleri
                .AsNoTracking()
                .Where(i => i.CekiSatiriId.HasValue && i.Sandik.ProjeId == sandik.ProjeId)
                .Select(i => i.CekiSatiriId!.Value)
                .Distinct()
                .ToListAsync();

            var sandikNo = NormalizeSandikNo(sandik.SandikNo);
            if (string.IsNullOrWhiteSpace(sandikNo))
                return icerikler;

            var baglantisizSatirlar = await _context.CekiSatirlari
                .AsNoTracking()
                .Include(cs => cs.Paketleyen)
                .Include(cs => cs.KontrolEden)
                .Where(cs => cs.Ceki.ProjeId == sandik.ProjeId)
                .Where(cs => !projeIcerikliCekiSatiriIds.Contains(cs.Id))
                .ToListAsync();

            icerikler.AddRange(baglantisizSatirlar
                .Where(cs => string.Equals(GetCekiSatiriSandikNo(cs), sandikNo, StringComparison.OrdinalIgnoreCase))
                .Select(cs => CekiSatirindanOkumaIcerigiOlustur(sandik.Id, cs)));

            return icerikler
                .OrderBy(i => i.CekiSatiri?.SiraNo ?? int.MaxValue)
                .ThenBy(i => i.Id)
                .ToList();
        }

        private async Task CekiSatirlariniSandikIcerigineYansitAsync(int projeId, List<Sandik> sandiklar)
        {
            if (sandiklar.Count == 0)
                return;

            var sandikMap = sandiklar
                .Where(s => !string.IsNullOrWhiteSpace(s.SandikNo))
                .GroupBy(s => NormalizeSandikNo(s.SandikNo), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            if (sandikMap.Count == 0)
                return;

            var mevcutCekiSatiriIds = sandiklar
                .SelectMany(s => s.SandikIcerikleri)
                .Where(i => i.CekiSatiriId.HasValue)
                .Select(i => i.CekiSatiriId!.Value)
                .ToHashSet();

            var baglantisizSatirlar = await _context.CekiSatirlari
                .AsNoTracking()
                .Where(cs => cs.Ceki.ProjeId == projeId)
                .Where(cs => !mevcutCekiSatiriIds.Contains(cs.Id))
                .ToListAsync();

            foreach (var satir in baglantisizSatirlar)
            {
                var sandikNo = GetCekiSatiriSandikNo(satir);
                if (string.IsNullOrWhiteSpace(sandikNo) || !sandikMap.TryGetValue(sandikNo, out var sandik))
                    continue;

                sandik.SandikIcerikleri.Add(CekiSatirindanOkumaIcerigiOlustur(sandik.Id, satir));
            }
        }

        private static SandikIcerik CekiSatirindanOkumaIcerigiOlustur(int sandikId, CekiSatiri satir)
        {
            var konulanAdet = Math.Max(
                satir.GelenMiktar + satir.StokKarsilanan + satir.ProjeKarsilanan + satir.TedarikciKarsilanan - satir.ProjeGonderilen,
                0);

            return new SandikIcerik
            {
                Id = -satir.Id,
                SandikId = sandikId,
                CekiSatiriId = satir.Id,
                CekiSatiri = satir,
                KonulanAdet = konulanAdet,
                EksikAdet = satir.KalanMiktar,
                BarkodNo = satir.BarkodNo,
                Isim = satir.Aciklama,
                Miktar = satir.IstenenAdet,
                BirimId = satir.BirimId
            };
        }

        private static string GetCekiSatiriSandikNo(CekiSatiri satir)
        {
            return NormalizeSandikNo(string.IsNullOrWhiteSpace(satir.FiiliSandikNo)
                ? satir.CekideGecenSandikNo
                : satir.FiiliSandikNo);
        }

        private static string NormalizeSandikNo(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
