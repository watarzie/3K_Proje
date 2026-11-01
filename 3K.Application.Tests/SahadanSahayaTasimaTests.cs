using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Application.Features.ProjeIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

/// <summary>
/// Sahadan sahaya taşıma (bellek içi; SQL/transaction kanıtı değildir): satır kopyalanmaz, sayaçlar ve kök bağ korunur,
/// aynı miktar iki sahada sayılmaz, her taşıma iz bırakır; engelde, tekrar istekte ve ret durumunda veri değişmez.
/// </summary>
public class SahadanSahayaTasimaTests
{
    private const int KaynakSaha = 2, HedefSaha = 3;

    [Fact]
    public async Task SatirTasima_SayaclarKokBagVeDefterHedefeGecer()
    {
        var k = new Kurgu();

        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1]);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var satir = k.Satir(Kurgu.S1);
        var hedefCeki = Assert.Single(k.Uow.Repo<Ceki>().Rows, c => c.ProjeId == HedefSaha);
        Assert.Equal((int)CekiTipi.EksikTamamlama, hedefCeki.CekiTipiId);
        Assert.Equal(hedefCeki.Id, satir.CekiId);
        Assert.Equal((101, 4m, 4m), (satir.KaynakCekiSatiriId!.Value, satir.GelenMiktar, satir.IstenenAdet));

        var hedefSandik = Assert.Single(k.Uow.Repo<Sandik>().Rows, s => s.ProjeId == HedefSaha && s.Id != 31);
        // Numara verilmedi: hedefin sıradaki numarası (en büyük "3" + 1), kaynak sandığın numarası ("1") değil.
        Assert.Equal("4", hedefSandik.SandikNo);
        // Kaynak sandıkta ürün kaldığı için kutu taşınmadı: yeni sandığa ölçü/ağırlık kopyalanmaz (aynı kutu iki kez sayılmaz).
        Assert.Equal(((string?)null, (decimal?)null, (decimal?)null, (decimal?)null, (decimal?)null),
            (hedefSandik.Ad, hedefSandik.Boy, hedefSandik.En, hedefSandik.NetKg, hedefSandik.GrossKg));
        var icerik = k.Uow.Repo<SandikIcerik>().Rows.Single(i => i.Id == 301);
        Assert.Equal((hedefSandik.Id, 4m), (icerik.SandikId, icerik.KonulanAdet));

        var kalem = k.Uow.Repo<SahaAktarimKalemi>().Rows.Single(x => x.Id == 401);
        Assert.Equal((HedefSaha, hedefSandik.Id, sonuc.Value!.SahaAktarimId), (kalem.SahaProjeId, kalem.SahaSandikId!.Value, kalem.SahaAktarimId));
        Assert.Equal((int)SahaAktarimDurum.Planlandi, kalem.DurumId);
        // Kök satırın aktif aktarım toplamı değişmez: aynı miktar iki sahada sayılmaz.
        Assert.Equal(4m, k.Uow.Repo<SahaAktarimKalemi>().Rows.Where(x => x.KaynakCekiSatiriId == 101 && x.DurumId == (int)SahaAktarimDurum.Planlandi).Sum(x => x.Miktar));

        var iz = Assert.Single(k.Uow.Repo<SahaTasimaKalemi>().Rows);
        Assert.Equal(("PA723-01-SAHA", "1", "PA573-01-SAHA", "4", 4m, 501, 401),
            (iz.KaynakSahaProjeNo, iz.KaynakSahaSandikNo, iz.HedefSahaProjeNo, iz.HedefSahaSandikNo, iz.Miktar, iz.OncekiSahaAktarimId!.Value, iz.SahaAktarimKalemiId!.Value));
        var baslik = k.Uow.Repo<SahaAktarim>().Rows.Single(a => a.Id == sonuc.Value.SahaAktarimId);
        Assert.Equal((HedefSaha, (int?)KaynakSaha), (baslik.SahaProjeId, baslik.KaynakSahaProjeId));

        // Kaynak sandıkta başka ürün kaldığı için sandık boşalmaz.
        Assert.Equal((int)SandikDurum.Hazirlaniyor, k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 21).DurumId);
        Assert.Contains(101, k.Saha.SenkronizeEdilen);
        Assert.Contains(k.Hareket.Rows, h => h.ProjeId == KaynakSaha && h.Islem.Contains("giden"));
        Assert.Contains(k.Hareket.Rows, h => h.ProjeId == HedefSaha && h.Islem.Contains("gelen"));
        Assert.Contains(k.Hareket.Rows, h => h.ProjeId == 1 && h.Islem == "Saha Taşıma");
    }

    [Fact]
    public async Task SandikTasima_ManuelIcerikDahilHepsiMevcutSandigaGecer_KaynakBosKalir()
    {
        var k = new Kurgu();

        var sonuc = await k.TasiAsync(sandik: 21, hedefSandik: 31);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((2, 1), (sonuc.Value!.TasinanSatirSayisi, sonuc.Value.TasinanManuelIcerikSayisi));
        Assert.All(k.Uow.Repo<SandikIcerik>().Rows.Where(i => i.Id is 301 or 302 or 304), i => Assert.Equal(31, i.SandikId));
        Assert.Equal((int)SandikDurum.Bos, k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 21).DurumId);
        Assert.Equal(["1"], sonuc.Value.BosalanSandikNolari);
        Assert.Equal(2, k.Uow.Repo<SahaTasimaKalemi>().Rows.Count);
        // Başka sandıktaki satır yerinde kalır.
        Assert.Equal(22, k.Uow.Repo<SandikIcerik>().Rows.Single(i => i.Id == 303).SandikId);
    }

    [Fact]
    public async Task SandikTasima_YeniSandigaKutuBilgileriKopyalanir()
    {
        var k = new Kurgu();

        var sonuc = await k.TasiAsync(sandik: 21);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var yeni = k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == sonuc.Value!.HedefSandikId);
        // Numara verilmedi: hedefin sıradaki numarası (en büyük "3" + 1); kutu bilgileri kaynak sandıktan gelir.
        Assert.Equal((HedefSaha, "4", "Saha eksikleri", 1200m), (yeni.ProjeId, yeni.SandikNo, yeni.Ad, yeni.Boy!.Value));
    }

    [Fact]
    public async Task SatirTasima_KaynakSandikBosalirsaKutuBilgileriKopyalanir()
    {
        var k = new Kurgu();
        k.Uow.Repo<SandikIcerik>().Rows.RemoveAll(i => i.Id == 304);

        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1, Kurgu.S2]);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var yeni = k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == sonuc.Value!.HedefSandikId);
        Assert.Equal(("Saha eksikleri", 1200m), (yeni.Ad, yeni.Boy!.Value));
        Assert.Equal((int)SandikDurum.Bos, k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 21).DurumId);
    }

    [Fact]
    public async Task TopluSatirTasima_FarkliSandiklardanSecilenlerTekIslemdeAyniHedefSandigaGecer()
    {
        // Aktarımlar penceresindeki toplu seçim: iki ayrı kaynak sandıktan üç satır, tek başlık ve tek hedef sandık.
        var k = new Kurgu();
        var anahtar = Guid.NewGuid();

        var onizleme = await k.OnizleAsync(satirlar: [Kurgu.S1, Kurgu.S2, Kurgu.S3], hedefSandik: 31);
        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1, Kurgu.S2, Kurgu.S3], hedefSandik: 31, anahtar: anahtar);

        Assert.True(onizleme.TasinabilirMi);
        Assert.Equal(3, onizleme.Satirlar.Count);
        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((3, 31), (sonuc.Value!.TasinanSatirSayisi, sonuc.Value.HedefSandikId));
        Assert.All(k.Uow.Repo<SandikIcerik>().Rows.Where(i => i.Id is 301 or 302 or 303), i => Assert.Equal(31, i.SandikId));
        Assert.Equal(1, k.Uow.Repo<SahaAktarim>().Rows.Count(a => a.KaynakSahaProjeId == KaynakSaha));
        Assert.Equal(3, k.Uow.Repo<SahaTasimaKalemi>().Rows.Count);
        // Sandık 2'nin tek satırı gitti, boşaldı; sandık 1'de manuel içerik kaldı.
        Assert.Equal(["2"], sonuc.Value.BosalanSandikNolari);

        // Aynı anahtarla tekrar ikinci taşıma üretmez.
        var once = k.Durum();
        var tekrar = await k.TasiAsync(satirlar: [Kurgu.S3, Kurgu.S2, Kurgu.S1], hedefSandik: 31, anahtar: anahtar);
        Assert.True(tekrar.IsSuccess, tekrar.Error?.Message);
        Assert.Equal(once, k.Durum());
    }

    [Fact]
    public async Task TopluSatirTasima_TekSatirEngelliyse409VeHicbirSatirTasinmaz()
    {
        // Hepsi ya da hiçbiri: sevk edilmiş sandıktaki tek satır bütün seçimi durdurur.
        var k = new Kurgu();
        k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 22).DurumId = (int)SandikDurum.Sevkedildi;
        var once = k.Durum();

        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1, Kurgu.S2, Kurgu.S3]);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("sevk edilmiş", string.Join(" ", (IEnumerable<string>)sonuc.Error!.Issues!));
        Assert.Equal(once, k.Durum());
    }

    [Fact]
    public async Task KapanmisHedefVeKaynakSandik_IcerigiDegisinceYenidenAcilir()
    {
        var k = new Kurgu();
        k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 21).DurumId = (int)SandikDurum.Kapandi;
        k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 31).DurumId = (int)SandikDurum.Kapandi;

        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1], hedefSandik: 31);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        // Proje içi taşımayla aynı kural: içeriği değişen kapalı sandık yeniden kontrol edilmeden sevk edilemez.
        Assert.Equal((int)SandikDurum.Hazirlaniyor, k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 31).DurumId);
        Assert.Equal((int)SandikDurum.Hazirlaniyor, k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 21).DurumId);
        Assert.Contains(k.Hareket.Rows, h => h.ProjeId == HedefSaha && h.Islem == "Sandık Geri Açıldı" && h.ReferansId == "31");
        Assert.Contains(k.Hareket.Rows, h => h.ProjeId == KaynakSaha && h.Islem == "Sandık Geri Açıldı" && h.ReferansId == "21");
    }

    [Theory]
    [InlineData(ProjeDurum.SevkEdildi)]
    [InlineData(ProjeDurum.EksikSevkEdildi)]
    public async Task KaynakSahaSevkEdilmis_SevkEdilmemisSandiktakiSatirTasinir_ProjeDurumuDegismez(ProjeDurum durum)
    {
        var k = new Kurgu();
        k.Uow.Repo<Proje>().Rows.Single(p => p.Id == KaynakSaha).DurumId = (int)durum;

        var onizleme = await k.OnizleAsync(satirlar: [Kurgu.S1]);
        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1]);

        // Engel proje değil sandık düzeyindedir: sandık 21 sevk edilmemiştir (PA600-42-SAHA, Sandık 2 vakası).
        Assert.True(onizleme.TasinabilirMi);
        Assert.Empty(onizleme.Engeller);
        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(HedefSaha, k.Uow.Repo<SahaAktarimKalemi>().Rows.Single(x => x.Id == 401).SahaProjeId);
        Assert.Equal((int)durum, k.Uow.Repo<Proje>().Rows.Single(p => p.Id == KaynakSaha).DurumId);
    }

    [Theory]
    [InlineData(ProjeDurum.SevkEdildi)]
    [InlineData(ProjeDurum.EksikSevkEdildi)]
    public async Task Engel_KaynakSahaSevkEdilmis_SevkEdilmisSandiktakiSatir_409VeHicbirKayitDegismez(ProjeDurum durum)
    {
        var k = new Kurgu();
        k.Uow.Repo<Proje>().Rows.Single(p => p.Id == KaynakSaha).DurumId = (int)durum;
        k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 21).DurumId = (int)SandikDurum.Sevkedildi;
        var once = k.Durum();

        var onizleme = await k.OnizleAsync(satirlar: [Kurgu.S1]);
        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1]);

        Assert.Contains(onizleme.Engeller, e => e.StartsWith("Kaynak sandık"));
        Assert.DoesNotContain(onizleme.Engeller, e => e.StartsWith("Kaynak saha projesi"));
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Durum());
    }

    [Fact]
    public async Task NumaraVerilmezse_HedefinEnBuyukNumarasiArtiBirAtanir_KaynakNumarasiKullanilmaz()
    {
        // Kullanıcı vakası (28.10.2026): hedefte sevkli "1" ve Hazırlanıyor "2" var, kaynak sandık da "2"; eskiden "2-2"
        // açılıyor, ekranda ikinci bir "2" görünüyordu. Sayısal olmayan "2-2" sıraya katılmaz; sevkli sandık numarayı tutar.
        var k = new Kurgu();
        k.Uow.Repo<Sandik>().Rows.RemoveAll(s => s.Id == 31);
        k.Uow.Repo<Sandik>().Rows.AddRange(
        [
            new Sandik { Id = 32, ProjeId = HedefSaha, SandikNo = "2", DurumId = (int)SandikDurum.Hazirlaniyor },
            new Sandik { Id = 33, ProjeId = HedefSaha, SandikNo = "01", DurumId = (int)SandikDurum.Sevkedildi },
            new Sandik { Id = 34, ProjeId = HedefSaha, SandikNo = "2-2", DurumId = (int)SandikDurum.Sevkedildi }
        ]);

        var onizleme = await k.OnizleAsync(satirlar: [Kurgu.S3]);
        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S3]);

        Assert.Equal(("3", true), (onizleme.HedefSandikNo, onizleme.YeniSandik));
        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal("3", sonuc.Value!.HedefSandikNo);
        Assert.Single(k.Uow.Repo<Sandik>().Rows, s => s.ProjeId == HedefSaha && s.SandikNo == "3");
    }

    [Fact]
    public void SandikNumarasiKurali_SiradakiEnBuyukSayisalArtiBir_DoluIstenendeSiradaki()
    {
        // Sayısal olmayan numaralar ("2-2", "1-SH", "M-7") sıraya katılmaz; çeki biçimi "05" = 5.
        Assert.Equal("6", KoliNoKurali.SiradakiNumara(["1", "01", "2-2", "05", "1-SH", "M-7", null, " "]));
        Assert.Equal("1", KoliNoKurali.SiradakiNumara([]));
        Assert.Equal("1", KoliNoKurali.SiradakiNumara(["A", "2-2"]));

        var kullanilan = new HashSet<string>(["1", "2"], StringComparer.OrdinalIgnoreCase);
        Assert.Equal("4", KoliNoKurali.BosNumaraYaDaSiradaki(" 4 ", kullanilan));
        Assert.Equal("5", KoliNoKurali.BosNumaraYaDaSiradaki("02", kullanilan)); // dolu → sıradaki (4 alındı)
        Assert.Equal("6", KoliNoKurali.BosNumaraYaDaSiradaki(null, kullanilan));
        Assert.Equal("7", KoliNoKurali.BosNumaraYaDaSiradaki("6", kullanilan)); // aynı işlemde alınan numara da dolu
        Assert.Equal(["1", "2", "4", "5", "6", "7"], kullanilan.Order());
    }

    [Theory]
    [InlineData("2")]
    [InlineData("02")]
    [InlineData(" 2 ")]
    public async Task VerilenNumaraHedefteVarsa_Engel409_SandikAcilmaz(string verilen)
    {
        var k = new Kurgu();
        k.Uow.Repo<Sandik>().Rows.Add(new Sandik { Id = 32, ProjeId = HedefSaha, SandikNo = "2", DurumId = (int)SandikDurum.Hazirlaniyor });
        var once = k.Durum();

        var onizleme = await k.OnizleAsync(satirlar: [Kurgu.S3], yeniNo: verilen);
        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S3], yeniNo: verilen);

        Assert.False(onizleme.TasinabilirMi);
        Assert.Contains(onizleme.Engeller, e => e.Contains("numaralı sandık zaten var"));
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Durum());
    }

    [Fact]
    public async Task VerilenNumaraBossa_OAdlaAcilir()
    {
        var k = new Kurgu();

        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S3], yeniNo: " 7 ");

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal("7", sonuc.Value!.HedefSandikNo);
    }

    [Fact]
    public async Task Engel_SevkEdilmisKaynakSandik_409VeHicbirKayitDegismez()
    {
        var k = new Kurgu();
        k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == 21).DurumId = (int)SandikDurum.Sevkedildi;
        var once = k.Durum();

        var onizleme = await k.OnizleAsync(satirlar: [Kurgu.S1]);
        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1]);

        Assert.False(onizleme.TasinabilirMi);
        Assert.Contains(onizleme.Engeller, e => e.Contains("sevk edilmiş"));
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Durum());
    }

    [Fact]
    public async Task Engel_BolunmusSatir_409VeHicbirKayitDegismez()
    {
        var k = new Kurgu();
        k.Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik { Id = 305, SandikId = 22, CekiSatiriId = Kurgu.S2, TahsisMiktari = 1 });
        var once = k.Durum();

        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S2]);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("bölünmüş", string.Join(" ", (IEnumerable<string>)sonuc.Error!.Issues!));
        Assert.Equal(once, k.Durum());
    }

    [Fact]
    public async Task Engel_AcikProjedenKarsilama_409VeHicbirKayitDegismez()
    {
        var k = new Kurgu();
        k.Uow.Repo<ProjeTransfer>().Rows.Add(new ProjeTransfer
        {
            Id = 1, KaynakProjeId = 9, HedefProjeId = KaynakSaha, KaynakCekiSatiriId = 900, HedefCekiSatiriId = Kurgu.S1,
            Miktar = 1, DurumId = (int)ProjeTransferDurum.Aktif
        });
        var once = k.Durum();

        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1]);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Durum());
    }

    [Fact]
    public async Task Engel_HedefSahaTamamenSevkEdilmis_409VeHicbirKayitDegismez()
    {
        var k = new Kurgu();
        k.Uow.Repo<Proje>().Rows.Single(p => p.Id == HedefSaha).DurumId = (int)ProjeDurum.SevkEdildi;
        var once = k.Durum();

        var onizleme = await k.OnizleAsync(satirlar: [Kurgu.S1]);
        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1]);

        Assert.Contains(onizleme.Engeller, e => e.StartsWith("Hedef saha projesi"));
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Durum());
    }

    [Theory]
    [InlineData(31, "3")]
    [InlineData(null, "10")] // sıradaki numara: hedefte "3" ve sevkli "9" var
    public async Task HedefSahaEksikSevkEdilmis_SevkEdilmemisYaDaYeniSandigaTasinir_ProjeDurumuDegismez(int? hedefSandik, string beklenenNo)
    {
        // Kullanıcı kararı (24.10.2026): Eksik Sevk Edildi saha da hedeftir; ürün sevk edilmemiş ya da yeni sandığa gider.
        var k = new Kurgu();
        k.Uow.Repo<Proje>().Rows.Single(p => p.Id == HedefSaha).DurumId = (int)ProjeDurum.EksikSevkEdildi;
        k.Uow.Repo<Sandik>().Rows.Add(new Sandik { Id = 33, ProjeId = HedefSaha, SandikNo = "9", DurumId = (int)SandikDurum.Sevkedildi });

        var onizleme = await k.OnizleAsync(satirlar: [Kurgu.S1], hedefSandik: hedefSandik);
        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1], hedefSandik: hedefSandik);

        Assert.Empty(onizleme.Engeller);
        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(beklenenNo, sonuc.Value!.HedefSandikNo);
        var hedefSandikKaydi = k.Uow.Repo<Sandik>().Rows.Single(s => s.Id == sonuc.Value.HedefSandikId);
        Assert.Equal((HedefSaha, (int)SandikDurum.Hazirlaniyor), (hedefSandikKaydi.ProjeId, hedefSandikKaydi.DurumId));
        Assert.Equal(HedefSaha, k.Uow.Repo<SahaAktarimKalemi>().Rows.Single(x => x.Id == 401).SahaProjeId);
        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, k.Uow.Repo<Proje>().Rows.Single(p => p.Id == HedefSaha).DurumId);
    }

    [Fact]
    public async Task Engel_HedefSahaEksikSevkEdilmis_SevkEdilmisHedefSandik_409VeHicbirKayitDegismez()
    {
        var k = new Kurgu();
        k.Uow.Repo<Proje>().Rows.Single(p => p.Id == HedefSaha).DurumId = (int)ProjeDurum.EksikSevkEdildi;
        k.Uow.Repo<Sandik>().Rows.Add(new Sandik { Id = 33, ProjeId = HedefSaha, SandikNo = "9", DurumId = (int)SandikDurum.Sevkedildi });
        var once = k.Durum();

        var onizleme = await k.OnizleAsync(satirlar: [Kurgu.S1], hedefSandik: 33);
        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1], hedefSandik: 33);

        Assert.Contains(onizleme.Engeller, e => e.StartsWith("Hedef sandık 9"));
        Assert.DoesNotContain(onizleme.Engeller, e => e.StartsWith("Hedef saha projesi"));
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Durum());
    }

    [Fact]
    public async Task Engel_SevkiyatKapsamindakiSatir_409()
    {
        var k = new Kurgu();
        k.Uow.Repo<SahaAktarimKalemi>().Rows.Single(x => x.Id == 401).SevkiyatKapsamindaMi = true;
        var once = k.Durum();

        var sonuc = await k.TasiAsync(satirlar: [Kurgu.S1]);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(once, k.Durum());
    }

    [Fact]
    public async Task AyniAnahtarlaTekrar_AyniSonucuDoner_YeniKayitYazmaz()
    {
        var k = new Kurgu();
        var anahtar = Guid.NewGuid();

        var ilk = await k.TasiAsync(satirlar: [Kurgu.S1], anahtar: anahtar);
        var durum = k.Durum();
        var tekrar = await k.TasiAsync(satirlar: [Kurgu.S1], anahtar: anahtar);

        Assert.True(tekrar.IsSuccess, tekrar.Error?.Message);
        Assert.Equal(ilk.Value!.SahaAktarimId, tekrar.Value!.SahaAktarimId);
        Assert.Equal(durum, k.Durum());
    }

    [Fact]
    public async Task AyniAnahtarFarkliTasima_409()
    {
        var k = new Kurgu();
        var anahtar = Guid.NewGuid();
        await k.TasiAsync(satirlar: [Kurgu.S1], anahtar: anahtar);
        var durum = k.Durum();

        var farkli = await k.TasiAsync(satirlar: [Kurgu.S2], anahtar: anahtar);

        Assert.Equal(409, farkli.StatusCode);
        Assert.Equal(durum, k.Durum());
    }

    [Fact]
    public async Task TasimaBasligininTamamiGeriAlinmaz_TersYondeTasimaIstenir()
    {
        var k = new Kurgu();
        var tasima = await k.TasiAsync(satirlar: [Kurgu.S1]);
        var durum = k.Durum();

        var geriAl = await new SahaAktarimiGeriAlCommandHandler(k.Uow, new OrtakUser(), k.Hareket, k.Saha)
            .Handle(new SahaAktarimiGeriAlCommand { SahaAktarimId = tasima.Value!.SahaAktarimId }, CancellationToken.None);

        Assert.Equal(409, geriAl.StatusCode);
        Assert.Contains("ters yönde", geriAl.Error!.Message);
        Assert.Equal(durum, k.Durum());

        var ters = await k.TasiAsync(satirlar: [Kurgu.S1], kaynak: HedefSaha, hedef: KaynakSaha, hedefSandik: 21);
        Assert.True(ters.IsSuccess, ters.Error?.Message);
        Assert.Equal(21, k.Uow.Repo<SandikIcerik>().Rows.Single(i => i.Id == 301).SandikId);
        Assert.Equal(2, k.Uow.Repo<SahaTasimaKalemi>().Rows.Count);
    }

    [Fact]
    public async Task Onizleme_VeriDegistirmez()
    {
        var k = new Kurgu();
        var once = k.Durum();

        var onizleme = await k.OnizleAsync(sandik: 21);

        Assert.True(onizleme.TasinabilirMi);
        Assert.Equal((2, 1, "4"), (onizleme.Satirlar.Count, onizleme.ManuelIcerikSayisi, onizleme.HedefSandikNo));
        Assert.Equal(["1"], onizleme.BosalacakSandikNolari);
        Assert.Equal(once, k.Durum());
    }

    private sealed class Kurgu
    {
        public const int S1 = 201, S2 = 202, S3 = 203;

        public OrtakMemoryUow Uow { get; } = new();
        public OrtakHareket Hareket { get; } = new();
        public OrtakSaha Saha { get; } = new();

        public Kurgu()
        {
            Uow.Repo<Proje>().Rows.AddRange(
            [
                new Proje { Id = 1, ProjeNo = "PA723-01", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.EksikSevkEdildi },
                new Proje { Id = KaynakSaha, ProjeNo = "PA723-01-SAHA", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor },
                new Proje { Id = HedefSaha, ProjeNo = "PA573-01-SAHA", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor }
            ]);
            Uow.Repo<Ceki>().Rows.AddRange(
            [
                new Ceki { Id = 1, ProjeId = 1, CekiTipiId = (int)CekiTipi.Normal },
                new Ceki { Id = 2, ProjeId = KaynakSaha, CekiTipiId = (int)CekiTipi.EksikTamamlama, TamamlamaNo = 1 }
            ]);
            Uow.Repo<Sandik>().Rows.AddRange(
            [
                new Sandik { Id = 21, ProjeId = KaynakSaha, SandikNo = "1", Ad = "Saha eksikleri", Boy = 1200, DurumId = (int)SandikDurum.Hazirlaniyor },
                new Sandik { Id = 22, ProjeId = KaynakSaha, SandikNo = "2", DurumId = (int)SandikDurum.Hazirlaniyor },
                new Sandik { Id = 31, ProjeId = HedefSaha, SandikNo = "3", DurumId = (int)SandikDurum.Hazirlaniyor }
            ]);
            Uow.Repo<SahaAktarim>().Rows.Add(new SahaAktarim { Id = 501, SahaProjeId = KaynakSaha, KaynakProjeId = 1 });

            Urun(101, S1, 21, 301, 401, istenen: 4, gelen: 4);
            Urun(102, S2, 21, 302, 402, istenen: 2, gelen: 0);
            Urun(103, S3, 22, 303, 403, istenen: 5, gelen: 0);
            Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik { Id = 304, SandikId = 21, Isim = "Cıvata", Miktar = 10, KonulanAdet = 10 });
        }

        private void Urun(int kokId, int sahaId, int sandikId, int icerikId, int kalemId, decimal istenen, decimal gelen)
        {
            Uow.Repo<CekiSatiri>().Rows.Add(new CekiSatiri { Id = kokId, CekiId = 1, SiraNo = kokId - 100, BarkodNo = $"K{kokId}", Aciklama = $"Ürün {kokId}", IstenenAdet = istenen });
            Uow.Repo<CekiSatiri>().Rows.Add(new CekiSatiri
            {
                Id = sahaId, CekiId = 2, KaynakCekiSatiriId = kokId, SiraNo = sahaId - 200, BarkodNo = $"K{kokId}", Aciklama = $"Ürün {kokId}",
                IstenenAdet = istenen, GelenMiktar = gelen, CekideGecenSandikNo = sandikId == 21 ? "1" : "2"
            });
            Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik { Id = icerikId, SandikId = sandikId, CekiSatiriId = sahaId, TahsisMiktari = istenen, KonulanAdet = gelen });
            Uow.Repo<SahaAktarimKalemi>().Rows.Add(new SahaAktarimKalemi
            {
                Id = kalemId, SahaAktarimId = 501, KaynakProjeId = 1, SahaProjeId = KaynakSaha, KaynakCekiSatiriId = kokId,
                SahaCekiSatiriId = sahaId, SahaSandikId = sandikId, Miktar = istenen, DurumId = (int)SahaAktarimDurum.Planlandi
            });
        }

        public CekiSatiri Satir(int id) => Uow.Repo<CekiSatiri>().Rows.Single(s => s.Id == id);

        public Task<Result<SahadanSahayaTasimaSonucDto>> TasiAsync(
            int[]? satirlar = null, int? sandik = null, int? hedefSandik = null, Guid? anahtar = null,
            int kaynak = KaynakSaha, int hedef = HedefSaha, string? yeniNo = null) =>
            new SahadanSahayaTasiCommandHandler(Uow, Hareket, new OrtakUser(), Saha).Handle(new SahadanSahayaTasiCommand
            {
                IslemAnahtari = anahtar ?? Guid.NewGuid(),
                KaynakSahaProjeId = kaynak,
                SahaCekiSatiriIds = satirlar?.ToList() ?? [],
                KaynakSandikId = sandik,
                HedefSahaProjeId = hedef,
                HedefSandikId = hedefSandik,
                YeniSandikNo = yeniNo
            }, CancellationToken.None);

        public async Task<SahadanSahayaTasimaOnizlemeDto> OnizleAsync(int[]? satirlar = null, int? sandik = null, int? hedefSandik = null, string? yeniNo = null)
        {
            var sonuc = await new SahadanSahayaTasimaOnizleQueryHandler(Uow, new LookupStub()).Handle(new SahadanSahayaTasimaOnizleQuery
            {
                KaynakSahaProjeId = KaynakSaha,
                SahaCekiSatiriIds = satirlar?.ToList() ?? [],
                KaynakSandikId = sandik,
                HedefSahaProjeId = HedefSaha,
                HedefSandikId = hedefSandik,
                YeniSandikNo = yeniNo
            }, CancellationToken.None);
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            return sonuc.Value!;
        }

        /// <summary>Taşımanın dokunduğu her alanın anlık görüntüsü; ret ve tekrar testleri bununla karşılaştırır.</summary>
        public string Durum() => string.Join("|",
            Uow.Repo<CekiSatiri>().Rows.OrderBy(s => s.Id).Select(s => $"{s.Id}:{s.CekiId}:{s.SiraNo}:{s.FiiliSandikNo}"),
            string.Join(",", Uow.Repo<SandikIcerik>().Rows.OrderBy(i => i.Id).Select(i => $"{i.Id}:{i.SandikId}")),
            string.Join(",", Uow.Repo<SahaAktarimKalemi>().Rows.OrderBy(x => x.Id).Select(x => $"{x.Id}:{x.SahaProjeId}:{x.SahaSandikId}:{x.SahaAktarimId}:{x.DurumId}")),
            string.Join(",", Uow.Repo<Sandik>().Rows.OrderBy(s => s.Id).Select(s => $"{s.Id}:{s.ProjeId}:{s.SandikNo}:{s.DurumId}")),
            Uow.Repo<SahaAktarim>().Rows.Count,
            Uow.Repo<SahaTasimaKalemi>().Rows.Count,
            Uow.Repo<Ceki>().Rows.Count,
            Hareket.Rows.Count);
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => id.ToString();
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }
}
