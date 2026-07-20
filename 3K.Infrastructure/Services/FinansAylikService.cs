using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Core.Services;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public class FinansAylikService : IFinansAylikService
    {
        private const decimal Tolerans = 0.000001m;
        private readonly AppDbContext _context;

        public FinansAylikService(AppDbContext context) => _context = context;

        public async Task<IReadOnlyList<FinansAylikIsDto>> ListeleAsync(int yil, int ay, CancellationToken cancellationToken = default)
        {
            if (yil is < 2000 or > 2100 || ay is < 1 or > 12)
                throw new ArgumentOutOfRangeException(nameof(ay), "Yıl 2000-2100, ay 1-12 aralığında olmalıdır.");

            var baslangic = new DateTime(yil, ay, 1);
            var bitis = baslangic.AddMonths(1);
            var urunler = await _context.FinansUrunleri.AsNoTracking().Where(u => u.Aktif)
                .Include(u => u.Eslesmeler.Where(e => e.Aktif)).OrderBy(u => u.Sira).ToListAsync(cancellationToken);
            var icSandikSablonAdlari = await _context.AmbalajIcSandikSablonlari.AsNoTracking()
                .ToDictionaryAsync(s => s.Id, s => s.Ad, cancellationToken);
            var projeIsleri = await _context.FinansIsKayitlari.AsNoTracking()
                .Where(k => k.KaynakAktif && k.OzelIsId == null && k.UretimeAlinmaTarihi >= baslangic && k.UretimeAlinmaTarihi < bitis)
                .Include(k => k.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .Include(k => k.SiparisKalemleri).ThenInclude(k => k.FaturaKalemleri).ThenInclude(k => k.Fatura)
                .AsSplitQuery().ToListAsync(cancellationToken);
            var ozelIsler = await _context.FinansOzelIsleri.AsNoTracking()
                .Where(o => o.IsTarihi >= baslangic && o.IsTarihi < bitis)
                .Include(o => o.Proje).Include(o => o.FinansKaydi).ThenInclude(k => k!.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .Include(o => o.FinansKaydi).ThenInclude(k => k!.SiparisKalemleri).ThenInclude(k => k.FaturaKalemleri).ThenInclude(k => k.Fatura)
                .AsSplitQuery().ToListAsync(cancellationToken);

            var sonuc = new List<FinansAylikIsDto>();
            foreach (var grup in projeIsleri.GroupBy(k => new
            {
                k.ProjeId,
                k.ProjeNo,
                k.IsTuru,
                SandikTipi = k.IsTuru is FinansIsTuru.IlaveSandik or FinansIsTuru.IcSandik or FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik ? k.SandikTipi : null,
                Boy = k.IsTuru is FinansIsTuru.IlaveSandik or FinansIsTuru.IcSandik or FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik ? k.Boy : null,
                En = k.IsTuru is FinansIsTuru.IlaveSandik or FinansIsTuru.IcSandik or FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik ? k.En : null,
                Yukseklik = k.IsTuru is FinansIsTuru.IlaveSandik or FinansIsTuru.IcSandik or FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik ? k.Yukseklik : null,
                IcSandikSablonId = k.IsTuru == FinansIsTuru.IcSandik ? k.IcSandikSablonId : null
            }))
            {
                var kayitlar = grup.ToList();
                var urun = FinansTarifeSecici.Sec(urunler, kayitlar[0]);
                var miktar = urun?.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.Adet
                    ? kayitlar.Sum(k => k.Adet)
                    : kayitlar.Sum(k => k.ToplamM3);
                if (grup.Key.IsTuru == FinansIsTuru.NormalSandik)
                    miktar = Math.Round(miktar, 2, MidpointRounding.AwayFromZero);
                var birim = urun?.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.Adet ? "Adet" : "m³";
                var siparisKalemleri = kayitlar.SelectMany(k => k.SiparisKalemleri)
                    .Where(k => k.Siparis.Durum != FinansSiparisDurumu.IptalEdildi).ToList();
                var faturalar = siparisKalemleri.SelectMany(k => k.FaturaKalemleri)
                    .Where(k => k.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).ToList();
                var birimFiyat = urun?.BirimFiyat ?? 0;
                var kdvOrani = urun?.KdvOrani ?? 0;
                var net = Para(miktar * birimFiyat);
                var kdv = Para(net * kdvOrani / 100);
                var akis = AkisTutarlari(miktar, siparisKalemleri, faturalar);
                var siparisMiktari = grup.Key.IsTuru == FinansIsTuru.NormalSandik
                    ? Math.Round(akis.SiparisMiktari, 2, MidpointRounding.AwayFromZero)
                    : akis.SiparisMiktari;
                var faturalananMiktar = grup.Key.IsTuru == FinansIsTuru.NormalSandik
                    ? Math.Round(akis.FaturalananMiktar, 2, MidpointRounding.AwayFromZero)
                    : akis.FaturalananMiktar;
                var isAdi = grup.Key.IsTuru == FinansIsTuru.IcSandik && grup.Key.IcSandikSablonId.HasValue
                    ? icSandikSablonAdlari.GetValueOrDefault(grup.Key.IcSandikSablonId.Value, "Silinmiş İç Sandık Tipi")
                    : grup.Key.IsTuru is FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik && !string.IsNullOrWhiteSpace(grup.Key.SandikTipi)
                        ? $"{IsGrubu(grup.Key.IsTuru)} · {grup.Key.SandikTipi}"
                        : IsGrubu(grup.Key.IsTuru);
                if (grup.Key.SandikTipi == "Katlanır Sandık")
                    isAdi += $" · {grup.Key.Boy:0.##}×{grup.Key.En:0.##}×{grup.Key.Yukseklik:0.##} mm";
                sonuc.Add(new FinansAylikIsDto("Proje", null, grup.Key.ProjeId, grup.Key.IsTuru, IsGrubu(grup.Key.IsTuru),
                    grup.Key.ProjeNo, isAdi, grup.Key.SandikTipi, grup.Key.Boy, grup.Key.En, grup.Key.Yukseklik,
                    kayitlar.Min(k => k.UretimeAlinmaTarihi), kayitlar.Max(k => k.UretimeAlinmaTarihi),
                    grup.Key.IsTuru == FinansIsTuru.SarfKereste ? 0 : kayitlar.Sum(k => k.Adet), miktar, birim, birimFiyat,
                    kdvOrani, net, kdv, net + kdv, urun?.ParaBirimi ?? "EUR", siparisMiktari,
                    faturalananMiktar, akis.SiparisToplamTutar, akis.FaturalananToplamTutar, kayitlar.Select(k => k.Id).ToArray(),
                    siparisKalemleri.Select(k => k.Siparis.PoNumarasi).Distinct().ToArray(),
                    faturalar.Select(k => k.Fatura.FaturaNumarasi).Distinct().ToArray(), Durum(miktar, siparisMiktari, faturalananMiktar),
                    false, false, false, null));
            }

            foreach (var ozel in ozelIsler)
            {
                var siparisKalemleri = ozel.FinansKaydi?.SiparisKalemleri
                    .Where(k => k.Siparis.Durum != FinansSiparisDurumu.IptalEdildi).ToList() ?? [];
                var faturalar = siparisKalemleri.SelectMany(k => k.FaturaKalemleri)
                    .Where(k => k.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).ToList();
                var net = Para(ozel.Miktar * ozel.BirimFiyat);
                var kdv = Para(net * ozel.KdvOrani / 100);
                var miktarBekliyor = ozel.HesaplamaYontemi == FinansHesaplamaYontemi.DegiskenTutar
                    ? ozel.BirimFiyat <= 0
                    : ozel.Miktar <= 0;
                var akis = AkisTutarlari(ozel.Miktar, siparisKalemleri, faturalar);
                var durum = ozel.IptalEdildi ? "İptal" : miktarBekliyor ? "Miktar Bekliyor" : Durum(ozel.Miktar, akis.SiparisMiktari, akis.FaturalananMiktar);
                sonuc.Add(new FinansAylikIsDto("OzelIs", ozel.Id, ozel.ProjeId, FinansIsTuru.OzelIs,
                    string.IsNullOrWhiteSpace(ozel.RaporGrubu) ? "Özel İş" : ozel.RaporGrubu,
                    ozel.Proje?.ProjeNo ?? string.Empty, ozel.IsAdi, null, null, null, null,
                    ozel.IsTarihi, ozel.IsTarihi, 0, ozel.Miktar,
                    ozel.Birim, ozel.BirimFiyat, ozel.KdvOrani, net, kdv, net + kdv, ozel.ParaBirimi,
                    akis.SiparisMiktari, akis.FaturalananMiktar, akis.SiparisToplamTutar, akis.FaturalananToplamTutar,
                    ozel.FinansKaydi == null ? [] : [ozel.FinansKaydi.Id],
                    siparisKalemleri.Select(k => k.Siparis.PoNumarasi).Distinct().ToArray(),
                    faturalar.Select(k => k.Fatura.FaturaNumarasi).Distinct().ToArray(), durum,
                    !ozel.IptalEdildi && siparisKalemleri.Count == 0 && ozel.HesaplamaYontemi == FinansHesaplamaYontemi.DegiskenAdet,
                    !ozel.IptalEdildi && siparisKalemleri.Count == 0 && ozel.HesaplamaYontemi == FinansHesaplamaYontemi.DegiskenTutar,
                    ozel.IptalEdildi, ozel.IptalAciklamasi));
            }

            return sonuc.OrderBy(x => x.IsGrubu).ThenBy(x => x.ProjeNo).ThenBy(x => x.IsAdi).ToList();
        }

        private static string Durum(decimal miktar, decimal siparisMiktari, decimal faturalananMiktar)
        {
            if (miktar <= Tolerans) return "Miktar Bekliyor";
            if (siparisMiktari <= Tolerans) return "Sipariş Bekliyor";
            if (miktar - siparisMiktari > Tolerans) return "Kısmi Sipariş";
            return siparisMiktari - faturalananMiktar > Tolerans ? "Fatura Bekliyor" : "Tamamlandı";
        }

        private static (decimal SiparisMiktari, decimal FaturalananMiktar, decimal SiparisToplamTutar, decimal FaturalananToplamTutar) AkisTutarlari(
            decimal miktar,
            IReadOnlyCollection<FinansSiparisKalemi> siparisler,
            IReadOnlyCollection<FinansFaturaKalemi> faturalar)
        {
            var siparisMiktari = Math.Min(miktar, siparisler.Sum(k => k.FiyatlandirmaMiktari));
            var faturalananMiktar = Math.Min(siparisMiktari, faturalar.Sum(k =>
                k.SiparisKalemi.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.M3 ? k.M3 : k.Adet));
            var siparisToplami = Para(siparisler.Sum(k => k.ToplamTutar));
            var faturaToplami = Para(faturalar.Sum(fatura =>
            {
                var siparisKalemi = fatura.SiparisKalemi;
                if (siparisKalemi.FiyatlandirmaMiktari <= Tolerans) return 0;
                var faturaMiktari = siparisKalemi.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.M3 ? fatura.M3 : fatura.Adet;
                return siparisKalemi.ToplamTutar * Math.Min(1, faturaMiktari / siparisKalemi.FiyatlandirmaMiktari);
            }));
            return (siparisMiktari, faturalananMiktar, siparisToplami, faturaToplami);
        }

        private static string IsGrubu(FinansIsTuru tur) => tur switch
        {
            FinansIsTuru.NormalSandik => "Ana Ambalaj",
            FinansIsTuru.SarfKereste => "Sarf Kereste",
            FinansIsTuru.IlaveSandik => "İlave Sandık",
            FinansIsTuru.IcSandik => "İç Sandık",
            FinansIsTuru.SahaSandigi => "Saha Sandığı",
            FinansIsTuru.YedekSandik => "Yedek Sandık",
            FinansIsTuru.Tadilat => "Tadilat",
            FinansIsTuru.DigerAmbalajIsi => "Diğer Ambalaj İşi",
            _ => "Özel İş"
        };

        private static decimal Para(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}