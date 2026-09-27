using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.FinansIslemleri;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Constants;
using _3K.Core.Models;
using _3K.Core.Entities;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace _3K.Application.Tests;

public sealed class FinansModuluTests
{
    [Theory]
    [InlineData(typeof(FinansPanelQuery), "finans-yonetimi")]
    [InlineData(typeof(FinansProjeSecenekleriQuery), "finans-yonetimi")]
    [InlineData(typeof(FinansAylikIslerQuery), YetkiKodlari.Finans.KayitGoruntule)]
    [InlineData(typeof(FinansIsKaydiOlusturCommand), YetkiKodlari.Finans.ManuelIsEkle)]
    [InlineData(typeof(FinansIsKaydiIptalCommand), YetkiKodlari.Finans.IsIptal)]
    [InlineData(typeof(FinansIsKaydiGeriAlCommand), YetkiKodlari.Finans.IsIptal)]
    [InlineData(typeof(FinansSiparisOlusturCommand), YetkiKodlari.Finans.PoGir)]
    [InlineData(typeof(FinansSiparisGuncelleCommand), YetkiKodlari.Finans.PoDegistir)]
    [InlineData(typeof(FinansFaturaOlusturCommand), YetkiKodlari.Finans.FaturaGir)]
    [InlineData(typeof(FinansFaturaGuncelleCommand), YetkiKodlari.Finans.FaturaDegistir)]
    [InlineData(typeof(FinansGiderlerQuery), YetkiKodlari.Finans.GiderGoruntule)]
    [InlineData(typeof(FinansGiderOlusturCommand), YetkiKodlari.Finans.GiderEkle)]
    [InlineData(typeof(FinansUrunGuncelleCommand), YetkiKodlari.Finans.IsKutuphanesiYonet)]
    [InlineData(typeof(FinansIsRaporuDosyaQuery), YetkiKodlari.Finans.RaporGoruntule)]
    public void Finans_requestleri_beklenen_sabit_yetki_kodunu_kullanir(Type requestType, string expectedCode)
    {
        var request = Activator.CreateInstance(requestType);
        var permission = Assert.IsAssignableFrom<IRequiresMenuPermission>(request);
        Assert.IsAssignableFrom<ISecuredRequest>(request);
        Assert.Equal(expectedCode, permission.RequiredMenuKod);
    }

    [Fact]
    public void Manuel_is_fiyat_tarih_ve_iptal_gelir_goruntulemeden_bagimsizdir()
    {
        var codes = new[] { YetkiKodlari.Finans.GelirGoruntule, YetkiKodlari.Finans.ManuelIsEkle,
            YetkiKodlari.Finans.IsIptal, YetkiKodlari.Finans.TarihDegistir,
            YetkiKodlari.Finans.BirimFiyatDegistir, YetkiKodlari.Finans.Modul };
        Assert.Equal(codes.Length, codes.Distinct().Count());
    }

    [Fact]
    public void Finans_proje_secenekleri_tum_projeleri_guvenli_alanlarla_sunar_ve_modul_okuma_yetkisine_baglidir()
    {
        Assert.Equal(
            new[] { nameof(FinansProjeSecenekModel.ProjeId), nameof(FinansProjeSecenekModel.ProjeNo), nameof(FinansProjeSecenekModel.Musteri) }.Order(),
            typeof(FinansProjeSecenekModel).GetProperties().Select(x => x.Name).Order());
        var query = new FinansProjeSecenekleriQuery();
        var requirement = Assert.Single(query.RequiredMenuPermissions);
        Assert.Equal(YetkiKodlari.Finans.Modul, requirement.MenuKod);
        Assert.Equal(YetkiTipi.R, requirement.YetkiTipi);

        var constructor = Assert.Single(typeof(FinansService).GetConstructors());
        Assert.DoesNotContain(constructor.GetParameters(), parameter =>
            parameter.ParameterType == typeof(_3K.Core.Interfaces.IRolService));
    }

    [Fact]
    public void Po_komutu_fiyat_override_edilirse_ek_fiyat_yetkisi_ister()
    {
        var plain = new FinansSiparisOlusturCommand
        {
            Model = new FinansSiparisOlusturModel(
                "PO-1", DateTime.Today, null,
                new[] { new FinansSiparisDagitimModel(1, 1, 0) })
        };
        var overridden = new FinansSiparisOlusturCommand
        {
            Model = new FinansSiparisOlusturModel(
                "PO-2", DateTime.Today, null,
                new[] { new FinansSiparisDagitimModel(1, 1, 0, BirimFiyat: 100) })
        };

        Assert.Equal(
            new[] { YetkiKodlari.Finans.PoGir },
            plain.RequiredMenuPermissions.Select(x => x.MenuKod));
        Assert.Equal(
            new[] { YetkiKodlari.Finans.PoGir, YetkiKodlari.Finans.BirimFiyatDegistir },
            overridden.RequiredMenuPermissions.Select(x => x.MenuKod));
    }

    [Fact]
    public void Aylik_rapor_dosyasi_hassas_yetkileri_ve_format_yetkisini_birlikte_ister()
    {
        var excel = new FinansAylikRaporDosyaQuery { Format = "xlsx", Yil = 2026, Ay = 8 };
        var pdf = new FinansAylikRaporDosyaQuery { Format = "pdf", Yil = 2026, Ay = 8 };

        foreach (var kod in new[] { YetkiKodlari.Finans.GelirGoruntule, YetkiKodlari.Finans.GiderGoruntule, YetkiKodlari.Finans.KarlilikGoruntule })
            Assert.Contains(excel.RequiredMenuPermissions, x => x.MenuKod == kod && x.YetkiTipi == YetkiTipi.R);
        Assert.Contains(excel.RequiredMenuPermissions, x => x.MenuKod == YetkiKodlari.Finans.ExcelAktar);
        Assert.Contains(excel.RequiredMenuPermissions, x => x.MenuKod == YetkiKodlari.Finans.RaporGoruntule && x.YetkiTipi == YetkiTipi.R);
        Assert.Contains(pdf.RequiredMenuPermissions, x => x.MenuKod == YetkiKodlari.Finans.PdfAktar);
        Assert.Contains(pdf.RequiredMenuPermissions, x => x.MenuKod == YetkiKodlari.Finans.RaporGoruntule && x.YetkiTipi == YetkiTipi.R);
    }

