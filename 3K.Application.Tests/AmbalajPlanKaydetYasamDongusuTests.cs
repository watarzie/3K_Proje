using System.Text.Json;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class AmbalajPlanKaydetYasamDongusuTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task PlanKaydet_KarmaDurumVeTarihleriKorur_LegacyDurumIdIslemUretmez(int legacyDurum)
    {
        var f = new Fixture();
        var bekleyen = f.Add(1);
        var uretimde = f.Add(2, AmbalajUretimDurumu.Uretimde);
        var tamamlanan = f.Add(3, AmbalajUretimDurumu.Tamamlandi);
        f.FormEkle(uretimde, tamamlanan);
        var zamanlar = new[] { bekleyen, uretimde, tamamlanan }
            .Select(x => (x.UretimDurumu, x.UretimTarihi, x.TamamlanmaTarihi)).ToArray();
        f.Uow.Repo<AmbalajUretimGerceklesmesi>().Rows.Add(new() { Id = 1, AmbalajUretimKaydiId = 3, Adet = 1 });

        var result = await f.Save(new() { ProjeId = 10, SeciliKaynakSandikIds = [101, 102, 103], DurumId = legacyDurum });

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(zamanlar, new[] { bekleyen, uretimde, tamamlanan }
            .Select(x => (x.UretimDurumu, x.UretimTarihi, x.TamamlanmaTarihi)).ToArray());
        Assert.Single(f.Uow.Repo<AmbalajUretimGerceklesmesi>().Rows);
        Assert.DoesNotContain(f.Roles.Checked, x => x == AmbalajMenuKodlari.DurumDuzenle ||
            x == AmbalajMenuKodlari.DurumuGeriAl || x == AmbalajMenuKodlari.UretimiTamamla);
    }

    [Fact]
    public async Task PlanKaydet_IlkFormOncesi_SecimPartiKaydeder_UretimVeAktifFinansBaslatmaz()
    {
        var f = new Fixture(); var row = f.Add(1); row.UretimeAlindi = false;
        var result = await f.Save(new() { ProjeId = 10, SeciliKaynakSandikIds = [101], FirinPartiNo = " FP-2 ", DurumId = 3 });
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.True(row.UretimeAlindi); Assert.Equal("FP-2", row.FirinPartiNo);
        Assert.Equal(AmbalajUretimDurumu.Planlandi, row.UretimDurumu);
        Assert.Null(row.UretimTarihi); Assert.Null(row.TamamlanmaTarihi);
        Assert.Empty(f.Uow.Repo<AmbalajUretimGerceklesmesi>().Rows);
        Assert.Empty(f.Uow.Repo<AmbalajUretimFormuSurumu>().Rows);
        Assert.All(f.Finans.Models, x => Assert.False(x.KaynakAktif));

        var form = new AmbalajFormOlusturCommandHandler(f.Uow, f.User, f.Roles, f.Finans);
        var command = new AmbalajFormOlusturCommand { KayitIdleri = [row.Id], IdempotencyAnahtari = Guid.NewGuid() };
        var ilk = await form.Handle(command, default);
        Assert.True(ilk.IsSuccess, ilk.Error?.Message);
        Assert.Equal(AmbalajUretimDurumu.Uretimde, row.UretimDurumu);
        Assert.NotNull(row.UretimTarihi);
        Assert.True(f.Finans.Models.Last().KaynakAktif);
        var tarih = row.UretimTarihi;
        var tekrar = await form.Handle(command, default);
        Assert.True(tekrar.IsSuccess); Assert.Equal(ilk.Value!.Id, tekrar.Value!.Id);
        Assert.Equal(tarih, row.UretimTarihi);
        Assert.Single(f.Uow.Repo<AmbalajUretimFormuSurumu>().Rows);
    }

    [Theory]
    [InlineData(false, "Parti düzeltme", 403)]
    [InlineData(true, null, 400)]
    [InlineData(true, "Parti düzeltme", 200)]
    public async Task PlanKaydet_FormSonrasiParti_GercekDegisimKritikIzinVeGerekceIster(
        bool kritikIzin, string? gerekce, int expected)
    {
        var f = new Fixture(kritikIzin ? [AmbalajMenuKodlari.KritikVeriDuzenle] : []);
        var row = f.Add(1, AmbalajUretimDurumu.Uretimde); row.FirinPartiNo = "FP-1";
        var form = f.FormEkle(row); var eskiJson = form.SnapshotJson;
        var result = await f.Save(new() { ProjeId = 10, SeciliKaynakSandikIds = [101], FirinPartiNo = "FP-2", Gerekce = gerekce, DurumId = 2 });
        Assert.Equal(expected, result.StatusCode);
        Assert.Equal(expected == 200 ? "FP-2" : "FP-1", row.FirinPartiNo);
        Assert.Equal(eskiJson, form.SnapshotJson);
        Assert.Equal(AmbalajUretimDurumu.Uretimde, row.UretimDurumu);
        if (expected != 200)
        {
            Assert.Equal(0, f.Sync.Calls); Assert.Equal(0, f.Uow.SaveCount);
            Assert.Empty(f.Finans.Models); Assert.Empty(f.Uow.Repo<AmbalajUretimHareketi>().Rows);
        }
        else
            Assert.Contains(f.Uow.Repo<AmbalajUretimHareketi>().Rows, x => x.AlanAdi == "YetkiliRevizyon" && x.Aciklama == gerekce);
    }

    [Fact]
    public async Task PlanKaydet_AyniPartiVeSecim_KritikIzinAuditFinansGerektirmez()
    {
        var f = new Fixture(); var row = f.Add(1, AmbalajUretimDurumu.Tamamlandi);
        row.FirinPartiNo = "FP-1"; f.FormEkle(row);
        var result = await f.Save(new() { ProjeId = 10, SeciliKaynakSandikIds = [101], FirinPartiNo = " FP-1 " });
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.DoesNotContain(AmbalajMenuKodlari.KritikVeriDuzenle, f.Roles.Checked);
        Assert.Empty(f.Uow.Repo<AmbalajUretimHareketi>().Rows); Assert.Empty(f.Finans.Models);
        Assert.Equal(0, f.Uow.Repo<AmbalajUretimKaydi>().UpdateCount);
    }

    [Theory]
    [InlineData(false, "Seçim düzeltme", 403)]
    [InlineData(true, null, 400)]
    [InlineData(true, "Seçim düzeltme", 200)]
    public async Task PlanKaydet_FormSonrasiSecim_EskiKritikKapiVeTarihleriKorur(bool allowed, string? reason, int expected)
    {
        var f = new Fixture(allowed ? [AmbalajMenuKodlari.KritikVeriDuzenle, AmbalajMenuKodlari.FormSonrasiSecimDegistir] : []);
        var row = f.Add(1, AmbalajUretimDurumu.Tamamlandi); f.FormEkle(row);
        var tarih = row.UretimTarihi; var bitis = row.TamamlanmaTarihi;
        var result = await f.Save(new() { ProjeId = 10, SeciliKaynakSandikIds = [], Gerekce = reason });
        Assert.Equal(expected, result.StatusCode);
        Assert.Equal(expected != 200, row.UretimeAlindi);
        Assert.Equal(AmbalajUretimDurumu.Tamamlandi, row.UretimDurumu);
        Assert.Equal(tarih, row.UretimTarihi); Assert.Equal(bitis, row.TamamlanmaTarihi);
        if (expected != 200) { Assert.Equal(0, f.Uow.SaveCount); Assert.Empty(f.Finans.Models); }
    }

    [Fact]
    public async Task PlanKaydet_GrupBir_BaskaGrupVeBagimsizKaydiDegistirmez()
    {
        var f = new Fixture(); var hedef = f.Add(1); hedef.UretimeAlindi = false;
        var ilave = f.Add(2); ilave.Tur = AmbalajSandikTuru.Ilave; ilave.FirinPartiNo = "ILAVE";
        var bagimsiz = f.Add(3); bagimsiz.BagimsizKayitMi = true; bagimsiz.FirinPartiNo = "BAGIMSIZ";
        var result = await f.Save(new() { ProjeId = 10, Grup = 1, SeciliKaynakSandikIds = [101], FirinPartiNo = "YENI" });
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("YENI", hedef.FirinPartiNo); Assert.Equal("ILAVE", ilave.FirinPartiNo);
        Assert.Equal("BAGIMSIZ", bagimsiz.FirinPartiNo);
        Assert.Equal(1, f.Uow.Repo<AmbalajUretimKaydi>().UpdateCount);
    }

    [Fact]
    public async Task DurumKomutu_IlkFormYokken_Hala409VeHicbirDegisiklikYok()
    {
        var f = new Fixture(AmbalajMenuKodlari.DurumDuzenle); var row = f.Add(1);
        var called = false;
        var result = await new AmbalajYasamDongusuBehavior<AmbalajUretimDurumuGuncelleCommand, Result<AmbalajUretimKaydiDto>>(
            f.Uow, f.Roles, f.User).Handle(new() { Id = row.Id, Durum = AmbalajUretimDurumu.Uretimde },
            () => { called = true; return Task.FromResult(Result<AmbalajUretimKaydiDto>.Success(new())); }, default);
        Assert.Equal(409, result.StatusCode); Assert.False(called);
        Assert.Equal(AmbalajUretimDurumu.Planlandi, row.UretimDurumu); Assert.Null(row.UretimTarihi);
    }

    private sealed class Fixture(params string[] allowed)
    {
        public OrtakMemoryUow Uow { get; } = new();
        public OrtakUser User { get; } = new();
        public Roles Roles { get; } = new(allowed);
        public Sync Sync { get; } = new();
        public Finans Finans { get; } = new();
        public AmbalajUretimKaydi Add(int id, AmbalajUretimDurumu durum = AmbalajUretimDurumu.Planlandi)
        {
            if (Uow.Repo<Proje>().Rows.Count == 0)
                Uow.Repo<Proje>().Rows.Add(new() { Id = 10, ProjeNo = "PLAN", ProjeTipiId = (int)ProjeTipi.Normal });
            Uow.Repo<Sandik>().Rows.Add(new() { Id = 100 + id, ProjeId = 10, SandikNo = id.ToString(),
                Ad = "Radyatör", TipId = 1, Boy = 2500, En = 1500, Yukseklik = 1800, CreatedDate = new(2026, 8, 1) });
            var row = new AmbalajUretimKaydi { Id = id, ProjeId = 10, IsAkisKimligi = Guid.NewGuid(), KaynakKayitId = 100 + id,
                KaynakModul = AmbalajKaynakModulu.Sandik, SandikNo = id.ToString(), Ad = "Radyatör",
                SandikCinsi = AmbalajSandikCinsi.AhsapKapali, AmbalajaDahil = true, UretimeAlindi = true, Adet = 1,
                Boy = 2500, En = 1500, Yukseklik = 1800, Tur = AmbalajSandikTuru.Normal, UretimDurumu = durum,
                UretimTarihi = durum == AmbalajUretimDurumu.Planlandi ? null : new(2026, 9, 1),
                TamamlanmaTarihi = durum == AmbalajUretimDurumu.Tamamlandi ? new(2026, 9, 3) : null,
                CreatedDate = new(2026, 8, 2) };
            AmbalajUretimYardimcilari.M3DegerleriniHesapla(row);
            Uow.Repo<AmbalajUretimKaydi>().Rows.Add(row); return row;
        }
        public AmbalajUretimFormuSurumu FormEkle(params AmbalajUretimKaydi[] rows)
        {
            var form = new AmbalajUretimFormuSurumu { Id = 1, ProjeId = 10, Surum = 1,
                SnapshotJson = JsonSerializer.Serialize(new AmbalajUretimFormuModel { ProjeNo = "SABIT" }) };
            Uow.Repo<AmbalajUretimFormuSurumu>().Rows.Add(form);
            foreach (var row in rows) Uow.Repo<AmbalajUretimFormuKaydi>().Rows.Add(new() { FormSurumuId = 1, AmbalajUretimKaydiId = row.Id });
            return form;
        }
        public Task<Result<AmbalajPlanlamaPlanDto>> Save(AmbalajPlanKaydetCommand command)
        {
            var handler = new AmbalajPlanKaydetCommandHandler(Uow, User, Sync, Finans);
            return new AmbalajYasamDongusuBehavior<AmbalajPlanKaydetCommand, Result<AmbalajPlanlamaPlanDto>>(
                Uow, Roles, User).Handle(command, () => handler.Handle(command, default), default);
        }
    }
    private sealed class Roles(params string[] allowed) : IRolService
    {
        public List<string> Checked { get; } = [];
        public Task<bool> HasUserPermissionAsync(int userId, string code, YetkiTipi type, CancellationToken ct = default)
        { Checked.Add(code); return Task.FromResult(type == YetkiTipi.R || allowed.Contains(code)); }
        public Task<bool> IsAdminAsync(int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yetkiler, CancellationToken ct = default) => throw new NotSupportedException();
    }
    private sealed class Sync : IAmbalajKaynakSenkronizasyonService
    {
        public int Calls { get; private set; }
        public Task<Result<AmbalajSenkronizasyonSonucuDto>> SenkronizeEtAsync(int projeId, ICurrentUserService user,
            CancellationToken ct, bool sonucKayitlariniOlustur = true)
        { Calls++; return Task.FromResult(Result<AmbalajSenkronizasyonSonucuDto>.Success(new(0, 0, 0, 0, []))); }
    }
    private sealed class Finans : IFinansUretimAktarimService
    {
        public List<FinansUretimAktarimModel> Models { get; } = [];
        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(IReadOnlyList<FinansUretimAktarimModel> rows, CancellationToken ct)
        { Models.AddRange(rows); return Task.FromResult(new FinansSenkronizasyonSonucModel(0, rows.Count, 0)); }
    }
}
