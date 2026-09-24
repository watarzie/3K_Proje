using System.Linq.Expressions;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class AmbalajUretimV2Tests
{
    [Theory]
    [InlineData(AmbalajSandikCinsi.Kontrplak)]
    [InlineData(AmbalajSandikCinsi.Katlanir)]
    [InlineData(AmbalajSandikCinsi.Diger)]
    public async Task U1_IlkForm_NonwoodOlcuVeAdediKorur_HacimVeParcaUretmez(AmbalajSandikCinsi cins)
    {
        var k = Kayit(1, cins); k.Boy = 50; k.En = 60; k.Yukseklik = 70; k.KaynakKayitId = 10;
        k.KaynakModul = AmbalajKaynakModulu.Sandik;
        var uow = new Uow().Add(k);
        AmbalajUretimYardimcilari.M3DegerleriniHesapla(k);
        var sonuc = await FormHandler(uow).Handle(new() { KayitIdleri = [1], IdempotencyAnahtari = Guid.NewGuid() }, default);
        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var kalem = Assert.Single(sonuc.Value!.Form.Kalemler);
        Assert.Equal(50, kalem.DisOlculer.Boy); Assert.Equal(2, kalem.Adet);
        Assert.False(kalem.M3HesaplanabilirMi); Assert.Null(kalem.NetM3); Assert.Null(kalem.SarfM3);
        Assert.Null(sonuc.Value.Form.NetM3); Assert.Null(sonuc.Value.Form.ToplamM3);
        Assert.Empty(kalem.Parcalar); Assert.Null(k.M3Override); Assert.Equal(0, k.ToplamM3);
        Assert.Equal(AmbalajUretimDurumu.Uretimde, k.UretimDurumu);
    }

    [Theory]
    [InlineData("TRANSFORMATÖR", null, true)]
    [InlineData("Transformatör sandığı", null, true)]
    [InlineData("Transformatör (Yağsız)", null, true)]
    [InlineData(null, "TRANSFORMER CASE", true)]
    [InlineData(null, "TRANSFORMER MAIN BODY", true)]
    [InlineData("Bushing AH sandığı", null, true)]
    [InlineData(null, "BUSHING YG CASE", true)]
    [InlineData("AG Bushing", null, true)]
    [InlineData("YG N Bushing", null, true)]
    [InlineData("Bushing bağlantı sandığı", null, true)]
    [InlineData("Aksesuar", "HV Bushing", true)]
    [InlineData("YG Busingler", null, true)]
    [InlineData("Trafo Merdiveni + Boru Donanım", null, false)]
    [InlineData("Trafo Merdiveni + Boru Donanım", "Transformer Ladder + Pipe Assembly", false)]
    [InlineData("Trafo Montaj", null, false)]
    [InlineData("Transformatör merdiveni", null, false)]
    [InlineData(null, "Transformer Ladder", false)]
    [InlineData("Mekanik 1", null, false)]
    [InlineData("Parafudr", null, false)]
    public void U2_Siniflandirma_NumaraSezgisiKullanmaz(string? ad, string? ing, bool expected) =>
        Assert.Equal(expected, AmbalajUretimPolitikasi.VarsayilanYapilmazMi(ad, ing));

    [Fact]
    public void U1_KaynakCinsi_KontraKorunur_BilinmeyenAhsapOlmaz()
    {
        Assert.Equal(AmbalajSandikCinsi.Kontrplak, AmbalajUretimPolitikasi.KaynakCinsi(3));
        Assert.Equal(AmbalajSandikCinsi.Diger, AmbalajUretimPolitikasi.KaynakCinsi(178));
    }

    [Fact]
    public async Task K03_TrafoMerdiveniKaynaktaVarsayilanDahilOlur()
    {
        var proje = new Proje { Id = 20, ProjeNo = "K03", Musteri = "Test", ProjeTipiId = (int)ProjeTipi.Normal };
        var source = new Sandik { Id = 30, ProjeId = 20, SandikNo = "18",
            Ad = "Trafo Merdiveni + Boru Donanım", TipId = (int)SandikTipi.AhsapKapali,
            Boy = 5300, En = 1200, Yukseklik = 1200 };
        var uow = new Uow().Add(proje).Add(source);
        var sync = new AmbalajKaynaklariSenkronizeEtCommandHandler(uow, new ApprovalUser(), new Finans(), new Roles());

        var result = await sync.Handle(new() { ProjeId = 20 }, default);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var row = Assert.Single(uow.Items<AmbalajUretimKaydi>());
        Assert.Equal("18", row.SandikNo);
        Assert.True(row.AmbalajaDahil);
        var plan = AmbalajPlanlamaYardimcisi.PlanDtoOlustur(proje, "Normal", [source], [row]);
        Assert.True(Assert.Single(plan.Kalemler).AmbalajaDahilMi);
    }

    [Theory]
    [InlineData("TRANSFORMATÖR")]
    [InlineData("Bushing AH sandığı")]
    [InlineData("BUSHING YG CASE")]
    [InlineData("AG Bushing")]
    [InlineData("YG N Bushing")]
    public async Task K03K04_KaynakVarsayilanHaric_YetkiliKararResyncteKorunur_FormaDahilOlur(string sourceName)
    {
        var proje = new Proje { Id = 20, ProjeNo = "K03", Musteri = "Test", ProjeTipiId = (int)ProjeTipi.Normal };
        var source = new Sandik { Id = 30, ProjeId = 20, SandikNo = "1", Ad = sourceName,
            TipId = (int)SandikTipi.AhsapKapali, Boy = 2500, En = 1500, Yukseklik = 1800 };
        var uow = new Uow().Add(proje).Add(source);
        var actor = new ApprovalUser();
        var sync = new AmbalajKaynaklariSenkronizeEtCommandHandler(uow, actor, new Finans(), new Roles());
        var initial = await sync.Handle(new() { ProjeId = 20 }, default);
        Assert.True(initial.IsSuccess, initial.Error?.Message);
        var row = Assert.Single(uow.Items<AmbalajUretimKaydi>());
        Assert.False(row.AmbalajaDahil); Assert.False(row.UretimeAlindi);
        var plan = AmbalajPlanlamaYardimcisi.PlanDtoOlustur(proje, "Normal", [source], [row]);
        Assert.False(Assert.Single(plan.Kalemler).AmbalajaDahilMi);
        Assert.Equal(0, plan.GerekliSandikAdedi); Assert.Equal(0, plan.SeciliSandikAdedi);
        Assert.Equal(0, AmbalajPlanlamaYardimcisi.ProjeOzetDtoOlustur(proje, "Normal", [source], [row]).ToplamSandikAdedi);
        var form = await FormHandler(uow).Handle(new() { KayitIdleri = [row.Id], IdempotencyAnahtari = Guid.NewGuid() }, default);
        Assert.False(form.IsSuccess); Assert.Empty(uow.Items<AmbalajUretimFormuSurumu>());
        var decision = new AmbalajUretimSecimGuncelleCommand { Id = row.Id, AmbalajaDahil = true, UretimeAlindi = false };
        var denied = await new AmbalajUretimSecimGuncelleCommandHandler(uow, actor, new Finans(), new Roles(false)).Handle(decision, default);
        Assert.Equal(403, denied.StatusCode); Assert.False(row.AmbalajaDahil);
        var allowed = await new AmbalajUretimSecimGuncelleCommandHandler(uow, actor, new Finans(), new Roles()).Handle(decision, default);
        Assert.True(allowed.IsSuccess, allowed.Error?.Message);
        source.Boy = 2600;
        Assert.True((await sync.Handle(new() { ProjeId = 20 }, default)).IsSuccess);
        Assert.True(row.AmbalajaDahil); Assert.Equal(2600, row.Boy);
        Assert.Equal(1, AmbalajPlanlamaYardimcisi.ProjeOzetDtoOlustur(proje, "Normal", [source], [row]).ToplamSandikAdedi);
        form = await FormHandler(uow).Handle(new() { KayitIdleri = [row.Id], IdempotencyAnahtari = Guid.NewGuid() }, default);
        Assert.True(form.IsSuccess, form.Error?.Message);
        Assert.Equal(row.Id, Assert.Single(form.Value!.Form.Kalemler).KayitId);
        Assert.Equal(AmbalajUretimDurumu.Uretimde, row.UretimDurumu);
        Assert.NotEmpty(uow.Items<AmbalajUretimHareketi>());
        Assert.All(uow.Items<AmbalajUretimHareketi>(), x => Assert.Equal(7, x.KullaniciId));
    }

    [Fact]
    public async Task U3_IlkFormRetry_TekSurumVeGecis_SonrakiSurumSnapshotiDegistirmez()
    {
        var k = Kayit(1); var uow = new Uow().Add(k); var handler = FormHandler(uow);
        var cmd = new AmbalajFormOlusturCommand { KayitIdleri = [1], IdempotencyAnahtari = Guid.NewGuid() };
        var ilk = await handler.Handle(cmd, default); var retry = await handler.Handle(cmd, default);
        Assert.True(ilk.IsSuccess, ilk.Error?.Message); Assert.Equal(ilk.Value!.Id, retry.Value!.Id);
        Assert.Single(uow.Items<AmbalajUretimFormuSurumu>());
        cmd.Aciklama = "Başka istek";
        Assert.Equal(409, (await handler.Handle(cmd, default)).StatusCode);
        k.UretimDurumu = AmbalajUretimDurumu.Tamamlandi; k.Boy = 3000;
        var yeni = await handler.Handle(new() { KayitIdleri = [1], IdempotencyAnahtari = Guid.NewGuid(), YenidenOlustur = true, Aciklama = "Ölçü düzeltildi" }, default);
        Assert.True(yeni.IsSuccess, yeni.Error?.Message); Assert.Equal(2, yeni.Value!.Surum);
        Assert.Equal(AmbalajUretimDurumu.Tamamlandi, k.UretimDurumu);
        var eski = await AmbalajYasamDongusuYardimcisi.FormDtoAsync(uow.Items<AmbalajUretimFormuSurumu>()[0], new Roles(), new User(), default);
        Assert.Equal(2500, eski.Form.Kalemler[0].IcOlculer.Boy);
    }

    [Fact]
    public async Task U3_GecersizTopluSecim_HicbirDurumuDegistirmez()
    {
        var bir = Kayit(1); var iki = Kayit(2); iki.AmbalajaDahil = false;
        var uow = new Uow().Add(bir, iki);
        var result = await FormHandler(uow).Handle(new() { KayitIdleri = [1,2], IdempotencyAnahtari = Guid.NewGuid() }, default);
        Assert.False(result.IsSuccess); Assert.False(bir.UretimeAlindi);
        Assert.Equal(AmbalajUretimDurumu.Planlandi, bir.UretimDurumu);
        Assert.Empty(uow.Items<AmbalajUretimFormuSurumu>());
    }

    [Fact]
    public async Task U4_TamamlaAcTamamla_TekGerceklesme_GuncelKayitDegisimiGecmisiDegistirmez()
    {
        var k = Kayit(1); k.UretimeAlindi = true; k.UretimDurumu = AmbalajUretimDurumu.Uretimde;
        var uow = new Uow().Add(k); var h = new AmbalajUretimDurumuGuncelleCommandHandler(uow,new User(),new Finans(),new Roles());
        await h.Handle(new() { Id = 1, Durum = AmbalajUretimDurumu.Tamamlandi }, default);
        await h.Handle(new() { Id = 1, Durum = AmbalajUretimDurumu.Uretimde, Aciklama = "Kontrol" }, default);
        k.Adet = 9;
        await h.Handle(new() { Id = 1, Durum = AmbalajUretimDurumu.Tamamlandi }, default);
        var gercek = Assert.Single(uow.Items<AmbalajUretimGerceklesmesi>());
        Assert.Equal(2, gercek.Adet);
    }

    [Fact]
    public async Task U4_U5_TarihSinirlari_GunAyProjeEsit_DuzeltmedeYalnizSonSurum()
    {
        var ahsap = Kayit(1); ahsap.Adet = 185; ahsap.HesaplananToplamM3 = 420.50m; ahsap.TamamlanmaTarihi = new(2026,9,1);
        var kontra = Kayit(2, AmbalajSandikCinsi.Kontrplak); kontra.Adet = 26; kontra.TamamlanmaTarihi = new(2026,9,30,23,59,59);
        var katlanir = Kayit(3, AmbalajSandikCinsi.Katlanir); katlanir.Adet = 14; katlanir.TamamlanmaTarihi = new(2026,10,1);
        var uow = new Uow().Add(ahsap, kontra, katlanir);
        foreach (var k in new[] { ahsap, kontra, katlanir }) await AmbalajYasamDongusuYardimcisi.GerceklesmeyiKaydetAsync(uow,k,7);
        var handler = new AmbalajGerceklesenRaporQueryHandler(uow,new Roles(),new User(),new AmbalajRaporDosyaService());
        var query = new GetAmbalajGerceklesenRaporQuery { Baslangic = new(2026,9,1), Bitis = new(2026,9,30) };
        var result = (await handler.Handle(query,default)).Value!;
        Assert.Equal(211,result.Toplam.Adet); Assert.Equal(420.50m,result.Toplam.NetM3);
        Assert.Equal(result.Toplam.Adet,result.Gunler.Sum(x=>x.Adet)); Assert.Equal(result.Toplam.NetM3,result.Projeler.Sum(x=>x.NetM3));
        Assert.Null(result.Cinsler.Single(x=>x.Anahtar.Contains("Kontrplak")).NetM3);
        Assert.Equal(result.Toplam.Adet,result.Aylar.Sum(x=>x.Adet));
        Assert.Equal(result.Toplam.NetM3,result.Gunler.Sum(x=>x.NetM3));
        Assert.Equal(result.Toplam.NetM3,result.Aylar.Sum(x=>x.NetM3));
        var eski = uow.Items<AmbalajUretimGerceklesmesi>()[0];
        var duzelt = new AmbalajGerceklesmeDuzeltCommandHandler(uow,new User(),new Roles());
        var correction = await duzelt.Handle(new() { Id=eski.Id, BeklenenSurum=1, Tarih=new(2026,9,2), Adet=184, NetM3=400, SarfM3=40, Gerekce="Bir adet fiilen üretilmedi" },default);
        Assert.True(correction.IsSuccess,correction.Error?.Message);
        result = (await handler.Handle(query,default)).Value!;
        Assert.Equal(210,result.Toplam.Adet); Assert.Equal(400,result.Toplam.NetM3);
        Assert.Equal(185,eski.Adet); Assert.Equal(4,uow.Items<AmbalajUretimGerceklesmesi>().Count);
    }

    [Fact]
    public async Task K11_UcCins_185Ahsap26Kontra14Katlanir_FinansManuelM3UretimeKarismaz()
    {
        var wood = Kayit(1); wood.Adet = 185; wood.HesaplananToplamM3 = 420.50m;
        var contra = Kayit(2, AmbalajSandikCinsi.Kontrplak); contra.Adet = 26;
        var fold = Kayit(3, AmbalajSandikCinsi.Katlanir); fold.Adet = 14;
        var finance = new FinansIsKaydi { Id = 1, KaynakTuru = "AMBALAJURETIM",
            KaynakKayitId = contra.IsAkisKimligi.ToString("D"), SandikCinsi = AmbalajSandikCinsi.Kontrplak,
            FinansMiktariManuel = true, BirimM3 = 9, ToplamM3 = 234, Adet = 26 };
        var uow = new Uow().Add(wood, contra, fold).Add(finance);
        foreach (var row in new[] { wood, contra, fold })
        {
            row.TamamlanmaTarihi = new(2026,9,5);
            await AmbalajYasamDongusuYardimcisi.GerceklesmeyiKaydetAsync(uow, row, 7);
        }
        var handler = new AmbalajGerceklesenRaporQueryHandler(uow, new Roles(), new User(), new AmbalajRaporDosyaService());
        var query = new GetAmbalajGerceklesenRaporQuery { Baslangic = new(2026,9,1), Bitis = new(2026,9,30) };
        var result = (await handler.Handle(query, default)).Value!;
        Assert.Equal(225, result.Toplam.Adet); Assert.Equal(420.50m, result.Toplam.NetM3);
        Assert.Equal(3, result.Cinsler.Count);
        Assert.Equal(185, Assert.Single(result.Cinsler, x => x.Anahtar == "Ahşap Kapalı").Adet);
        Assert.Equal(26, Assert.Single(result.Cinsler, x => x.Anahtar == "Kontrplak Sandık").Adet);
        Assert.Equal(14, Assert.Single(result.Cinsler, x => x.Anahtar == "Katlanır Sandık").Adet);
        Assert.All(result.Cinsler.Where(x => x.Anahtar != "Ahşap Kapalı"), x => { Assert.Null(x.NetM3); Assert.Null(x.SarfM3); });
        finance.ToplamM3 = 9999;
        Assert.Equal(420.50m, (await handler.Handle(query, default)).Value!.Toplam.NetM3);
    }

    [Fact]
    public void U3_ProjeTamamlanmasi_HerGerekliKalemiKapsar()
    {
        var a=Kayit(1); a.UretimDurumu=AmbalajUretimDurumu.Tamamlandi;
        var b=Kayit(2);
        Assert.Equal(AmbalajUretimDurumu.Uretimde,AmbalajUretimPolitikasi.ProjeDurumu([a,b]));
        b.AmbalajaDahil=false;
        Assert.Equal(AmbalajUretimDurumu.Tamamlandi,AmbalajUretimPolitikasi.ProjeDurumu([a,b]));
        Assert.Equal(AmbalajUretimDurumu.Planlandi,AmbalajUretimPolitikasi.ProjeDurumu([b]));
    }

    [Fact]
    public async Task U6_YenidenAcma_AyriIzinOlmadanHandlerCalismaz()
    {
        var k=Kayit(1); k.UretimDurumu=AmbalajUretimDurumu.Tamamlandi;
        var behavior=new AmbalajYasamDongusuBehavior<AmbalajUretimDurumuGuncelleCommand,Result<AmbalajUretimKaydiDto>>(new Uow().Add(k),new Roles(false),new User());
        var called=false;
        var result=await behavior.Handle(new(){Id=1,Durum=AmbalajUretimDurumu.Uretimde,Aciklama="Kontrol"},
            ()=>{called=true; return Task.FromResult(Result<AmbalajUretimKaydiDto>.Success(new()));},default);
        Assert.Equal(403,result.StatusCode); Assert.False(called); Assert.Equal(AmbalajUretimDurumu.Tamamlandi,k.UretimDurumu);
    }

    private static AmbalajUretimKaydi Kayit(int id, AmbalajSandikCinsi cins=AmbalajSandikCinsi.AhsapKapali)
    {
        var k=new AmbalajUretimKaydi { Id=id,ManuelProjeNo="V2",SandikNo=id.ToString(),SandikCinsi=cins,Adet=2,Boy=2500,En=1500,Yukseklik=1800,AmbalajaDahil=true };
        AmbalajUretimYardimcilari.M3DegerleriniHesapla(k); return k;
    }

    [Fact]
    public async Task U1U4_KarmaForm_CinsVeKesimDetaylariniKorur_MaskeliDosyadaOlcuVeHacimYoktur()
    {
        var wood = Kayit(1); wood.Ad = "Ahşap Radyatör Sandığı";
        var contra = Kayit(2, AmbalajSandikCinsi.Kontrplak); contra.Ad = "Kontra Kontrol Paneli"; contra.Adet = 26;
        var fold = Kayit(3, AmbalajSandikCinsi.Katlanir); fold.Ad = "Katlanır Yardımcı Parça"; fold.Adet = 14;
        var result = await FormHandler(new Uow().Add(wood, contra, fold)).Handle(new()
            { KayitIdleri = [1, 2, 3], IdempotencyAnahtari = Guid.NewGuid() }, default);
        Assert.True(result.IsSuccess);
        var form = result.Value!.Form;
        var service = new AmbalajRaporDosyaService();
        var excel = service.UretimFormuExcelOlustur(form);
        var pdf = service.UretimFormuPdfOlustur(form);
        using (var workbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(excel)))
        {
            var rows = workbook.Worksheet("Kesim Listesi").RowsUsed().Skip(1).ToList();
            Assert.Contains(rows, x => x.Cell(12).GetString() == "AP_3");
            var kontra = Assert.Single(rows, x => x.Cell(4).GetString() == "Kontrplak Sandık");
            Assert.Equal(26, kontra.Cell(5).GetValue<int>()); Assert.True(kontra.Cell(22).IsEmpty());
            Assert.Contains("uygulanmaz", kontra.Cell(14).GetString());
            Assert.Contains(rows, x => x.Cell(4).GetString() == "Katlanır Sandık" && x.Cell(5).GetValue<int>() == 14);
        }
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        var output = Environment.GetEnvironmentVariable("THREEK_URETIM_ARTIFACTS");
        if (!string.IsNullOrWhiteSpace(output))
        {
            Directory.CreateDirectory(output);
            await File.WriteAllBytesAsync(Path.Combine(output, "uretim-karma.pdf"), pdf);
            await File.WriteAllBytesAsync(Path.Combine(output, "uretim-karma.xlsx"), excel);
        }
        AmbalajYasamDongusuYardimcisi.Maskele(form, new(false, false, false, false));
        var maskedExcel = service.UretimFormuExcelOlustur(form);
        var maskedPdf = service.UretimFormuPdfOlustur(form);
        using (var workbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(maskedExcel)))
        {
            Assert.True(workbook.Worksheet("Üretim Özeti").Cell("B5").IsEmpty());
            foreach (var row in workbook.Worksheet("Kesim Listesi").RowsUsed().Skip(1))
                foreach (var col in new[] { 6, 7, 8, 9, 10, 11, 16, 17, 18, 21, 22, 23, 24, 25 }) Assert.True(row.Cell(col).IsEmpty());
        }
        if (!string.IsNullOrWhiteSpace(output))
        {
            await File.WriteAllBytesAsync(Path.Combine(output, "uretim-maskeli.pdf"), maskedPdf);
            await File.WriteAllBytesAsync(Path.Combine(output, "uretim-maskeli.xlsx"), maskedExcel);
        }
    }
    private static AmbalajFormOlusturCommandHandler FormHandler(Uow uow)=>new(uow,new User(),new Roles(),new Finans());
    private sealed class User:ICurrentUserService {public int? UserId=>7;public bool IsAuthenticated=>true;public string? MenuKod=>null;}
    private sealed class ApprovalUser:ICurrentUserService {public int? UserId=>99;public int? IslemKullaniciId=>7;public bool IsAuthenticated=>true;public string? MenuKod=>null;}
    private sealed class Roles(bool allow=true):IRolService
    {
        public Task<bool> HasUserPermissionAsync(int id,string kod,YetkiTipi tip,CancellationToken ct=default)=>Task.FromResult(allow);
        public Task<bool> IsAdminAsync(int id,CancellationToken ct=default)=>Task.FromResult(false);
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct=default)=>Task.FromResult(new List<MenuTanimi>());
        public Task<List<RolYetki>> GetRolYetkileriAsync(int id,CancellationToken ct=default)=>Task.FromResult(new List<RolYetki>());
        public Task YetkileriGuncelleAsync(int id,List<RolYetki> list,CancellationToken ct=default)=>Task.CompletedTask;
    }
    private sealed class Finans:IFinansUretimAktarimService
    {public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(IReadOnlyList<FinansUretimAktarimModel> m,CancellationToken ct)=>Task.FromResult(new FinansSenkronizasyonSonucModel(0,m.Count,0));}
    private sealed class Uow:IUnitOfWork
    {
        private readonly Dictionary<Type,object> repos=[];
        public bool HasActiveTransaction{get;private set;}
        public Uow Add<T>(params T[] data) where T:BaseEntity {foreach(var x in data) Items<T>().Add(x); return this;}
        public List<T> Items<T>() where T:BaseEntity=>((Repo<T>)GetRepository<T>()).Data;
        public IGenericRepository<T> GetRepository<T>() where T:BaseEntity {if(!repos.ContainsKey(typeof(T)))repos[typeof(T)]=new Repo<T>(); return (Repo<T>)repos[typeof(T)];}
        public Task<int> SaveChangesAsync(CancellationToken ct=default)
        {
            foreach(var f in Items<AmbalajUretimFormuSurumu>()) foreach(var k in f.Kayitlar)
                if(!Items<AmbalajUretimFormuKaydi>().Contains(k)){k.FormSurumuId=f.Id;Items<AmbalajUretimFormuKaydi>().Add(k);}
            return Task.FromResult(1);
        }
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken,Task<T>> f,CancellationToken ct=default){HasActiveTransaction=true;try{return await f(ct);}finally{HasActiveTransaction=false;}}
        public void RegisterAfterCommit(Func<CancellationToken,Task> f){} public void RegisterAfterRollback(Func<CancellationToken,Task> f){}public void Dispose(){}
    }
    private sealed class Repo<T>:IGenericRepository<T> where T:BaseEntity
    {
        public List<T> Data{get;}=[];
        public Task<T?> GetByIdAsync(int id)=>Task.FromResult(Data.FirstOrDefault(x=>x.Id==id));
        public Task<IEnumerable<T>> GetAllAsync()=>Task.FromResult<IEnumerable<T>>(Data);
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TP>(Expression<Func<T,TP>> x)=>GetAllAsync();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T,bool>> p)=>Task.FromResult(Data.Where(p.Compile()));
        public IQueryable<T> Queryable()=>Data.AsQueryable();
        public Task AddAsync(T x){if(x.Id==0)x.Id=Data.Select(d=>d.Id).DefaultIfEmpty().Max()+1;Data.Add(x);return Task.CompletedTask;}
        public void Update(T x){}public void Remove(T x)=>Data.Remove(x);
    }
}