    [Fact]
    public void Fatura_belge_tutarlari_birlikte_ve_dengeli_girilmelidir()
    {
        var validator = new FinansFaturaOlusturModelValidator();
        var missingVat = new FinansFaturaOlusturModel(
            1, "F-1", new DateTime(2026, 8, 29), null,
            new[] { new FinansFaturaKalemiOlusturModel(1, 1, 0) },
            "EUR", 100, null, 120);
        var unbalanced = missingVat with { BelgeKdvTutari = 10 };
        var valid = missingVat with { BelgeKdvTutari = 20 };

        Assert.False(validator.Validate(missingVat).IsValid);
        Assert.False(validator.Validate(unbalanced).IsValid);
        Assert.True(validator.Validate(valid).IsValid);
    }

    [Fact]
    public void Fatura_guncellemede_mutabakati_koru_mevcut_snapshotlari_degistirmez()
    {
        var invoice = new FinansFatura
        {
            BelgeParaBirimiSnapshot = "EUR",
            BelgeNetTutarSnapshot = 100,
            BelgeKdvTutariSnapshot = 20,
            BelgeToplamTutarSnapshot = 120,
            MutabakatFarkiSnapshot = 3,
            MutabakatAciklamasi = "Mevcut mutabakat"
        };
        var model = new FinansFaturaGuncelleModel(
            "F-1",
            new DateTime(2026, 8, 29),
            null,
            BelgeMutabakatiniKoru: true);

        FinansService.ApplyInvoiceDocumentReconciliationForUpdate(invoice, model);

        Assert.Equal("EUR", invoice.BelgeParaBirimiSnapshot);
        Assert.Equal(100, invoice.BelgeNetTutarSnapshot);
        Assert.Equal(20, invoice.BelgeKdvTutariSnapshot);
        Assert.Equal(120, invoice.BelgeToplamTutarSnapshot);
        Assert.Equal(3, invoice.MutabakatFarkiSnapshot);
        Assert.Equal("Mevcut mutabakat", invoice.MutabakatAciklamasi);
        Assert.True(new FinansFaturaGuncelleModelValidator().Validate(model).IsValid);
    }

    [Fact]
    public void Fatura_guncellemede_varsayilan_davranis_mutabakati_temizlemeye_ve_yenilemeye_devam_eder()
    {
        var invoice = new FinansFatura
        {
            BelgeParaBirimiSnapshot = "EUR",
            BelgeNetTutarSnapshot = 100,
            BelgeKdvTutariSnapshot = 20,
            BelgeToplamTutarSnapshot = 120,
            MutabakatAciklamasi = "Eski"
        };
        var clearModel = new FinansFaturaGuncelleModel("F-1", new DateTime(2026, 8, 29), null);

        FinansService.ApplyInvoiceDocumentReconciliationForUpdate(invoice, clearModel);

        Assert.Null(invoice.BelgeParaBirimiSnapshot);
        Assert.Null(invoice.BelgeNetTutarSnapshot);
        Assert.Null(invoice.BelgeKdvTutariSnapshot);
        Assert.Null(invoice.BelgeToplamTutarSnapshot);
        Assert.Null(invoice.MutabakatAciklamasi);

        invoice.Kalemler.Add(new FinansFaturaKalemi
        {
            FinansSiparisKalemi = new FinansSiparisKalemi { ParaBirimiSnapshot = "USD" },
            NetTutarSnapshot = 50,
            KdvTutariSnapshot = 10,
            ToplamTutarSnapshot = 60
        });

        var updateModel = clearModel with
        {
            BelgeParaBirimi = "USD",
            BelgeNetTutar = 50,
            BelgeKdvTutari = 10,
            BelgeToplamTutar = 60,
            MutabakatAciklamasi = "Yeni"
        };
        FinansService.ApplyInvoiceDocumentReconciliationForUpdate(invoice, updateModel);

        Assert.Equal("USD", invoice.BelgeParaBirimiSnapshot);
        Assert.Equal(50, invoice.BelgeNetTutarSnapshot);
        Assert.Equal(10, invoice.BelgeKdvTutariSnapshot);
        Assert.Equal(60, invoice.BelgeToplamTutarSnapshot);
        Assert.Equal("Yeni", invoice.MutabakatAciklamasi);
    }

    [Fact]
    public void Faturalama_bekleyen_siparis_filtresi_yalniz_aktif_acik_ve_kismi_kayitlari_sayar()
    {
        var orders = new[]
        {
            new FinansSiparis { Id = 1, Durum = FinansSiparisDurumu.Acik },
            new FinansSiparis { Id = 2, Durum = FinansSiparisDurumu.KismiFaturalandi },
            new FinansSiparis { Id = 3, Durum = FinansSiparisDurumu.Faturalandi },
            new FinansSiparis { Id = 4, Durum = FinansSiparisDurumu.IptalEdildi, IptalEdildi = true },
            new FinansSiparis { Id = 5, Durum = FinansSiparisDurumu.Acik, IptalEdildi = true }
        }.AsQueryable();

        var visibleIds = FinansService.ApplyFaturalamaBekleyenFilter(orders, true)
            .Select(x => x.Id)
            .Order()
            .ToArray();

        Assert.Equal(new[] { 1, 2 }, visibleIds);
        Assert.Equal(5, FinansService.ApplyFaturalamaBekleyenFilter(orders, false).Count());
    }

    [Fact]
    public void Kutuphane_okuma_endpointleri_yonetici_yetkisini_baska_gorme_yetkisine_baglamaz()
    {
        Assert.Equal(YetkiKodlari.Finans.GiderKutuphanesiYonet, new FinansGiderKutuphaneKategorileriQuery().RequiredMenuKod);
        Assert.Equal(YetkiKodlari.Finans.GiderKutuphanesiYonet, new FinansGiderKutuphaneKalemleriQuery().RequiredMenuKod);
        Assert.Equal(YetkiKodlari.Finans.IsKutuphanesiYonet, new FinansUrunlerQuery().RequiredMenuKod);
    }

