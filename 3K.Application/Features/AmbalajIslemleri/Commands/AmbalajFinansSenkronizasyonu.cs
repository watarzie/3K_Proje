using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Application.Features.AmbalajIslemleri;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    internal static class AmbalajFinansSenkronizasyonu
    {
        public static Task KaydetVeAktarAsync(
            IUnitOfWork unitOfWork,
            IFinansUretimAktarimService finansService,
            AmbalajUretimKaydi kayit,
            Proje? proje,
            CancellationToken cancellationToken) =>
            unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                await unitOfWork.SaveChangesAsync(transactionToken);
                await finansService.UretimKayitlariniAktarAsync(
                    [ModelOlustur(kayit, proje)],
                    transactionToken);
                return true;
            }, cancellationToken);

        internal static FinansUretimAktarimModel ModelOlustur(AmbalajUretimKaydi kayit, Proje? proje)
        {
            var uretimTarihi = kayit.UretimTarihi ?? kayit.CreatedDate;
            // Finans fiyatlandırmasının siparişe esas miktarı net üretim m³'üdür.
            // %11 sarf kereste, Üretim isterinde ayrı raporlanan bir üretim metriğidir;
            // ayrı PO/gelir kalemi olarak tarif edilmediğinden burada ToplamM3 veya
            // ikinci bir SarfKereste hareketi üretmek aynı işi iki kez fiyatlandırırdı.
            // SarfKereste enum'u bilinçli manuel/gelecekteki ayrı tarife kullanımı içindir.
            var netM3 = kayit.M3Override ?? kayit.HesaplananToplamM3;
            var birimM3 = kayit.Adet > 0 ? decimal.Round(netM3 / kayit.Adet, 6) : 0;
            return new FinansUretimAktarimModel(
                KaynakTuru: "AmbalajUretim",
                KaynakKayitId: kayit.IsAkisKimligi.ToString("D"),
                KaynakAktif: AmbalajFinansAktarimPolitikasi.AktarimaHazirMi(kayit),
                ProjeId: kayit.ProjeId,
                ProjeNo: proje?.ProjeNo ?? kayit.ManuelProjeNo ?? "BAĞIMSIZ",
                Musteri: proje?.Musteri ?? kayit.ManuelProjeAdi ?? string.Empty,
                IsTuru: IsTuruBelirle(kayit.Tur),
                IsAdi: $"{AmbalajUretimYardimcilari.TurMetni(kayit.Tur)} Ambalaj - {kayit.SandikNo}",
                Adet: kayit.Adet,
                BirimM3: birimM3,
                UretimTarihi: uretimTarihi,
                FinansDonemi: new DateTime(uretimTarihi.Year, uretimTarihi.Month, 1),
                SandikNo: kayit.SandikNo,
                SandikAdi: kayit.Ad,
                SandikTipi: AmbalajUretimYardimcilari.CinsMetni(kayit.SandikCinsi, kayit.DigerSandikCinsi),
                Boy: kayit.Boy,
                En: kayit.En,
                Yukseklik: kayit.Yukseklik,
                Aciklama: kayit.Aciklama,
                TalepEdenKisi: kayit.TalepEdenKisi,
                TalepEdenBolum: kayit.TalepEdenBolum);
        }

        private static FinansIsTuru IsTuruBelirle(AmbalajSandikTuru tur) => tur switch
        {
            AmbalajSandikTuru.Normal => FinansIsTuru.AnaAmbalaj,
            AmbalajSandikTuru.Ilave => FinansIsTuru.IlaveSandik,
            AmbalajSandikTuru.Saha => FinansIsTuru.SahaSandigi,
            AmbalajSandikTuru.Yedek => FinansIsTuru.YedekSandik,
            AmbalajSandikTuru.Ic => FinansIsTuru.IcSandik,
            _ => FinansIsTuru.DigerAmbalaj
        };
    }
}
