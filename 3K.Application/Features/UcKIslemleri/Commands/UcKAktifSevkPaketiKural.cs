using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// Önceden teslimi bulunan bir satıra yapılan yeni Grid sevk paketini,
    /// önceki kümülatif sandık miktarlarından ayırır.
    /// </summary>
    public static class UcKAktifSevkPaketiKural
    {
        public static bool YeniPaketTeslimiMi(CekiSatiri satir)
        {
            // Proje transferli legacy satırlar yanlışlıkla TamGeldi durumuna geçmiş
            // olsa da yeni paket korumasında kalır. Diğer yeniden sevklerde Grid,
            // yeni paketi açarken iki 3K durumunu da Bekliyor'a alır.
            return satir.ProjeGonderilen > 0 ||
                   (satir.GelenMiktar > 0 &&
                    satir.UcKDurumuId == (int)UcKDurum.Bekliyor &&
                    satir.UcKKarsilamaTipiId == (int)UcKDurum.Bekliyor);
        }

        public static Result<decimal> HesaplaTeslimMiktari(CekiSatiri satir)
        {
            if (!satir.GridSevkMiktari.HasValue || satir.GridSevkMiktari.Value <= 0)
            {
                return Result<decimal>.Failure(
                    "Yeniden sevk teslimi için pozitif bir aktif Grid sevk miktarı bulunmalıdır.",
                    409);
            }

            var fizikselKalan = HesaplaFizikselKalan(satir);
            if (fizikselKalan <= 0)
            {
                return Result<decimal>.Failure(
                    "Ürünün fiziksel kalanı bulunmadığı için aktif Grid sevki otomatik olarak teslim alınamaz.",
                    409);
            }

            var aktifSevkMiktari = satir.GridSevkMiktari.Value;
            if (aktifSevkMiktari > fizikselKalan)
            {
                return Result<decimal>.Failure(
                    $"Aktif Grid sevk miktarı ({aktifSevkMiktari}), ürünün fiziksel kalanından ({fizikselKalan}) büyük. Veri uzlaştırılmadan 'Tam Geldi' işlemi yapılamaz.",
                    409);
            }

            return Result<decimal>.Success(aktifSevkMiktari);
        }

        public static Result SandikKapsaminiDogrula(
            IUnitOfWork unitOfWork,
            int cekiSatiriId,
            IEnumerable<int?> seciliSandikIcerikIdleri)
        {
            var mevcutIcerikIdleri = unitOfWork.GetRepository<SandikIcerik>()
                .Queryable()
                .Where(i => i.CekiSatiriId == cekiSatiriId)
                .Select(i => i.Id)
                .ToList();

            return TumSandiklarSecildiMi(mevcutIcerikIdleri, seciliSandikIcerikIdleri)
                ? Result.Success()
                : Result.Failure(
                    "Yeniden sevk paketi satır seviyesindedir. Çok sandıklı üründe 'Tam Geldi' için ürüne ait tüm sandık satırları birlikte seçilmelidir.",
                    409);
        }

        public static bool TumSandiklarSecildiMi(
            IReadOnlyCollection<int> mevcutIcerikIdleri,
            IEnumerable<int?> seciliSandikIcerikIdleri)
        {
            var secimler = seciliSandikIcerikIdleri.ToList();
            if (mevcutIcerikIdleri.Count <= 1 || secimler.Any(id => !id.HasValue))
                return true;

            var seciliIdler = secimler
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();
            return mevcutIcerikIdleri.All(seciliIdler.Contains);
        }

        public static decimal HesaplaFizikselKalan(CekiSatiri satir)
        {
            return Math.Max(
                satir.IstenenAdet
                - satir.GelenMiktar
                - satir.StokKarsilanan
                - satir.ProjeKarsilanan
                - satir.TedarikciKarsilanan
                + satir.ProjeGonderilen
                - satir.TrafoSevkAdet,
                0);
        }
    }
}