    [Fact]
    public void Siparis_validatoru_sifir_miktarli_kalemi_reddeder()
    {
        var validator = new FinansSiparisOlusturModelValidator();
        var model = new FinansSiparisOlusturModel(
            "PO-1",
            new DateTime(2026, 8, 29),
            null,
            new[] { new FinansSiparisDagitimModel(1, 0, 0) });

        var result = validator.Validate(model);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("sıfırdan büyük", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Manuel_is_modeli_bagimsiz_proje_ve_talep_eden_bilgilerini_tasir()
    {
        var model = new FinansIsKaydiKaydetModel(
            null,
            "MAN-001",
            "Bağımsız İş",
            "Müşteri",
            FinansIsTuru.OzelIs,
            "Saha hizmeti",
            null,
            "Ayşe Yılmaz",
            "Saha Operasyon",
            2,
            "Adet",
            0,
            null,
            150,
            "EUR",
            20,
            new DateTime(2026, 8, 10),
            new DateTime(2026, 8, 1));

        var result = new FinansIsKaydiKaydetModelValidator().Validate(model);

        Assert.True(result.IsValid);
        Assert.Equal("Ayşe Yılmaz", model.TalepEdenKisi);
        Assert.Equal("Saha Operasyon", model.TalepEdenBolum);
    }

    [Fact]
    public void Is_turu_sayisal_degerleri_frontend_sozlesmesiyle_sabittir()
    {
        Assert.Equal(1, (int)FinansIsTuru.AnaAmbalaj);
        Assert.Equal(2, (int)FinansIsTuru.IlaveSandik);
        Assert.Equal(3, (int)FinansIsTuru.IcSandik);
        Assert.Equal(4, (int)FinansIsTuru.SahaSandigi);
        Assert.Equal(5, (int)FinansIsTuru.YedekSandik);
        Assert.Equal(9, (int)FinansIsTuru.SarfKereste);
    }

    [Fact]
    public void Ef_finans_modeli_unique_kaynak_ve_belge_indekslerini_icerir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        var work = context.Model.FindEntityType(typeof(FinansIsKaydi));
        var order = context.Model.FindEntityType(typeof(FinansSiparis));
        var invoice = context.Model.FindEntityType(typeof(FinansFatura));

        Assert.NotNull(work);
        Assert.Contains(work!.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(x => x.Name).SequenceEqual(new[] { "KaynakTuru", "KaynakKayitId", "KaynakBileseni" }));
        Assert.Contains(order!.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == "PoNumarasi");
        Assert.Contains(invoice!.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == "FaturaNumarasi");
    }

    [Fact]
    public void Rol_proje_kapsami_ef_modelinde_yer_almaz()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        Assert.DoesNotContain(
            context.Model.GetEntityTypes(),
            entityType => entityType.GetTableName() == "RolProjeKapsamlari");
    }

