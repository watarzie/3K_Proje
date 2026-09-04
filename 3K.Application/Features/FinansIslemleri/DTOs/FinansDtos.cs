using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.DTOs
{
    /// <summary>
    /// Menüye erişebilen herkesin görebileceği, parasal veri içermeyen operasyon özeti.
    /// Gelir, gider ve net tutarlar ayrı izinli sorgulardan döner.
    /// </summary>
    public sealed record FinansDashboardDto(
        int ToplamIs,
        decimal ToplamSandik,
        decimal ToplamM3,
        int SiparisBekleyen,
        int SiparisAcik,
        int KismiSiparis,
        int FaturaBekleyen,
        int Faturalanan,
        int BuAyOzelIs,
        decimal BuAyGider);

    public sealed record FinansHassasOzetDto(IReadOnlyList<FinansParaToplamiModel> Tutarlar);
    public sealed record FinansDurumTutarOzetiDto(
        IReadOnlyList<FinansParaToplamiModel> SiparisBekleyen,
        IReadOnlyList<FinansParaToplamiModel> SiparisAcik,
        IReadOnlyList<FinansParaToplamiModel> Faturalanan);
    public sealed record FinansIptalDto(string Aciklama);
    public sealed record FinansDosyaDto(byte[] Icerik, string IcerikTuru, string DosyaAdi);

    public static class FinansHassasAlanMaskeleme
    {
        public static FinansIsKaydiModel IsKaydi(FinansIsKaydiModel value)
            => value with
            {
                BirimFiyat = 0,
                ParaBirimi = string.Empty,
                KdvOrani = 0,
                NetTutar = 0,
                KdvTutari = 0,
                ToplamTutar = 0
            };

        public static FinansSiparisModel Siparis(FinansSiparisModel value)
            => value with
            {
                Tutarlar = Array.Empty<FinansParaToplamiModel>(),
                Kalemler = value.Kalemler.Select(line => line with
                {
                    BirimFiyat = 0,
                    ParaBirimi = string.Empty,
                    KdvOrani = 0,
                    NetTutar = 0,
                    KdvTutari = 0,
                    ToplamTutar = 0
                }).ToArray()
            };

        public static FinansFaturaModel Fatura(FinansFaturaModel value)
            => value with
            {
                Tutarlar = Array.Empty<FinansParaToplamiModel>(),
                BelgeParaBirimi = null,
                BelgeNetTutar = null,
                BelgeKdvTutari = null,
                BelgeToplamTutar = null,
                MutabakatFarki = 0,
                MutabakatAciklamasi = null
            };

        public static FinansUrunModel Urun(FinansUrunModel value)
            => new()
            {
                Id = value.Id,
                Kod = value.Kod,
                Ad = value.Ad,
                FiyatlandirmaBirimi = value.FiyatlandirmaBirimi,
                Aktif = value.Aktif,
                Sira = value.Sira,
                GuncelBirimFiyat = null,
                GuncelParaBirimi = null,
                GuncelKdvOrani = null,
                Eslesmeler = value.Eslesmeler
            };
    }
}
