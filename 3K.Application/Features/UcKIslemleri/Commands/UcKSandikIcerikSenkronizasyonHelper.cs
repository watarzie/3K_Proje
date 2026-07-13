using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// Çeki satırındaki kümülatif 3K toplamlarını sandık tahsislerine yalnızca fark kadar yansıtır.
    /// Mevcut dağılımı korur; proje toplamını hiçbir zaman tek bir SandikIcerik kaydına yazmaz.
    /// </summary>
    internal static class UcKSandikIcerikSenkronizasyonHelper
    {
        public static Task<Result<List<SandikIcerik>>> SenkronizeAsync(
            IUnitOfWork unitOfWork,
            CekiSatiri satir,
            int? tercihEdilenSandikIcerikId = null)
        {
            var repo = unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = repo.Queryable()
                .Where(i => i.CekiSatiriId == satir.Id)
                .OrderBy(i => i.Id)
                .ToList();

            if (!icerikler.Any())
                return Task.FromResult(Result<List<SandikIcerik>>.Success(icerikler));

            SandikIcerik? tercihEdilen = null;
            if (tercihEdilenSandikIcerikId.HasValue)
            {
                tercihEdilen = icerikler.FirstOrDefault(i => i.Id == tercihEdilenSandikIcerikId.Value);
                if (tercihEdilen == null)
                    return Task.FromResult(Result<List<SandikIcerik>>.Failure("Seçilen sandık içeriği bu ürüne ait değil.", 400));
            }

            var hedefKonulan = Math.Max(
                satir.GelenMiktar + satir.StokKarsilanan + satir.ProjeKarsilanan + satir.TedarikciKarsilanan - satir.ProjeGonderilen,
                0);
            var toplamKapasite = icerikler.Sum(i => TahsisMiktari(satir, i, icerikler.Count));
            if (hedefKonulan > toplamKapasite)
            {
                return Task.FromResult(Result<List<SandikIcerik>>.Failure(
                    $"3K toplamı ({hedefKonulan}) sandıklara tahsis edilen toplam miktarı ({toplamKapasite}) aşamaz."));
            }

            Esitle(
                icerikler,
                hedefKonulan,
                i => i.KonulanAdet,
                (i, value) => i.KonulanAdet = value,
                i => TahsisMiktari(satir, i, icerikler.Count),
                tercihEdilen);

            Esitle(
                icerikler,
                satir.StokKarsilanan,
                i => i.StokKarsilanan,
                (i, value) => i.StokKarsilanan = value,
                i => Math.Max(i.KonulanAdet - i.ProjeKarsilanan - i.TedarikciKarsilanan, 0),
                tercihEdilen);

            Esitle(
                icerikler,
                satir.ProjeKarsilanan,
                i => i.ProjeKarsilanan,
                (i, value) => i.ProjeKarsilanan = value,
                i => Math.Max(i.KonulanAdet - i.StokKarsilanan - i.TedarikciKarsilanan, 0),
                tercihEdilen);

            Esitle(
                icerikler,
                satir.TedarikciKarsilanan,
                i => i.TedarikciKarsilanan,
                (i, value) => i.TedarikciKarsilanan = value,
                i => Math.Max(i.KonulanAdet - i.StokKarsilanan - i.ProjeKarsilanan, 0),
                tercihEdilen);

            foreach (var icerik in icerikler)
            {
                icerik.EksikAdet = Math.Max(TahsisMiktari(satir, icerik, icerikler.Count) - icerik.KonulanAdet, 0);
                repo.Update(icerik);
            }

            return Task.FromResult(Result<List<SandikIcerik>>.Success(icerikler));
        }

        public static async Task<Result<SandikIcerik?>> GetSeciliIcerikAsync(
            IUnitOfWork unitOfWork,
            int cekiSatiriId,
            int? sandikIcerikId)
        {
            if (!sandikIcerikId.HasValue)
                return Result<SandikIcerik?>.Success(null);

            var icerik = (await unitOfWork.GetRepository<SandikIcerik>()
                    .FindAsync(i => i.Id == sandikIcerikId.Value && i.CekiSatiriId == cekiSatiriId))
                .FirstOrDefault();
            return icerik != null
                ? Result<SandikIcerik?>.Success(icerik)
                : Result<SandikIcerik?>.Failure("Seçilen sandık içeriği bu ürüne ait değil.", 400);
        }

        public static decimal TahsisMiktari(CekiSatiri satir, SandikIcerik icerik, int tahsisSayisi)
        {
            if (icerik.TahsisMiktari > 0)
                return icerik.TahsisMiktari;

            return tahsisSayisi <= 1
                ? Math.Max(satir.IstenenAdet, 0)
                : Math.Max(icerik.KonulanAdet, 0);
        }

        public static decimal ToplamdanSeciliTahsisPayi(
            IUnitOfWork unitOfWork,
            CekiSatiri satir,
            SandikIcerik seciliIcerik,
            decimal toplam)
        {
            var icerikler = unitOfWork.GetRepository<SandikIcerik>()
                .Queryable()
                .Where(i => i.CekiSatiriId == satir.Id)
                .ToList();
            var secili = icerikler.FirstOrDefault(i => i.Id == seciliIcerik.Id) ?? seciliIcerik;
            var seciliTahsis = TahsisMiktari(satir, secili, icerikler.Count);
            var toplamTahsis = icerikler.Sum(i => TahsisMiktari(satir, i, icerikler.Count));

            return SandikTahsisHelper.ToplamdanTahsisPayi(toplam, seciliTahsis, toplamTahsis);
        }

        private static void Esitle(
            IReadOnlyCollection<SandikIcerik> icerikler,
            decimal hedefToplam,
            Func<SandikIcerik, decimal> getValue,
            Action<SandikIcerik, decimal> setValue,
            Func<SandikIcerik, decimal> getCapacity,
            SandikIcerik? tercihEdilen)
        {
            hedefToplam = Math.Max(hedefToplam, 0);
            var mevcutToplam = icerikler.Sum(getValue);
            var fark = hedefToplam - mevcutToplam;
            if (fark == 0)
                return;

            var sirali = icerikler
                .OrderByDescending(i => ReferenceEquals(i, tercihEdilen))
                .ThenBy(i => i.Id)
                .ToList();

            if (fark > 0)
            {
                foreach (var icerik in sirali)
                {
                    var mevcut = Math.Max(getValue(icerik), 0);
                    var bosluk = Math.Max(getCapacity(icerik) - mevcut, 0);
                    var eklenecek = Math.Min(fark, bosluk);
                    if (eklenecek <= 0)
                        continue;

                    setValue(icerik, mevcut + eklenecek);
                    fark -= eklenecek;
                    if (fark <= 0)
                        break;
                }
            }
            else
            {
                var azaltilacak = Math.Abs(fark);
                foreach (var icerik in sirali)
                {
                    var mevcut = Math.Max(getValue(icerik), 0);
                    var dusulecek = Math.Min(azaltilacak, mevcut);
                    if (dusulecek <= 0)
                        continue;

                    setValue(icerik, mevcut - dusulecek);
                    azaltilacak -= dusulecek;
                    if (azaltilacak <= 0)
                        break;
                }
            }
        }
    }
}
