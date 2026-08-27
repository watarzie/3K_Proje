using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// Tek sandıklı bir satırda, başka projeye verilen miktarın yerine Grid tarafından
    /// açılmış yeni sevk paketinin önceki sandık mevcudundan düşülmeden teslim alınmasını sağlar.
    /// </summary>
    public static class UcKProjeTransferTelafiTeslimKural
    {
        public static bool AdayMi(CekiSatiri satir)
        {
            return satir.ProjeGonderilen > 0 &&
                   satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi &&
                   (satir.GridSevkMiktari ?? 0) > 0 &&
                   satir.UcKDurumuId == (int)UcKDurum.Bekliyor &&
                   satir.UcKKarsilamaTipiId == (int)UcKDurum.Bekliyor &&
                   satir.TeslimTarihi == null &&
                   HamKalanMiktar(satir) > 0;
        }

        public static bool AktifMi(CekiSatiri satir, int sandikIcerikSayisi)
        {
            return sandikIcerikSayisi == 1 && AdayMi(satir);
        }

        public static Result<decimal> TeslimMiktariniHesapla(
            bool aktif,
            decimal mevcutHesaplananMiktar,
            CekiSatiri satir)
        {
            if (!aktif)
                return Result<decimal>.Success(mevcutHesaplananMiktar);

            var aktifGridSevkMiktari = satir.GridSevkMiktari ?? 0;
            var hamKalanMiktari = HamKalanMiktar(satir);
            if (aktifGridSevkMiktari <= 0 || aktifGridSevkMiktari > hamKalanMiktari)
            {
                return Result<decimal>.Failure(
                    $"Proje transferi telafi sevk miktarı ({aktifGridSevkMiktari}), satırın ham kalan miktarıyla ({hamKalanMiktari}) uyumlu değil. İşlem yapılmadı; Grid sevk kaydı kontrol edilmelidir.",
                    409);
            }

            return Result<decimal>.Success(aktifGridSevkMiktari);
        }

        public static bool TeslimIslemiGerekliMi(bool aktif, decimal sandikKalanMiktari)
        {
            return aktif || sandikKalanMiktari > 0;
        }

        public static decimal HamKalanMiktar(CekiSatiri satir)
        {
            return Math.Max(
                satir.IstenenAdet - satir.GelenMiktar - satir.StokKarsilanan -
                satir.ProjeKarsilanan - satir.TedarikciKarsilanan +
                satir.ProjeGonderilen - satir.TrafoSevkAdet,
                0);
        }
    }
}
