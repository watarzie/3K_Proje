using System.Globalization;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Sandık bazlı saha aktarımında kullanılacak tek ve doğrulanmış fiziksel tahsisi temsil eder.
    /// </summary>
    public sealed record SandikBazliSahaAktarimAdayi(
        int SandikId,
        int CekiSatiriId,
        decimal Miktar);

    public sealed record SandikBazliSahaAktarimDogrulamaSonucu(
        IReadOnlyList<SandikBazliSahaAktarimAdayi> Adaylar,
        IReadOnlyList<string> Engeller)
    {
        public bool Basarili => Engeller.Count == 0;
    }

    /// <summary>
    /// Salt okunur ve yan etkisiz sandık bazlı aktarım güvenlik kuralı.
    /// Ürün bazlı saha aktarım akışından bağımsız tutulur.
    /// </summary>
    public static class SandikBazliSahaAktarimGuvenlikKural
    {
        public static SandikBazliSahaAktarimDogrulamaSonucu Dogrula(
            IReadOnlyCollection<Sandik> sandiklar,
            IReadOnlyDictionary<int, IReadOnlyCollection<SandikIcerik>> etkinIcerikMap,
            IReadOnlyDictionary<int, CekiSatiri> kaynakSatirlar,
            IReadOnlyCollection<SandikIcerik> tumFizikselIcerikler,
            IReadOnlyDictionary<int, decimal> aktifTamamlamaMap)
        {
            var engeller = new List<string>();
            var adaylar = new List<SandikBazliSahaAktarimAdayi>();

            // Teklik kontrolü yalnızca miktar alanları dolu kayıtlara bakamaz. Eski veride
            // ikinci bir gerçek SandikIcerik satırı tamamen sıfır kalmış olabilir; onu yok
            // saymak, bölünmüş/belirsiz tahsisi yanlışlıkla tek tahsis kabul ettirir.
            var gercekFizikselTahsisler = tumFizikselIcerikler
                .Where(i => i.Id > 0 && i.CekiSatiriId.HasValue)
                .GroupBy(i => i.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Id).ToList());

            foreach (var sandik in sandiklar)
            {
                var etkinIcerikler = etkinIcerikMap
                    .GetValueOrDefault(sandik.Id, Array.Empty<SandikIcerik>())
                    .Where(IncelenecekIcerikMi)
                    .OrderBy(i => i.CekiSatiri?.SiraNo ?? int.MaxValue)
                    .ThenBy(i => i.Id)
                    .ToList();

                if (etkinIcerikler.Count == 0)
                {
                    engeller.Add($"Sandık {SandikEtiketi(sandik)}: aktif fiziksel içerik bulunamadı.");
                    continue;
                }

                foreach (var icerik in etkinIcerikler)
                {
                    var nedenler = new List<string>();
                    var urunEtiketi = UrunEtiketi(icerik);
                    var etkinTahsisMiktari = icerik.TahsisMiktari;

                    if (!icerik.CekiSatiriId.HasValue)
                    {
                        nedenler.Add("manuel içerik bir normal proje çeki satırına bağlı değil");
                    }
                    else if (!kaynakSatirlar.TryGetValue(icerik.CekiSatiriId.Value, out var kaynakSatir))
                    {
                        nedenler.Add("normal projeye ait kök çeki satırı bulunamadı");
                    }
                    else
                    {
                        if (icerik.Id <= 0)
                        {
                            nedenler.Add("yalnız sandık numarasıyla eşleşen legacy kayıt var; gerçek SandikIcerik tahsisi yok");
                        }
                        else
                        {
                            if (!gercekFizikselTahsisler.TryGetValue(kaynakSatir.Id, out var satirTahsisleri) ||
                                satirTahsisleri.Count == 0)
                            {
                                nedenler.Add("aktif fiziksel tahsis kaydı bulunamadı");
                            }
                            else if (satirTahsisleri.Count > 1)
                            {
                                nedenler.Add($"ürün {satirTahsisleri.Count} aktif sandık tahsisine bölünmüş; önce tek tahsise birleştirilmeli");
                            }
                            else if (satirTahsisleri[0].Id != icerik.Id)
                            {
                                nedenler.Add("aktif fiziksel tahsis seçilen sandığa ait değil");
                            }
                            else
                            {
                                // Tek gerçek fiziksel kayda sahip eski verilerde TahsisMiktari 0 kalmış olabilir.
                                // Uygulamanın diğer okuma akışlarıyla aynı semantiği kullan: bu tek kayıt,
                                // çeki satırının istenen miktarını temsil eder. Negatif tahsis bozuk veri olarak,
                                // çoklu/bölünmüş kayıtlar da yukarıdaki korumayla bloklanmaya devam eder.
                                if (LegacyTekTahsisFallbackUygunMu(icerik, kaynakSatir))
                                {
                                    etkinTahsisMiktari = SandikTahsisHelper.HesaplaSandikMiktari(
                                        kaynakSatir,
                                        icerik,
                                        satirTahsisleri.Count);
                                }
                            }
                        }

                        if (etkinTahsisMiktari <= 0)
                        {
                            nedenler.Add("seçilen sandık tahsis miktarı sıfır veya negatif");
                        }
                        else if (etkinTahsisMiktari > kaynakSatir.IstenenAdet)
                        {
                            nedenler.Add(
                                $"sandık tahsisi ({FormatAdet(etkinTahsisMiktari)}) istenen miktardan " +
                                $"({FormatAdet(kaynakSatir.IstenenAdet)}) büyük");
                        }

                        if (!SandikIcerigiBaslangicDurumundaMi(icerik, etkinTahsisMiktari))
                        {
                            nedenler.Add(
                                "sandık içeriği işlenmiş " +
                                $"(konulan:{FormatAdet(icerik.KonulanAdet)}, eksik:{FormatAdet(icerik.EksikAdet)}, " +
                                $"stok:{FormatAdet(icerik.StokKarsilanan)}, proje:{FormatAdet(icerik.ProjeKarsilanan)}, " +
                                $"tedarikçi:{FormatAdet(icerik.TedarikciKarsilanan)})");
                        }

                        if (!GridBaslangicDurumundaMi(kaynakSatir))
                        {
                            nedenler.Add(
                                "Grid işlemleri başlangıç durumunda değil " +
                                $"(durum:{kaynakSatir.GridDurumuId}, gelen:{FormatAdet(kaynakSatir.GridGelenAdet)}, " +
                                $"trafo:{FormatAdet(kaynakSatir.TrafoSevkAdet)}, sevk:{kaynakSatir.GridSevkDurumuId})");
                        }

                        if (!UcKBaslangicDurumundaMi(kaynakSatir))
                        {
                            nedenler.Add(
                                "3K işlemleri başlangıç durumunda değil " +
                                $"(durum:{kaynakSatir.UcKDurumuId}, gelen:{FormatAdet(kaynakSatir.GelenMiktar)}, " +
                                $"karşılanan:{FormatAdet(kaynakSatir.KarsilananMiktar)})");
                        }

                        if (kaynakSatir.KaliteDurumId.HasValue || kaynakSatir.SurecDurumId.HasValue)
                            nedenler.Add("kalite veya süreç durumu geri alınmamış");

                        var aktifTamamlama = aktifTamamlamaMap.GetValueOrDefault(kaynakSatir.Id);
                        if (aktifTamamlama > 0)
                        {
                            nedenler.Add(
                                $"ürün için daha önce oluşturulmuş {FormatAdet(aktifTamamlama)} miktarında aktif saha aktarımı var");
                        }
                    }

                    if (nedenler.Count > 0)
                    {
                        engeller.Add(
                            $"Sandık {SandikEtiketi(sandik)} / {urunEtiketi}: {string.Join(", ", nedenler)}.");
                        continue;
                    }

                    adaylar.Add(new SandikBazliSahaAktarimAdayi(
                        sandik.Id,
                        icerik.CekiSatiriId!.Value,
                        etkinTahsisMiktari));
                }
            }

            return new SandikBazliSahaAktarimDogrulamaSonucu(
                engeller.Count == 0 ? adaylar : Array.Empty<SandikBazliSahaAktarimAdayi>(),
                engeller);
        }

        public static bool EtkinIcerikMi(SandikIcerik icerik)
        {
            // Sıfırdan farklı herhangi bir fiziksel/tahsis miktarı kaydı etkin kabul edilir.
            // Negatif veya yalnız gölge Miktar alanında kalmış bozuk kayıtlar da sessizce atlanmaz.
            return icerik.TahsisMiktari != 0 ||
                   icerik.KonulanAdet != 0 ||
                   icerik.EksikAdet != 0 ||
                   icerik.StokKarsilanan != 0 ||
                   icerik.ProjeKarsilanan != 0 ||
                   icerik.TedarikciKarsilanan != 0 ||
                   icerik.Miktar != 0;
        }

        public static bool IncelenecekIcerikMi(SandikIcerik icerik)
        {
            // Miktarları tamamen sıfır olsa bile veritabanında gerçek ve çeki satırına
            // bağlı bir fiziksel kayıt güvenlik incelemesinden sessizce düşürülmemelidir.
            return EtkinIcerikMi(icerik) ||
                   (icerik.Id > 0 && icerik.CekiSatiriId.HasValue);
        }

        private static bool LegacyTekTahsisFallbackUygunMu(
            SandikIcerik icerik,
            CekiSatiri kaynakSatir)
        {
            if (icerik.TahsisMiktari != 0 || kaynakSatir.IstenenAdet <= 0)
                return false;

            var golgeMiktarUygunMu = icerik.Miktar == 0 ||
                                     icerik.Miktar == kaynakSatir.IstenenAdet;
            var eksikBaslangicDegeriMi = icerik.EksikAdet == 0 ||
                                         icerik.EksikAdet == kaynakSatir.IstenenAdet;

            return golgeMiktarUygunMu &&
                   eksikBaslangicDegeriMi &&
                   icerik.KonulanAdet == 0 &&
                   icerik.StokKarsilanan == 0 &&
                   icerik.ProjeKarsilanan == 0 &&
                   icerik.TedarikciKarsilanan == 0;
        }

        private static bool SandikIcerigiBaslangicDurumundaMi(
            SandikIcerik icerik,
            decimal etkinTahsisMiktari)
        {
            // Yeni import 0, 3K geri alma senkronizasyonu ise tahsis kadar EksikAdet bırakır.
            // Diğer başlangıç alanları sıfırken iki gösterim de aynı iş anlamına (henüz karşılanmadı) gelir.
            var eksikBaslangicDegeriMi = icerik.EksikAdet == 0 ||
                                         (etkinTahsisMiktari > 0 &&
                                          icerik.EksikAdet == etkinTahsisMiktari);

            return icerik.KonulanAdet == 0 &&
                   eksikBaslangicDegeriMi &&
                   icerik.StokKarsilanan == 0 &&
                   icerik.ProjeKarsilanan == 0 &&
                   icerik.TedarikciKarsilanan == 0;
        }

        private static bool GridBaslangicDurumundaMi(CekiSatiri satir)
        {
            return satir.GridDurumuId == (int)GridDurum.Gelmedi &&
                   satir.GridGelenAdet == 0 &&
                   satir.TrafoSevkAdet == 0 &&
                   satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdilmedi &&
                   (!satir.GridSevkMiktari.HasValue || satir.GridSevkMiktari.Value == 0) &&
                   satir.YenidenSevkGerekliAdet == 0;
        }

        private static bool UcKBaslangicDurumundaMi(CekiSatiri satir)
        {
            return satir.UcKDurumuId == (int)UcKDurum.Bekliyor &&
                   satir.UcKKarsilamaTipiId == (int)UcKDurum.Bekliyor &&
                   satir.GelenMiktar == 0 &&
                   satir.KarsilananMiktar == 0 &&
                   satir.StokKarsilanan == 0 &&
                   satir.ProjeKarsilanan == 0 &&
                   satir.ProjeGonderilen == 0 &&
                   satir.TedarikciKarsilanan == 0 &&
                   satir.HataliMiktar == 0 &&
                   satir.GeriGonderilenMiktar == 0;
        }

        private static string SandikEtiketi(Sandik sandik)
        {
            return string.IsNullOrWhiteSpace(sandik.SandikNo)
                ? $"#{sandik.Id}"
                : sandik.SandikNo.Trim();
        }

        private static string UrunEtiketi(SandikIcerik icerik)
        {
            var satir = icerik.CekiSatiri;
            var aciklama = satir?.Aciklama ?? icerik.Isim;
            var barkod = satir?.BarkodNo ?? icerik.BarkodNo;

            if (!string.IsNullOrWhiteSpace(aciklama) && !string.IsNullOrWhiteSpace(barkod))
                return $"{aciklama.Trim()} [{barkod.Trim()}]";

            if (!string.IsNullOrWhiteSpace(aciklama))
                return aciklama.Trim();

            if (!string.IsNullOrWhiteSpace(barkod))
                return barkod.Trim();

            return icerik.CekiSatiriId.HasValue
                ? $"çeki satırı #{icerik.CekiSatiriId.Value}"
                : $"içerik #{icerik.Id}";
        }

        private static string FormatAdet(decimal miktar)
        {
            return miktar.ToString("0.####", CultureInfo.GetCultureInfo("tr-TR"));
        }
    }
}
