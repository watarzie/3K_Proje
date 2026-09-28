using _3K.Core.Interfaces;
using _3K.Infrastructure.Services.Finans;

namespace _3K.Application.Tests;

/// <summary>
/// Her finans alan arayüzü kendi servisine çözülür. Transaction, denetim ve fiyat bileşenleri kapsam
/// başına tektir; aynı istekteki servisler tek transaction'a katılır ve tek denetim grubuna yazar.
/// </summary>
public sealed class FinansServisKayitlariTests
{
    [Fact]
    public void Alan_arayuzleri_kendi_servislerine_cozulur_ortak_bilesenler_kapsamda_tektir()
    {
        using var db = FinansTestServisleri.BaglanmayanContext();
        var finans = new FinansTestServisleri(db, new Kullanici());

        Assert.IsType<FinansPanelService>(finans.Panel);
        Assert.IsType<FinansIsKaydiService>(finans.IsKaydi);
        Assert.IsType<FinansAylikOzetService>(finans.AylikOzet);
        Assert.IsType<FinansSiparisFaturaService>(finans.SiparisFatura);
        Assert.IsType<FinansGiderService>(finans.Gider);
        Assert.IsType<FinansGiderTanimService>(finans.GiderTanim);
        Assert.IsType<FinansUrunService>(finans.Urun);
        Assert.IsType<FinansSablonService>(finans.Sablon);
        Assert.IsType<FinansDuzenliIsService>(finans.DuzenliIs);
        Assert.IsType<FinansBelgeService>(finans.Belge);
        Assert.IsType<FinansKaliciSilmeService>(finans.KaliciSilme);
        Assert.IsType<FinansRaporVerisiService>(finans.RaporVerisi);
        Assert.IsType<FinansUretimAktarimService>(finans.UretimAktarim);
        Assert.IsType<FinansRaporService>(finans.Raporlar);
        Assert.Same(finans.Al<FinansIslemYurutucu>(), finans.Al<FinansIslemYurutucu>());
        Assert.Same(finans.Al<FinansDenetimKaydi>(), finans.Al<FinansDenetimKaydi>());
        Assert.Same(finans.Al<FinansFiyatBelirleyici>(), finans.Al<FinansFiyatBelirleyici>());
    }

    private sealed class Kullanici : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
    }
}
