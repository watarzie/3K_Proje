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
                .Where(s => s.ProjeId == projeId)
                .OrderBy(s => s.SandikNo)
                .ToListAsync();

            var etkinIcerikler = await GetEtkinSandikIcerikleriAsync(
                sandiklar,
                personelDetaylariniYukle: false,
                projeSandiklari: sandiklar);

            foreach (var sandik in sandiklar)
            {
                sandik.SandikIcerikleri = etkinIcerikler
                    .GetValueOrDefault(sandik.Id, new List<SandikIcerik>());
            }

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

            var eskiIcerikler = (await sandikIcerikRepo.FindAsync(
                    si => si.CekiSatiriId == cekiSatiriId))
                .ToList();

            // Bu eski metot yalnızca ürünün tamamını taşır. Parçalı tahsislerde hangi
            // miktarın taşınacağı belirsiz olduğundan veri kaybı yerine güvenli biçimde durur.
            if (eskiIcerikler.Count > 1)
                return false;

            var eskiIcerik = eskiIcerikler.SingleOrDefault();

            var cekiSatiri = await cekiSatiriRepo.GetByIdAsync(cekiSatiriId);

            if (eskiIcerik != null)
                sandikIcerikRepo.Remove(eskiIcerik);

            var yeniIcerik = new SandikIcerik
            {
                SandikId = yeniSandikId,
                CekiSatiriId = cekiSatiriId,
                TahsisMiktari = eskiIcerik?.TahsisMiktari > 0
                    ? eskiIcerik.TahsisMiktari
                    : cekiSatiri?.IstenenAdet ?? eskiIcerik?.KonulanAdet ?? 0,
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

            var etkinIcerikler = await GetEtkinSandikIcerikleriAsync(
                new[] { sandik },
                personelDetaylariniYukle: true);

            return etkinIcerikler.GetValueOrDefault(sandik.Id, new List<SandikIcerik>())
                .OrderBy(i => i.CekiSatiri?.SiraNo ?? int.MaxValue)
                .ThenBy(i => i.Id)
                .ToList();
        }

        public async Task<IReadOnlyDictionary<int, IReadOnlyCollection<SandikIcerik>>> GetEtkinSandikIcerikleriAsync(
            IEnumerable<int> sandikIds,
            CancellationToken cancellationToken = default)
        {
            var tekilSandikIds = sandikIds.Distinct().ToList();
            if (tekilSandikIds.Count == 0)
                return new Dictionary<int, IReadOnlyCollection<SandikIcerik>>();

            var sandiklar = await _context.Sandiklar
                .AsNoTracking()
                .Where(s => tekilSandikIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var etkinIcerikler = await GetEtkinSandikIcerikleriAsync(
                sandiklar,
                personelDetaylariniYukle: false,
                cancellationToken: cancellationToken);

            return etkinIcerikler.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyCollection<SandikIcerik>)pair.Value);
        }

        /// <summary>
        /// Yeni kayıtlardaki fiziksel SandikIcerik tahsislerini korur. Eski kayıtlarda tahsis
        /// hiç oluşmamışsa, benzersiz proje + sandık numarası eşleşmesini yalnız okuma amacıyla
        /// sentetik içerik olarak ekler; veritabanında örtük bir tahsis oluşturmaz.
        /// </summary>
        private async Task<Dictionary<int, List<SandikIcerik>>> GetEtkinSandikIcerikleriAsync(
            IReadOnlyCollection<Sandik> sandiklar,
            bool personelDetaylariniYukle,
            IReadOnlyCollection<Sandik>? projeSandiklari = null,
            CancellationToken cancellationToken = default)
        {
            var sonuc = sandiklar.ToDictionary(s => s.Id, _ => new List<SandikIcerik>());
            if (sandiklar.Count == 0)
                return sonuc;

            var sandikIds = sandiklar.Select(s => s.Id).ToList();
            IQueryable<SandikIcerik> icerikQuery = _context.SandikIcerikleri
                .AsNoTracking()
                .Where(i => sandikIds.Contains(i.SandikId));

            icerikQuery = personelDetaylariniYukle
                ? icerikQuery
                    .Include(i => i.CekiSatiri)
                        .ThenInclude(cs => cs!.Paketleyen)
                    .Include(i => i.CekiSatiri)
                        .ThenInclude(cs => cs!.KontrolEden)
                : icerikQuery.Include(i => i.CekiSatiri);

            var fizikselIcerikler = await icerikQuery.ToListAsync(cancellationToken);
            foreach (var icerik in fizikselIcerikler)
                sonuc[icerik.SandikId].Add(icerik);

            var projeIds = sandiklar.Select(s => s.ProjeId).Distinct().ToList();
            var seciliSandikNolari = sandiklar
                .Select(s => NormalizeSandikNo(s.SandikNo).ToUpperInvariant())
                .Where(sandikNo => !string.IsNullOrWhiteSpace(sandikNo))
                .Distinct()
                .ToList();

            if (seciliSandikNolari.Count == 0)
                return sonuc;

            IQueryable<CekiSatiri> baglantisizSatirQuery = _context.CekiSatirlari
                .AsNoTracking()
                .Include(cs => cs.Ceki)
                .Where(cs => projeIds.Contains(cs.Ceki.ProjeId))
                .Where(cs => !_context.SandikIcerikleri.Any(i => i.CekiSatiriId == cs.Id))
                .Where(cs => seciliSandikNolari.Contains(
                    (cs.FiiliSandikNo != null && cs.FiiliSandikNo.Trim() != string.Empty
                        ? cs.FiiliSandikNo
                        : cs.CekideGecenSandikNo ?? string.Empty)
                    .Trim()
                    .ToUpper()));

            if (personelDetaylariniYukle)
            {
                baglantisizSatirQuery = baglantisizSatirQuery
                    .Include(cs => cs.Paketleyen)
                    .Include(cs => cs.KontrolEden);
            }

            var baglantisizSatirlar = await baglantisizSatirQuery.ToListAsync(cancellationToken);
            var benzersizlikAdaylari = projeSandiklari?.Select(s => new SandikKimligi(s.Id, s.ProjeId, s.SandikNo)).ToList();
            if (benzersizlikAdaylari == null)
            {
                var projeSandikKayitlari = await _context.Sandiklar
                    .AsNoTracking()
                    .Where(s => projeIds.Contains(s.ProjeId))
                    .Select(s => new { s.Id, s.ProjeId, s.SandikNo })
                    .ToListAsync(cancellationToken);
                benzersizlikAdaylari = projeSandikKayitlari
                    .Select(s => new SandikKimligi(s.Id, s.ProjeId, s.SandikNo))
                    .ToList();
            }

            var seciliSandikIds = sandikIds.ToHashSet();
            var benzersizSandikIdsByKey = benzersizlikAdaylari
                .Where(s => !string.IsNullOrWhiteSpace(s.SandikNo))
                .GroupBy(s => GetSandikAnahtari(s.ProjeId, s.SandikNo))
                .Where(group => group.Count() == 1)
                .Select(group => new { group.Key, SandikId = group.Single().Id })
                .Where(item => seciliSandikIds.Contains(item.SandikId))
                .ToDictionary(item => item.Key, item => item.SandikId);

            foreach (var satir in baglantisizSatirlar)
            {
                var sandikNo = GetCekiSatiriSandikNo(satir);
                var sandikAnahtari = GetSandikAnahtari(satir.Ceki.ProjeId, sandikNo);
                if (string.IsNullOrWhiteSpace(sandikNo) ||
                    !benzersizSandikIdsByKey.TryGetValue(sandikAnahtari, out var sandikId))
                    continue;

                sonuc[sandikId].Add(CekiSatirindanOkumaIcerigiOlustur(sandikId, satir));
            }

            return sonuc;
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
                TahsisMiktari = satir.IstenenAdet,
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

        private static string GetSandikAnahtari(int projeId, string? sandikNo)
        {
            return $"{projeId}:{NormalizeSandikNo(sandikNo).ToUpperInvariant()}";
        }

        private sealed record SandikKimligi(int Id, int ProjeId, string SandikNo);
    }
}
