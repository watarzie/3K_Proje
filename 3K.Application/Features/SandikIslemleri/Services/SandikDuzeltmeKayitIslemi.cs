using System.Text.Json;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Services
{
    /// <summary>
    /// Sevk sonrası düzeltme kaydının açılış/tamamlanma adımları. Kayıt, çağıran handler'ın aynı kaydetme
    /// adımında yazılır; transaction sınırını handler yönetir.
    /// </summary>
    internal sealed class SandikDuzeltmeKayitIslemi
    {
        private readonly IUnitOfWork _unitOfWork;

        public SandikDuzeltmeKayitIslemi(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>Sandıkta açık düzeltme kaydı yoksa açılış görüntüsüyle birlikte yenisini ekler.</summary>
        public async Task AcAsync(Sandik sandik, string? gerekce, int kullaniciId, CancellationToken cancellationToken)
        {
            if (await AcikKayitBulAsync(sandik.Id) != null)
                return;

            var goruntu = await GoruntuAlAsync(sandik, cancellationToken);
            await _unitOfWork.GetRepository<SandikDuzeltmeKaydi>().AddAsync(new SandikDuzeltmeKaydi
            {
                ProjeId = sandik.ProjeId,
                SandikId = sandik.Id,
                SandikNo = sandik.SandikNo,
                Gerekce = (gerekce ?? string.Empty).Trim(),
                AcanKullaniciId = kullaniciId,
                AcilisTarihi = TurkeyTime.Now,
                AcilisGoruntusu = JsonSerializer.Serialize(goruntu)
            });
        }

        /// <summary>
        /// Açık kaydı kapatır ve fark özetini döndürür. Düzeltme bu özellikten önce açılmışsa kayıt yoktur; null döner.
        /// </summary>
        public async Task<IReadOnlyList<string>?> TamamlaAsync(
            Sandik sandik, int kullaniciId, ILookupCacheService lookupCache, CancellationToken cancellationToken)
        {
            var kayit = await AcikKayitBulAsync(sandik.Id);
            if (kayit == null)
                return null;

            var fark = await FarkAsync(kayit, sandik, lookupCache, cancellationToken);
            kayit.TamamlayanKullaniciId = kullaniciId;
            kayit.TamamlanmaTarihi = TurkeyTime.Now;
            kayit.FarkOzeti = string.Join('\n', fark);
            _unitOfWork.GetRepository<SandikDuzeltmeKaydi>().Update(kayit);
            return fark;
        }

        /// <summary>Açılış görüntüsü ile sandığın şu anki hali arasındaki fark.</summary>
        public async Task<IReadOnlyList<string>> FarkAsync(
            SandikDuzeltmeKaydi kayit, Sandik sandik, ILookupCacheService lookupCache, CancellationToken cancellationToken)
        {
            var once = JsonSerializer.Deserialize<SandikDuzeltmeGoruntusu>(kayit.AcilisGoruntusu) ?? BosGoruntu(sandik);
            var sonra = await GoruntuAlAsync(sandik, cancellationToken);
            return SandikDuzeltmeFarkKurali.Hesapla(
                once,
                sonra,
                id => lookupCache.GetDeger<LookupGridDurum>(id),
                id => lookupCache.GetDeger<LookupUcKDurum>(id),
                id => lookupCache.GetDeger<LookupSandikTipi>(id));
        }

        public async Task<SandikDuzeltmeKaydi?> AcikKayitBulAsync(int sandikId)
        {
            return (await _unitOfWork.GetRepository<SandikDuzeltmeKaydi>()
                    .FindAsync(k => k.SandikId == sandikId && k.TamamlanmaTarihi == null))
                .FirstOrDefault();
        }

        private async Task<SandikDuzeltmeGoruntusu> GoruntuAlAsync(Sandik sandik, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var icerikler = (await _unitOfWork.GetRepository<SandikIcerik>()
                    .FindAsync(i => i.SandikId == sandik.Id))
                .ToList();
            var satirIds = icerikler
                .Where(i => i.CekiSatiriId.HasValue)
                .Select(i => i.CekiSatiriId!.Value)
                .Distinct()
                .ToList();
            var satirlar = satirIds.Count == 0
                ? new Dictionary<int, CekiSatiri>()
                : (await _unitOfWork.GetRepository<CekiSatiri>().FindAsync(s => satirIds.Contains(s.Id)))
                    .ToDictionary(s => s.Id);

            return BosGoruntu(sandik) with
            {
                Kalemler = icerikler
                    .OrderBy(i => i.Id)
                    .Select(i =>
                    {
                        var satir = i.CekiSatiriId is int id ? satirlar.GetValueOrDefault(id) : null;
                        return new SandikDuzeltmeKalemi
                        {
                            SandikIcerikId = i.Id,
                            CekiSatiriId = i.CekiSatiriId,
                            BarkodNo = satir?.BarkodNo ?? i.BarkodNo ?? string.Empty,
                            Urun = satir?.Aciklama ?? i.Isim ?? string.Empty,
                            Tahsis = i.TahsisMiktari > 0 ? i.TahsisMiktari : i.Miktar,
                            Konulan = i.KonulanAdet,
                            GridDurumuId = satir?.GridDurumuId,
                            UcKDurumuId = satir?.UcKDurumuId,
                            UrunBilgisi = i.UrunBilgisi
                        };
                    })
                    .ToList()
            };
        }

        private static SandikDuzeltmeGoruntusu BosGoruntu(Sandik sandik) => new()
        {
            SandikNo = sandik.SandikNo,
            Ad = sandik.Ad,
            TipId = sandik.TipId,
            En = sandik.En,
            Boy = sandik.Boy,
            Yukseklik = sandik.Yukseklik,
            NetKg = sandik.NetKg,
            GrossKg = sandik.GrossKg
        };
    }
}
