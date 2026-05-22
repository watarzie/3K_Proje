using _3K.Application.Features.DashboardIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.DashboardIslemleri.Queries
{
    internal static class DashboardProjection
    {
        public static DashboardProjeItemDto ToProjeItem(Proje proje, ILookupCacheService lookupCache)
        {
            var sandiklar = proje.Sandiklar ?? new List<Sandik>();
            var cekiSatirlari = proje.Cekiler?.SelectMany(c => c.CekiSatirlari).ToList() ?? new List<CekiSatiri>();
            var sandikIcerikleri = sandiklar.SelectMany(s => s.SandikIcerikleri).ToList();
            var isSahaYedek = proje.ProjeTipiId is (int)ProjeTipi.Saha or (int)ProjeTipi.Yedek;
            var toplamUrun = isSahaYedek ? sandikIcerikleri.Count : cekiSatirlari.Count;
            var tamamlananUrun = isSahaYedek
                ? sandikIcerikleri.Count(si =>
                {
                    var istenen = si.CekiSatiri?.IstenenAdet ?? si.Miktar;
                    return istenen > 0 && si.KonulanAdet >= istenen;
                })
                : cekiSatirlari.Count(cs => cs.KalanMiktar <= 0);
            var durumId = HesaplaDurumId(proje, sandiklar);

            return new DashboardProjeItemDto
            {
                Id = proje.Id,
                ProjeNo = proje.ProjeNo,
                Musteri = proje.Musteri,
                DurumId = durumId,
                DurumMetni = lookupCache.GetDeger<LookupProjeDurum>(durumId),
                ProjeTipiId = proje.ProjeTipiId,
                ProjeTipiMetni = lookupCache.GetDeger<LookupProjeTipi>(proje.ProjeTipiId),
                BaslamaTarihi = proje.CreatedDate,
                CalismaGunSayisi = HesaplaCalismaGunSayisi(proje.CreatedDate, proje.GerceklesenSevkTarihi),
                GerceklesenSevkTarihi = proje.GerceklesenSevkTarihi,
                Lokasyon = proje.Lokasyon,
                SandikSayisi = sandiklar.Count,
                ToplamUrunSayisi = toplamUrun,
                TamamlananUrunSayisi = tamamlananUrun
            };
        }

        public static bool DepodaSayilacakSandik(Sandik sandik, IReadOnlySet<string> gridKapandiSandikNolari)
        {
            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                return false;

            if (SandiktaGridKapandiUrunVar(sandik, gridKapandiSandikNolari))
                return true;

            return sandik.SandikIcerikleri.Any(i =>
            {
                var satir = i.CekiSatiri;
                if (satir == null)
                    return i.Miktar > 0 || i.KonulanAdet > 0 || i.StokKarsilanan > 0 || i.ProjeKarsilanan > 0 || i.TedarikciKarsilanan > 0;

                return satir.GelenMiktar > 0
                    || satir.ProjeKarsilanan > 0
                    || satir.StokKarsilanan > 0
                    || satir.TedarikciKarsilanan > 0;
            });
        }

        public static int EtkinDepoLokasyonId(Sandik sandik, IReadOnlySet<string> gridKapandiSandikNolari)
        {
            return SandiktaGridKapandiUrunVar(sandik, gridKapandiSandikNolari)
                ? (int)DepoLokasyon.Grid
                : sandik.DepoLokasyonId;
        }

        private static int HesaplaDurumId(Proje proje, ICollection<Sandik> sandiklar)
        {
            var toplamSandik = sandiklar.Count;
            var hazirSandik = sandiklar.Count(s =>
                s.DurumId == (int)SandikDurum.Kapandi ||
                s.DurumId == (int)SandikDurum.Sevkedildi);

            if (proje.DurumId == (int)ProjeDurum.SevkEdildi)
                return (int)ProjeDurum.SevkEdildi;

            if (hazirSandik == toplamSandik && toplamSandik > 0)
                return (int)ProjeDurum.Tamamlandi;

            return (int)ProjeDurum.Hazirlaniyor;
        }

        private static bool SandiktaGridKapandiUrunVar(Sandik sandik, IReadOnlySet<string> gridKapandiSandikNolari)
        {
            return gridKapandiSandikNolari.Contains(sandik.SandikNo.Trim())
                || sandik.SandikIcerikleri.Any(i => i.CekiSatiri?.GridDurumuId == (int)GridDurum.GridKapandi);
        }

        private static int HesaplaCalismaGunSayisi(DateTime baslamaTarihi, DateTime? bitisTarihi)
        {
            var bitis = (bitisTarihi ?? TurkeyTime.Now).Date;
            var gunSayisi = (bitis - baslamaTarihi.Date).Days;
            return Math.Max(0, gunSayisi);
        }
    }
}