    [Fact]
    public void Finans_sorgulari_rol_proje_kapsami_tablosuna_bagimli_degildir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);
        var workSql = context.Set<FinansIsKaydi>().Select(x => x.Id).ToQueryString();
        var auditSql = context.Set<FinansDegisiklikGecmisi>().Select(x => x.Id).ToQueryString();

        Assert.DoesNotContain("RolProjeKapsamlari", workSql, StringComparison.Ordinal);
        Assert.DoesNotContain("RolProjeKapsamlari", auditSql, StringComparison.Ordinal);
        Assert.Contains("FinansDegisiklikGecmisleri", auditSql);
        Assert.Contains("FinansIsKayitlari", workSql);
    }

    [Fact]
    public void Finans_para_toplami_sorgulari_postgresql_group_by_sorgusuna_cevrilebilir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        var incomeSql = FinansService.BuildInvoiceTotalsQuery(
                context.Set<FinansFaturaKalemi>().Where(x => !x.FinansFatura.IptalEdildi))
            .ToQueryString();
        var expenseSql = FinansService.BuildExpenseTotalsQuery(
                context.Set<FinansGider>().Where(x => !x.IptalEdildi))
            .ToQueryString();

        Assert.Contains("GROUP BY", incomeSql);
        Assert.Contains("GROUP BY", expenseSql);
    }

    [Fact]
    public void Finans_sayfali_sonuc_toplam_sayfa_ve_gezinme_bilgilerini_dogru_hesaplar()
    {
        var result = new FinansSayfaliSonuc<int>
        {
            Items = Enumerable.Range(1, 25).ToArray(),
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 61
        };

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void Sabit_tutarli_is_tek_seferde_tam_dagitilir_ve_ikinci_po_reddedilir()
    {
        var first = FinansMiktarKurallari.DagitimiNormalizeEt(
            FinansFiyatlandirmaBirimi.SabitTutar,
            1,
            0,
            10,
            5,
            0,
            0,
            false,
            "Sipariş");

        Assert.Equal(10, first.Adet);
        Assert.Equal(5, first.M3);
        Assert.Throws<IsKuraliException>(() => FinansMiktarKurallari.DagitimiNormalizeEt(
            FinansFiyatlandirmaBirimi.SabitTutar,
            1,
            0,
            10,
            5,
            first.Adet,
            first.M3,
            true,
            "Sipariş"));
    }

    [Fact]
    public void Adet_fiyatli_kismi_dagitimda_m3_adet_oranindan_turetilir()
    {
        var first = FinansMiktarKurallari.DagitimiNormalizeEt(
            FinansFiyatlandirmaBirimi.Adet,
            4,
            99,
            10,
            5,
            0,
            0,
            false,
            "Sipariş");
        var second = FinansMiktarKurallari.DagitimiNormalizeEt(
            FinansFiyatlandirmaBirimi.Adet,
            6,
            0,
            10,
            5,
            first.Adet,
            first.M3,
            true,
            "Sipariş");

        Assert.Equal(2, first.M3);
        Assert.Equal(3, second.M3);
        Assert.True(FinansMiktarKurallari.TamamiDagitildi(
            FinansFiyatlandirmaBirimi.Adet,
            10,
            5,
            first.Adet + second.Adet,
            first.M3 + second.M3,
            true));
    }

    [Fact]
    public void Metrekup_fiyatli_kismi_dagitimda_adet_m3_oranindan_turetilir()
    {
        var result = FinansMiktarKurallari.DagitimiNormalizeEt(
            FinansFiyatlandirmaBirimi.Metrekup,
            99,
            2,
            10,
            5,
            0,
            0,
            false,
            "Fatura");

        Assert.Equal(4, result.Adet);
        Assert.Equal(2, result.M3);
    }

    [Theory]
    [InlineData(FinansFiyatlandirmaBirimi.Adet, 0, 2)]
    [InlineData(FinansFiyatlandirmaBirimi.Metrekup, 2, 0)]
    public void Dagitim_fiyatlandirma_biriminin_authoritative_miktarini_zorunlu_tutar(
        FinansFiyatlandirmaBirimi birim,
        decimal adet,
        decimal m3)
    {
        Assert.Throws<IsKuraliException>(() => FinansMiktarKurallari.DagitimiNormalizeEt(
            birim,
            adet,
            m3,
            10,
            5,
            0,
            0,
            false,
            "Sipariş"));
    }

    [Theory]
    [InlineData(FinansFiyatlandirmaBirimi.Adet, 12, 12, 0)]
    [InlineData(FinansFiyatlandirmaBirimi.Metrekup, 12, 1, 12)]
    [InlineData(FinansFiyatlandirmaBirimi.SabitTutar, 12, 1, 0)]
    public void Duzenli_is_miktari_fiyatlandirma_birimine_gore_normalize_edilir(
        FinansFiyatlandirmaBirimi birim,
        decimal miktar,
        decimal beklenenAdet,
        decimal beklenenM3)
    {
        var result = FinansMiktarKurallari.DuzenliIsMiktari(birim, miktar);

        Assert.Equal(beklenenAdet, result.Adet);
        Assert.Equal(beklenenM3, result.M3);
    }

    [Fact]
    public void Duzenli_is_ve_gider_ayni_anda_sistem_ve_manuel_proje_kabul_etmez()
    {
        var recurring = new FinansDuzenliIsKaydetModel(
            1, "MAN-1", "Manuel", "Kira", FinansIsTuru.OzelIs, "Müşteri", null,
            new DateTime(2026, 1, 1), null, 1, 1, "Adet", null, 100, "EUR", 20, true);
        var expense = new FinansGiderKaydetModel(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), 1, null, null, null,
            "Gider", 1, "Adet", 100, "EUR", false, 20, 1, "MAN-1", null);

        Assert.False(new FinansDuzenliIsKaydetModelValidator().Validate(recurring).IsValid);
        Assert.False(new FinansGiderKaydetModelValidator().Validate(expense).IsValid);
    }

    [Fact]
    public void Gelir_raporu_karma_faturada_yalniz_filtreye_uyan_kalemi_toplar()
    {
        var order = new FinansSiparis { PoNumarasi = "PO-1", Durum = FinansSiparisDurumu.Acik };
        var invoice = new FinansFatura
        {
            FaturaNumarasi = "F-1",
            FaturaTarihi = new DateTime(2026, 8, 29),
            FinansSiparis = order
        };
        var matchingWork = new FinansIsKaydi
        {
            ProjeId = 1,
            ProjeNo = "PA-1",
            IsTuru = FinansIsTuru.AnaAmbalaj,
            Durum = FinansIsDurumu.Faturalandi
        };
        var otherWork = new FinansIsKaydi
        {
            ProjeId = 2,
            ProjeNo = "PA-2",
            IsTuru = FinansIsTuru.IlaveSandik,
            Durum = FinansIsDurumu.Faturalandi
        };
        var matchingOrderLine = new FinansSiparisKalemi
        {
            FinansIsKaydi = matchingWork,
            FinansSiparis = order,
            ParaBirimiSnapshot = "EUR"
        };
        var otherOrderLine = new FinansSiparisKalemi
        {
            FinansIsKaydi = otherWork,
            FinansSiparis = order,
            ParaBirimiSnapshot = "USD"
        };
        var lines = new[]
        {
            new FinansFaturaKalemi
            {
                FinansFatura = invoice,
                FinansSiparisKalemi = matchingOrderLine,
                NetTutarSnapshot = 100
            },
            new FinansFaturaKalemi
            {
                FinansFatura = invoice,
                FinansSiparisKalemi = otherOrderLine,
                NetTutarSnapshot = 900
            }
        };

        var filtered = FinansService.ApplyInvoiceLineFilter(
            lines.AsQueryable(),
            new FinansListeFiltre(
                ProjeId: 1,
                ProjeNo: "PA-1",
                IsTuru: FinansIsTuru.AnaAmbalaj,
                ParaBirimi: "EUR")).ToArray();

        Assert.Single(filtered);
        Assert.Equal(100, filtered.Sum(x => x.NetTutarSnapshot));
    }

    [Fact]
    public void Gelir_raporu_fatura_durumu_filtresini_kalem_seviyesinde_uygular()
    {
        var activeInvoice = new FinansFatura
        {
            FaturaNumarasi = "F-AKTIF",
            FaturaTarihi = new DateTime(2026, 8, 29),
            Durum = FinansFaturaDurumu.Aktif,
            FinansSiparis = new FinansSiparis { PoNumarasi = "PO-1" }
        };
        var work = new FinansIsKaydi { ProjeNo = "PA-1" };
        var orderLine = new FinansSiparisKalemi
        {
            FinansIsKaydi = work,
            FinansSiparis = activeInvoice.FinansSiparis,
            ParaBirimiSnapshot = "EUR"
        };
        var lines = new[]
        {
            new FinansFaturaKalemi
            {
                FinansFatura = activeInvoice,
                FinansSiparisKalemi = orderLine,
                NetTutarSnapshot = 100
            }
        };

        var filtered = FinansService.ApplyInvoiceLineFilter(
            lines.AsQueryable(),
            new FinansListeFiltre(FaturaDurumu: FinansFaturaDurumu.IptalEdildi)).ToArray();

        Assert.Empty(filtered);
    }

    [Fact]
    public void Iptal_fatura_durumu_is_ve_fatura_listelerinde_genel_dahil_et_flaginden_bagimsiz_calısir()
    {
        var activeInvoice = new FinansFatura { Id = 1, Durum = FinansFaturaDurumu.Aktif };
        var cancelledInvoice = new FinansFatura
        {
            Id = 2,
            Durum = FinansFaturaDurumu.IptalEdildi,
            IptalEdildi = true
        };
        var activeWork = new FinansIsKaydi { Id = 11 };
        var activeOrderLine = new FinansSiparisKalemi { FinansIsKaydi = activeWork };
        activeOrderLine.FaturaKalemleri.Add(new FinansFaturaKalemi
        {
            FinansSiparisKalemi = activeOrderLine,
            FinansFatura = activeInvoice
        });
        activeWork.SiparisKalemleri.Add(activeOrderLine);
        var cancelledWork = new FinansIsKaydi { Id = 12 };
        var cancelledOrderLine = new FinansSiparisKalemi { FinansIsKaydi = cancelledWork };
        cancelledOrderLine.FaturaKalemleri.Add(new FinansFaturaKalemi
        {
            FinansSiparisKalemi = cancelledOrderLine,
            FinansFatura = cancelledInvoice
        });
        cancelledWork.SiparisKalemleri.Add(cancelledOrderLine);

        var cancelledFilter = new FinansListeFiltre(FaturaDurumu: FinansFaturaDurumu.IptalEdildi);
        var activeFilter = new FinansListeFiltre(FaturaDurumu: FinansFaturaDurumu.Aktif, IptalEdilenleriDahilEt: true);

        Assert.Equal(
            new[] { 12 },
            FinansService.ApplyFilter(new[] { activeWork, cancelledWork }.AsQueryable(), cancelledFilter)
                .Select(x => x.Id).ToArray());
        Assert.Equal(
            new[] { 11 },
            FinansService.ApplyFilter(new[] { activeWork, cancelledWork }.AsQueryable(), activeFilter)
                .Select(x => x.Id).ToArray());
        Assert.Equal(
            new[] { 2 },
            FinansService.ApplyInvoiceStatusFilter(
                    new[] { activeInvoice, cancelledInvoice }.AsQueryable(), cancelledFilter)
                .Select(x => x.Id).ToArray());
        Assert.Equal(
            new[] { 1 },
            FinansService.ApplyInvoiceStatusFilter(
                    new[] { activeInvoice, cancelledInvoice }.AsQueryable(), activeFilter)
                .Select(x => x.Id).ToArray());
    }

    [Fact]
    public void Iptal_fatura_filtreleri_npgsql_tarafindan_sql_ifadesine_cevrilir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);
        var filter = new FinansListeFiltre(FaturaDurumu: FinansFaturaDurumu.IptalEdildi);

        var workSql = FinansService.ApplyFilter(context.Set<FinansIsKaydi>(), filter).ToQueryString();
        var invoiceSql = FinansService.ApplyInvoiceStatusFilter(context.Set<FinansFatura>(), filter).ToQueryString();

        Assert.Contains("IptalEdildi", workSql, StringComparison.Ordinal);
        Assert.Contains("KaynakAktif", workSql, StringComparison.Ordinal);
        Assert.Contains("IptalEdildi", invoiceSql, StringComparison.Ordinal);
    }

    [Fact]
    public void Aylik_tarih_araligi_po_talep_eden_ve_iptal_filtrelerini_korur()
    {
        var requested = new FinansListeFiltre(
            PageNumber: 2,
            PageSize: 40,
            PoNumarasi: "PO-2026-1",
            TalepEden: "Saha Operasyon",
            IptalEdilenleriDahilEt: true);
        var start = new DateTime(2026, 8, 1);

        var monthly = requested with
        {
            Baslangic = start,
            Bitis = start.AddMonths(1).AddDays(-1)
        };

        Assert.Equal(2, monthly.PageNumber);
        Assert.Equal(40, monthly.PageSize);
        Assert.Equal("PO-2026-1", monthly.PoNumarasi);
        Assert.Equal("Saha Operasyon", monthly.TalepEden);
        Assert.True(monthly.IptalEdilenleriDahilEt);
        Assert.Equal(new DateTime(2026, 8, 31), monthly.Bitis);
    }

    [Fact]
    public void Fatura_bekleyen_filtresi_iki_bekleyen_durumu_sql_sayfalamadan_once_kapsar()
    {
        var records = new[]
        {
            new FinansIsKaydi { Id = 1, Durum = FinansIsDurumu.SiparisBekliyor },
            new FinansIsKaydi { Id = 2, Durum = FinansIsDurumu.SiparisAcildi },
            new FinansIsKaydi { Id = 3, Durum = FinansIsDurumu.KismiFaturalandi },
            new FinansIsKaydi { Id = 4, Durum = FinansIsDurumu.Faturalandi }
        }.AsQueryable();

        var filtered = FinansService.ApplyFilter(
            records,
            new FinansListeFiltre(PageNumber: 1, PageSize: 1, FaturaBekleyen: true));
        var totalCount = filtered.Count();
        var firstPage = filtered.OrderBy(x => x.Id).Take(1).ToArray();

        Assert.Equal(2, totalCount);
        Assert.Single(firstPage);
        Assert.Equal(FinansIsDurumu.SiparisAcildi, firstPage[0].Durum);
        Assert.Equal(
            new[] { FinansIsDurumu.SiparisAcildi, FinansIsDurumu.KismiFaturalandi },
            filtered.OrderBy(x => x.Id).Select(x => x.Durum).ToArray());
    }

    [Fact]
    public void Fatura_bekleyen_filtresi_postgresql_sorgusuna_cevrilebilir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        var sql = FinansService.ApplyFilter(
                context.Set<FinansIsKaydi>(),
                new FinansListeFiltre(FaturaBekleyen: true))
            .ToQueryString();

        Assert.Contains("Durum", sql);
        Assert.Contains(" IN (", sql);
    }

    [Fact]
    public void Varsayilan_is_filtresi_pasif_kaynaklari_disarida_birakir_gecmis_filtresi_korur()
    {
        var aktif = new FinansIsKaydi { Id = 1, KaynakAktif = true };
        var pasif = new FinansIsKaydi
        {
            Id = 2,
            KaynakAktif = false,
            IptalEdildi = true
        };
        var tutarsizEskiKayit = new FinansIsKaydi
        {
            Id = 3,
            KaynakAktif = false,
            IptalEdildi = false
        };
        var kayitlar = new[] { aktif, pasif, tutarsizEskiKayit }.AsQueryable();

        var varsayilan = FinansService.ApplyFilter(kayitlar, new FinansListeFiltre())
            .Select(x => x.Id)
            .ToArray();
        var gecmisDahil = FinansService.ApplyFilter(
                kayitlar,
                new FinansListeFiltre(IptalEdilenleriDahilEt: true))
            .Select(x => x.Id)
            .Order()
            .ToArray();

        Assert.Equal([1], varsayilan);
        Assert.Equal([1, 2, 3], gecmisDahil);
    }

    [Fact]
    public void Aylik_ozet_pasif_ambalaj_kaynagini_dislar_ozel_is_iptal_gecmisini_korur()
    {
        var ay = new DateTime(2026, 9, 1);
        var aktifAmbalaj = new FinansIsKaydi
        {
            Id = 1,
            IsTuru = FinansIsTuru.AnaAmbalaj,
            UretimTarihi = ay.AddDays(1),
            FinansDonemi = ay,
            KaynakAktif = true
        };
        var pasifAmbalaj = new FinansIsKaydi
        {
            Id = 2,
            IsTuru = FinansIsTuru.AnaAmbalaj,
            UretimTarihi = ay.AddDays(2),
            FinansDonemi = ay,
            KaynakAktif = false,
            IptalEdildi = true
        };
        var iptalOzelIs = new FinansIsKaydi
        {
            Id = 3,
            IsTuru = FinansIsTuru.OzelIs,
            UretimTarihi = ay.AddDays(3),
            FinansDonemi = ay,
            KaynakAktif = true,
            IptalEdildi = true
        };

        var sonuc = FinansService.AylikKayitlariFiltrele(
                new[] { aktifAmbalaj, pasifAmbalaj, iptalOzelIs }.AsQueryable(),
                ay,
                ay.AddMonths(1))
            .Select(x => x.Id)
            .Order()
            .ToArray();

        Assert.Equal([1, 3], sonuc);
    }

    [Fact]
    public void Aylik_aktif_kaynak_filtresi_postgresql_sorgusuna_cevrilebilir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);
        var ay = new DateTime(2026, 9, 1);

        var sql = FinansService.AylikKayitlariFiltrele(
                context.Set<FinansIsKaydi>(),
                ay,
                ay.AddMonths(1))
            .ToQueryString();

        Assert.Contains("KaynakAktif", sql, StringComparison.Ordinal);
        Assert.Contains("IptalEdildi", sql, StringComparison.Ordinal);
        Assert.Contains("IsTuru", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Aylik_iptalleri_goster_normal_uretimde_pasif_kaynagi_geri_getirmez()
    {
        var month = new DateTime(2026, 9, 1);
        var records = new[]
        {
            new FinansIsKaydi { Id = 1, IsTuru = FinansIsTuru.AnaAmbalaj, UretimTarihi = month, FinansDonemi = month, KaynakAktif = true },
            new FinansIsKaydi { Id = 2, IsTuru = FinansIsTuru.AnaAmbalaj, UretimTarihi = month, FinansDonemi = month, KaynakAktif = false, IptalEdildi = true },
            new FinansIsKaydi { Id = 3, IsTuru = FinansIsTuru.OzelIs, UretimTarihi = month, FinansDonemi = month, KaynakAktif = true, IptalEdildi = true }
        }.AsQueryable();

        var scoped = FinansService.AylikKayitlariFiltrele(records, month, month.AddMonths(1));
        var result = FinansService.ApplyFilter(
                scoped,
                new FinansListeFiltre(IptalEdilenleriDahilEt: true))
            .Select(x => x.Id)
            .Order()
            .ToArray();

        Assert.Equal([1, 3], result);
    }

    [Fact]
    public void Aylik_sayfa_birimi_proje_alt_kayitlarini_bolmez_ozel_isi_ayri_sayar()
    {
        var records = new[]
        {
            new FinansIsKaydi { Id = 1, ProjeId = 10, ProjeNo = "PA-10", Musteri = "M", IsTuru = FinansIsTuru.AnaAmbalaj },
            new FinansIsKaydi { Id = 2, ProjeId = 10, ProjeNo = "PA-10", Musteri = "M", IsTuru = FinansIsTuru.IlaveSandik },
            new FinansIsKaydi { Id = 3, ProjeId = 20, ProjeNo = "PA-20", Musteri = "M", IsTuru = FinansIsTuru.AnaAmbalaj },
            new FinansIsKaydi { Id = 4, ProjeNo = "BAĞIMSIZ", Musteri = "M", IsTuru = FinansIsTuru.OzelIs, IsAdi = "Kira", RaporGrubu = "Kira" }
        }.AsQueryable();

        var units = FinansService.BuildMonthlyPageUnitsQuery(records).ToArray();

        Assert.Equal(3, units.Length);
        Assert.Single(units, x => x.ProjeId == 10 && !x.OzelIsId.HasValue);
        Assert.Single(units, x => x.OzelIsId == 4);
    }

    [Fact]
    public void Finans_buyuk_listeleri_postgresql_tarafinda_sayfalanir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);
        var month = new DateTime(2026, 9, 1);

        var monthlySql = FinansService.BuildMonthlyPageUnitsQuery(
                FinansService.AylikKayitlariFiltrele(
                    context.Set<FinansIsKaydi>(), month, month.AddMonths(1)))
            .OrderBy(x => x.Sira)
            .Skip(25)
            .Take(25)
            .ToQueryString();
        var recurringSql = FinansService.ApplyRecurringFilter(
                context.Set<FinansDuzenliIs>(), false, "kira")
            .OrderBy(x => x.Id)
            .Skip(25)
            .Take(25)
            .ToQueryString();
        var productSql = FinansService.ApplyProductFilter(
                context.Set<FinansUrun>(), false, "ahsap")
            .OrderBy(x => x.Id)
            .Skip(25)
            .Take(25)
            .ToQueryString();

        Assert.Contains("DISTINCT", monthlySql, StringComparison.Ordinal);
        Assert.All(new[] { monthlySql, recurringSql, productSql }, sql =>
        {
            Assert.Contains("OFFSET", sql, StringComparison.Ordinal);
            Assert.Contains("LIMIT", sql, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Aylik_sayfali_contract_global_ozetleri_sayfa_metasindan_ayri_tasir()
    {
        var result = new FinansAylikSayfaliSonuc
        {
            Items = Array.Empty<FinansAylikIsModel>(),
            FinansOzeti =
            [
                new FinansAylikFinansOzetiModel("EUR", 120m, 80m, 40m, 60m, 20m, 10m, 50m)
            ],
            GrupToplamlari =
            [
                new FinansAylikGrupToplamiModel("Ana Ambalaj", "EUR", 100m, 20m, 120m)
            ],
            PageNumber = 2,
            PageSize = 25,
            TotalCount = 51
        };

        Assert.Equal(3, result.TotalPages);
        Assert.Equal(120m, Assert.Single(result.FinansOzeti).Toplam);
        Assert.Equal(120m, Assert.Single(result.GrupToplamlari).ToplamTutar);
    }

    [Fact]
    public void Aylik_proje_birimi_ve_satir_gruplamasi_ayni_kimligi_kullanir()
    {
        var records = new[]
        {
            new FinansIsKaydi
            {
                Id = 1, ProjeId = 10, ProjeNo = "PA-ESKI", Musteri = "Eski Müşteri",
                IsTuru = FinansIsTuru.AnaAmbalaj, KaynakAktif = true, Adet = 1, ToplamM3 = 1
            },
            new FinansIsKaydi
            {
                Id = 2, ProjeId = 10, ProjeNo = "PA-YENI", Musteri = "Yeni Müşteri",
                IsTuru = FinansIsTuru.AnaAmbalaj, KaynakAktif = true, Adet = 1, ToplamM3 = 1
            },
            new FinansIsKaydi
            {
                Id = 3, ProjeNo = "MANUEL-1", Musteri = "Müşteri A",
                IsTuru = FinansIsTuru.AnaAmbalaj, KaynakAktif = true, Adet = 1, ToplamM3 = 1
            },
            new FinansIsKaydi
            {
                Id = 4, ProjeNo = "MANUEL-1", Musteri = "Müşteri B",
                IsTuru = FinansIsTuru.AnaAmbalaj, KaynakAktif = true, Adet = 1, ToplamM3 = 1
            }
        };

        var units = FinansService.BuildMonthlyPageUnitsQuery(records.AsQueryable()).ToArray();
        var rows = FinansService.AylikModelleriOlustur(
            records,
            new Dictionary<int, string>());

        Assert.Equal(3, units.Length);
        Assert.Single(units, x => x.ProjeId == 10);
        Assert.Equal(2, units.Count(x => !x.ProjeId.HasValue && x.ProjeNo == "MANUEL-1"));
        Assert.Equal(3, rows.Count);
        Assert.Equal(2, Assert.Single(rows, x => x.ProjeId == 10).IsKaydiIds.Count);
        Assert.Equal(2, rows.Count(x => !x.ProjeId.HasValue && x.ProjeNo == "MANUEL-1"));
        Assert.Single(rows.Where(x => x.ProjeId == 10).Select(x => x.ProjeBirimAnahtari).Distinct());
        Assert.Equal(2, rows.Where(x => !x.ProjeId.HasValue).Select(x => x.ProjeBirimAnahtari).Distinct().Count());
        Assert.Contains(rows, x => x.Musteri == "Müşteri A");
        Assert.Contains(rows, x => x.Musteri == "Müşteri B");
    }

    [Fact]
    public void Aylik_farkli_fiyat_snapshotlarini_ilk_kaydin_fiyatiyla_birlestirmez()
    {
        var records = new[]
        {
            new FinansIsKaydi
            {
                Id = 1, ProjeId = 10, ProjeNo = "PA-10", Musteri = "M",
                IsTuru = FinansIsTuru.AnaAmbalaj, FinansUrunId = 100,
                FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup,
                BirimFiyatSnapshot = 10m, ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20m,
                ToplamM3 = 2m, Adet = 1, KaynakAktif = true
            },
            new FinansIsKaydi
            {
                Id = 2, ProjeId = 10, ProjeNo = "PA-10", Musteri = "M",
                IsTuru = FinansIsTuru.AnaAmbalaj, FinansUrunId = 101,
                FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup,
                BirimFiyatSnapshot = 30m, ParaBirimiSnapshot = "USD", KdvOraniSnapshot = 10m,
                ToplamM3 = 3m, Adet = 1, KaynakAktif = true
            }
        };

        var rows = FinansService.AylikModelleriOlustur(records, new Dictionary<int, string>());

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, x => x.ParaBirimi == "EUR" && x.Miktar == 2m && x.NetTutar == 20m && x.ToplamTutar == 24m);
        Assert.Contains(rows, x => x.ParaBirimi == "USD" && x.Miktar == 3m && x.NetTutar == 90m && x.ToplamTutar == 99m);
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Aylik_arama_bir_alt_satira_eslesince_ayni_proje_biriminin_tum_satirlarini_toplama_dahil_eder()
    {
        await FinansV2PostgresTests.InDatabase(async options =>
        {
            await using var context = new AppDbContext(options);
            context.AddRange(new Proje { Id = 10, ProjeNo = "PA-10", Musteri = "A" },
                new Proje { Id = 20, ProjeNo = "PA-20", Musteri = "B" });
            await context.SaveChangesAsync();
            var records = new[]
            {
                new FinansIsKaydi { Id = 1, ProjeId = 10, ProjeNo = "PA-10", Musteri = "A", IsAdi = "Ana", IsTuru = FinansIsTuru.AnaAmbalaj, KaynakAktif = true, Adet = 1 },
                new FinansIsKaydi { Id = 2, ProjeId = 10, ProjeNo = "PA-ESKI", Musteri = "Eski", IsAdi = "needle detay", IsTuru = FinansIsTuru.IlaveSandik, KaynakAktif = true, Adet = 1 },
                new FinansIsKaydi { Id = 3, ProjeId = 20, ProjeNo = "PA-20", Musteri = "B", IsAdi = "Başka", IsTuru = FinansIsTuru.AnaAmbalaj, KaynakAktif = true, Adet = 1 },
                new FinansIsKaydi { Id = 4, ProjeNo = "MANUEL", Musteri = "C", IsAdi = "needle manuel", IsTuru = FinansIsTuru.IlaveSandik, KaynakAktif = true, Adet = 1 },
                new FinansIsKaydi { Id = 5, ProjeNo = "MANUEL", Musteri = "D", IsAdi = "Ayrı müşteri", IsTuru = FinansIsTuru.AnaAmbalaj, KaynakAktif = true, Adet = 1 }
            };
            context.AddRange(records);
            await context.SaveChangesAsync();

            var result = await FinansService.BuildMonthlyExpandedFilterQuery(
                    context.Set<FinansIsKaydi>().AsNoTracking(),
                    new FinansListeFiltre(Arama: "needle"))
                .Select(x => x.Id)
                .Order()
                .ToArrayAsync();

            Assert.Equal([1, 2, 4], result);
        });
    }

    [Fact]
    public void Aylik_genisletilmis_arama_postgresql_exists_sorgusuna_cevrilir()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        var sql = FinansService.BuildMonthlyExpandedFilterQuery(
                context.Set<FinansIsKaydi>().AsNoTracking(),
                new FinansListeFiltre(Arama: "radyatör"))
            .ToQueryString();

        Assert.Contains("EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ProjeId", sql, StringComparison.Ordinal);
        Assert.Contains("Musteri", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Aylik_rapor_kanonik_gruplari_gercek_is_gruplarina_esler()
    {
        var rows = FinansService.AylikModelleriOlustur(
        [
            new FinansIsKaydi { Id = 1, ProjeId = 10, ProjeNo = "PA-10", Musteri = "M", IsAdi = "Ana", IsTuru = FinansIsTuru.AnaAmbalaj, KaynakAktif = true },
            new FinansIsKaydi { Id = 2, ProjeNo = "KIRA", Musteri = "M", IsAdi = "Kira", IsTuru = FinansIsTuru.OzelIs, RaporGrubu = "Kira", KaynakAktif = true },
            new FinansIsKaydi { Id = 3, ProjeNo = "EK", Musteri = "M", IsAdi = "Danışmanlık", IsTuru = FinansIsTuru.OzelIs, RaporGrubu = "Danışmanlık", KaynakAktif = true }
        ], new Dictionary<int, string>());

        Assert.Single(rows, x => FinansRaporService.AylikRaporGrubunaDahil(x, new HashSet<string>(["Sabit İşler"])));
        Assert.Single(rows, x => FinansRaporService.AylikRaporGrubunaDahil(x, new HashSet<string>(["Ana Ambalaj"])));
        Assert.Single(rows, x => FinansRaporService.AylikRaporGrubunaDahil(x, new HashSet<string>(["Ekstra İşler"])));
    }

    [Fact]
    public void Siparis_secimi_250_kayit_sinirina_takilmaz_ve_guvenli_sinir_koyar()
    {
        var ids = Enumerable.Range(1, 300).Concat([1, 2]).ToArray();

        var normalized = FinansService.NormalizeWorkSelectionIds(ids);

        Assert.Equal(300, normalized.Count);
        Assert.Throws<IsKuraliException>(() =>
            FinansService.NormalizeWorkSelectionIds(Enumerable.Range(1, 2001).ToArray()));
        Assert.Throws<IsKuraliException>(() =>
            FinansService.NormalizeWorkSelectionIds([1, 0]));
    }

    [Theory]
    [InlineData(int.MaxValue, 25, 51, 100, 3, 25, 50)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, 100, 21474837, 100, 2147483600)]
    [InlineData(500, 25, 0, 100, 1, 25, 0)]
    [InlineData(-5, 0, 60, 100, 1, 1, 0)]
    public void Finans_sayfalama_yuksek_sayfayi_gecerli_araliga_ceker_ve_tasma_uretmez(
        int requestedPage,
        int requestedSize,
        int totalCount,
        int maxSize,
        int expectedPage,
        int expectedSize,
        int expectedSkip)
    {
        var actual = FinansService.NormalizePagination(
            requestedPage, requestedSize, totalCount, maxSize);

        Assert.Equal(expectedPage, actual.PageNumber);
        Assert.Equal(expectedSize, actual.PageSize);
        Assert.Equal(expectedSkip, actual.Skip);
    }

    [Fact]
    public void Urun_sayfasi_ve_tekil_urun_sorgulari_tarifeyi_sql_tarafinda_yukler()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        var pageSql = FinansService.WithProductPageDetails(
                context.Set<FinansUrun>(), new DateTime(2026, 9, 2))
            .Take(25)
            .ToQueryString();

        Assert.Contains("FinansFiyatTarifeleri", pageSql, StringComparison.Ordinal);
        Assert.Contains("GecerlilikBaslangici", pageSql, StringComparison.Ordinal);
        Assert.Contains("GecerlilikBitisi", pageSql, StringComparison.Ordinal);

        var byIdSql = FinansService.BuildProductByIdQuery(
                context.Set<FinansUrun>(), 42, new DateTime(2026, 9, 2))
            .ToQueryString();
        Assert.Contains("Id", byIdSql, StringComparison.Ordinal);
        Assert.Contains("42", byIdSql, StringComparison.Ordinal);
    }
}
