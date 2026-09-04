using System.Globalization;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri
{
    internal static class AmbalajUretimYardimcilari
    {
        private const decimal DisOlcuBoyEnFarki = 92m;
        private const decimal DisOlcuYukseklikFarki = 255m;

        public static bool KaynakSandikOlculeriMi(AmbalajUretimKaydi kayit) =>
            !kayit.BagimsizKayitMi &&
            kayit.KaynakKayitId.HasValue &&
            kayit.KaynakModul is AmbalajKaynakModulu.Sandik or AmbalajKaynakModulu.Saha or AmbalajKaynakModulu.Yedek;

        public static bool OlculerGecerli(AmbalajUretimKaydi kayit) =>
            kayit.Adet > 0 && (KaynakSandikOlculeriMi(kayit)
                ? kayit.Boy > DisOlcuBoyEnFarki &&
                  kayit.En > DisOlcuBoyEnFarki &&
                  kayit.Yukseklik > DisOlcuYukseklikFarki
                : kayit.Boy > 0 && kayit.En > 0 && kayit.Yukseklik > 0);

        public static bool UretimMiktariGecerli(AmbalajUretimKaydi kayit) =>
            OlculerGecerli(kayit) || kayit.M3Override.HasValue;

        public static AmbalajOlculeri HesaplamaIcOlculeriniGetir(AmbalajUretimKaydi kayit) =>
            KaynakSandikOlculeriMi(kayit)
                ? new AmbalajOlculeri(
                    kayit.Boy - DisOlcuBoyEnFarki,
                    kayit.En - DisOlcuBoyEnFarki,
                    kayit.Yukseklik - DisOlcuYukseklikFarki)
                : new AmbalajOlculeri(kayit.Boy, kayit.En, kayit.Yukseklik);

        public static void M3DegerleriniHesapla(AmbalajUretimKaydi kayit)
        {
            if (!OlculerGecerli(kayit))
            {
                kayit.HesaplananBirimM3 = 0;
                kayit.HesaplananToplamM3 = 0;
                kayit.SarfM3 = kayit.M3Override.HasValue
                    ? Yuvarla(kayit.M3Override.Value * kayit.SarfOrani)
                    : 0;
                kayit.ToplamM3 = kayit.M3Override.HasValue
                    ? Yuvarla(kayit.M3Override.Value + kayit.SarfM3)
                    : 0;
                kayit.M3HesaplamaVersiyonu = AmbalajHesaplayici.FormulVersiyonu;
                return;
            }

            var icOlculer = HesaplamaIcOlculeriniGetir(kayit);
            var ayakProfili = AmbalajAyakProfiliBelirleyici.Belirle(kayit.Ad, kayit.KullanimAmaci);
            var ozet = AmbalajHesaplayici.M3OzetiHesapla(
                icOlculer.Boy,
                icOlculer.En,
                icOlculer.Yukseklik,
                kayit.Adet,
                kayit.SarfOrani,
                kayit.M3Override,
                ayakProfili,
                KaynakSandikOlculeriMi(kayit) ? kayit.Boy : null);
            kayit.HesaplananBirimM3 = ozet.HesaplananBirimM3;
            kayit.HesaplananToplamM3 = ozet.HesaplananToplamM3;
            kayit.SarfM3 = ozet.SarfM3;
            kayit.ToplamM3 = ozet.ToplamM3;
            kayit.M3HesaplamaVersiyonu = AmbalajHesaplayici.FormulVersiyonu;
        }

        public static AmbalajUretimKaydiDto DtoOlustur(
            AmbalajUretimKaydi kayit,
            string? projeNo = null,
            string? projeAdi = null,
            string? ustSandikNo = null)
        {
            var netM3 = kayit.M3Override ?? kayit.HesaplananToplamM3;
            return new AmbalajUretimKaydiDto
            {
                Id = kayit.Id,
                IsAkisKimligi = kayit.IsAkisKimligi,
                ProjeId = kayit.ProjeId,
                ProjeNo = projeNo ?? kayit.ManuelProjeNo,
                ProjeAdi = projeAdi ?? kayit.ManuelProjeAdi,
                UstKayitId = kayit.UstKayitId,
                UstSandikNo = ustSandikNo,
                Tur = kayit.Tur,
                TurMetni = TurMetni(kayit.Tur),
                KaynakModul = kayit.KaynakModul,
                KaynakModulMetni = KaynakMetni(kayit.KaynakModul),
                KaynakKayitId = kayit.KaynakKayitId,
                KaynakSenkronizasyonuKilitliMi = kayit.KaynakSenkronizasyonuKilitliMi,
                KaynakSonSenkronizasyonTarihi = kayit.KaynakSonSenkronizasyonTarihi,
                SandikNo = kayit.SandikNo,
                Ad = kayit.Ad,
                SandikCinsi = kayit.SandikCinsi,
                SandikCinsiMetni = CinsMetni(kayit.SandikCinsi, kayit.DigerSandikCinsi),
                DigerSandikCinsi = kayit.DigerSandikCinsi,
                Adet = kayit.Adet,
                Boy = kayit.Boy,
                En = kayit.En,
                Yukseklik = kayit.Yukseklik,
                OlcuEksikMi = !OlculerGecerli(kayit),
                AmbalajaDahil = kayit.AmbalajaDahil,
                UretimeAlindi = kayit.UretimeAlindi,
                HesaplananBirimM3 = kayit.HesaplananBirimM3,
                HesaplananToplamM3 = kayit.HesaplananToplamM3,
                M3Override = kayit.M3Override,
                M3OverrideNedeni = kayit.M3OverrideNedeni,
                NetM3 = netM3,
                M3HesaplamaVersiyonu = kayit.M3HesaplamaVersiyonu,
                SarfOrani = kayit.SarfOrani,
                SarfM3 = kayit.SarfM3,
                ToplamM3 = kayit.ToplamM3,
                KullanimAmaci = kayit.KullanimAmaci,
                TalepEdenKisi = kayit.TalepEdenKisi,
                TalepEdenBolum = kayit.TalepEdenBolum,
                TalimatVeren = kayit.TalimatVeren,
                FirinPartiNo = kayit.FirinPartiNo,
                Aciklama = kayit.Aciklama,
                UretimDurumu = kayit.UretimDurumu,
                UretimDurumuMetni = DurumMetni(kayit.UretimDurumu),
                UretimTarihi = kayit.UretimTarihi,
                TamamlanmaTarihi = kayit.TamamlanmaTarihi,
                FinansAktarimaHazirMi = AmbalajFinansAktarimPolitikasi.AktarimaHazirMi(kayit),
                IptalMi = kayit.IptalMi,
                IptalTarihi = kayit.IptalTarihi,
                IptalEdenKullaniciId = kayit.IptalEdenKullaniciId,
                IptalNedeni = kayit.IptalNedeni,
                CreatedDate = kayit.CreatedDate,
                UpdatedDate = kayit.UpdatedDate,
                CreatedBy = kayit.CreatedBy,
                UpdatedBy = kayit.UpdatedBy
            };
        }

        public static async Task AlanHareketleriniEkleAsync(
            IUnitOfWork unitOfWork,
            AmbalajUretimKaydi kayit,
            IReadOnlyDictionary<string, string?>? eski,
            string islem,
            int kullaniciId,
            string? aciklama = null)
        {
            var yeni = Snapshot(kayit);
            var islemGrubu = Guid.NewGuid();
            var hareketRepo = unitOfWork.GetRepository<AmbalajUretimHareketi>();

            foreach (var alan in yeni.Keys)
            {
                string? eskiDeger = null;
                if (eski != null)
                    eski.TryGetValue(alan, out eskiDeger);
                var yeniDeger = yeni[alan];
                if (eski != null && string.Equals(eskiDeger, yeniDeger, StringComparison.Ordinal))
                    continue;

                await hareketRepo.AddAsync(new AmbalajUretimHareketi
                {
                    AmbalajUretimKaydiId = kayit.Id,
                    AmbalajUretimKaydi = kayit,
                    IslemGrubu = islemGrubu,
                    KullaniciId = kullaniciId,
                    Islem = islem,
                    AlanAdi = alan,
                    EskiDeger = eskiDeger,
                    YeniDeger = yeniDeger,
                    Aciklama = Temizle(aciklama)
                });
            }
        }

        public static IReadOnlyDictionary<string, string?> Snapshot(AmbalajUretimKaydi k) =>
            new Dictionary<string, string?>
            {
                [nameof(k.ProjeId)] = Format(k.ProjeId),
                [nameof(k.ManuelProjeNo)] = k.ManuelProjeNo,
                [nameof(k.ManuelProjeAdi)] = k.ManuelProjeAdi,
                [nameof(k.UstKayitId)] = Format(k.UstKayitId),
                [nameof(k.Tur)] = Format((int)k.Tur),
                [nameof(k.KaynakModul)] = Format((int)k.KaynakModul),
                [nameof(k.KaynakKayitId)] = Format(k.KaynakKayitId),
                [nameof(k.KaynakSenkronizasyonuKilitliMi)] = Format(k.KaynakSenkronizasyonuKilitliMi),
                [nameof(k.KaynakSonSenkronizasyonTarihi)] = Format(k.KaynakSonSenkronizasyonTarihi),
                [nameof(k.SandikNo)] = k.SandikNo,
                [nameof(k.Ad)] = k.Ad,
                [nameof(k.SandikCinsi)] = Format((int)k.SandikCinsi),
                [nameof(k.DigerSandikCinsi)] = k.DigerSandikCinsi,
                [nameof(k.Adet)] = Format(k.Adet),
                [nameof(k.Boy)] = Format(k.Boy),
                [nameof(k.En)] = Format(k.En),
                [nameof(k.Yukseklik)] = Format(k.Yukseklik),
                [nameof(k.AmbalajaDahil)] = Format(k.AmbalajaDahil),
                [nameof(k.UretimeAlindi)] = Format(k.UretimeAlindi),
                [nameof(k.HesaplananBirimM3)] = Format(k.HesaplananBirimM3),
                [nameof(k.HesaplananToplamM3)] = Format(k.HesaplananToplamM3),
                [nameof(k.M3Override)] = Format(k.M3Override),
                [nameof(k.M3OverrideNedeni)] = k.M3OverrideNedeni,
                [nameof(k.M3HesaplamaVersiyonu)] = k.M3HesaplamaVersiyonu,
                [nameof(k.SarfOrani)] = Format(k.SarfOrani),
                [nameof(k.SarfM3)] = Format(k.SarfM3),
                [nameof(k.ToplamM3)] = Format(k.ToplamM3),
                [nameof(k.KullanimAmaci)] = k.KullanimAmaci,
                [nameof(k.TalepEdenKisi)] = k.TalepEdenKisi,
                [nameof(k.TalepEdenBolum)] = k.TalepEdenBolum,
                [nameof(k.TalimatVeren)] = k.TalimatVeren,
                [nameof(k.FirinPartiNo)] = k.FirinPartiNo,
                [nameof(k.Aciklama)] = k.Aciklama,
                [nameof(k.UretimDurumu)] = Format((int)k.UretimDurumu),
                [nameof(k.UretimTarihi)] = Format(k.UretimTarihi),
                [nameof(k.TamamlanmaTarihi)] = Format(k.TamamlanmaTarihi),
                [nameof(k.IptalMi)] = Format(k.IptalMi),
                [nameof(k.IptalTarihi)] = Format(k.IptalTarihi),
                [nameof(k.IptalEdenKullaniciId)] = Format(k.IptalEdenKullaniciId),
                [nameof(k.IptalNedeni)] = k.IptalNedeni
            };

        public static int SandikAdediHesapla(string? sandikNo)
        {
            var eslesme = System.Text.RegularExpressions.Regex.Match(sandikNo ?? string.Empty, @"^(\d+)\s*-\s*(\d+)$");
            if (!eslesme.Success)
                return 1;

            var baslangic = int.Parse(eslesme.Groups[1].Value, CultureInfo.InvariantCulture);
            var bitis = int.Parse(eslesme.Groups[2].Value, CultureInfo.InvariantCulture);
            return bitis >= baslangic ? bitis - baslangic + 1 : 1;
        }

        public static string? Temizle(string? deger) => string.IsNullOrWhiteSpace(deger) ? null : deger.Trim();

        public static string TurMetni(AmbalajSandikTuru tur) => tur switch
        {
            AmbalajSandikTuru.Normal => "Normal",
            AmbalajSandikTuru.Ilave => "İlave",
            AmbalajSandikTuru.Saha => "Saha",
            AmbalajSandikTuru.Yedek => "Yedek",
            AmbalajSandikTuru.Ic => "İç",
            _ => "Diğer"
        };

        public static string KaynakMetni(AmbalajKaynakModulu kaynak) => kaynak switch
        {
            AmbalajKaynakModulu.Sandik => "Sandık",
            AmbalajKaynakModulu.Saha => "Saha",
            AmbalajKaynakModulu.Yedek => "Yedek",
            AmbalajKaynakModulu.Manuel => "Manuel",
            _ => "Diğer"
        };

        public static string DurumMetni(AmbalajUretimDurumu durum) => durum switch
        {
            AmbalajUretimDurumu.Planlandi => "Bekliyor",
            AmbalajUretimDurumu.Uretimde => "Üretimde",
            _ => "Tamamlandı"
        };

        public static string CinsMetni(AmbalajSandikCinsi cins, string? diger) => cins switch
        {
            AmbalajSandikCinsi.AhsapKapali => "Ahşap Kapalı",
            AmbalajSandikCinsi.Kafes => "Kafes Sandık",
            AmbalajSandikCinsi.Kontrplak => "Kontrplak Sandık",
            AmbalajSandikCinsi.Katlanir => "Katlanır Sandık",
            _ => diger ?? "Diğer"
        };

        private static decimal Yuvarla(decimal deger) => Math.Round(deger, 6, MidpointRounding.AwayFromZero);
        private static string? Format(object? deger) => deger switch
        {
            null => null,
            DateTime tarih => tarih.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset tarih => tarih.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formatlanabilir => formatlanabilir.ToString(null, CultureInfo.InvariantCulture),
            _ => deger.ToString()
        };
    }
}
