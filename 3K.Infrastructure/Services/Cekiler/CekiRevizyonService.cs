using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;

namespace _3K.Infrastructure.Services.Cekiler;

/// <summary>Çeki revizyonu akışı: ön izleme, onaya sunma ve ön izleme bütünlüğü doğrulanarak onaylanan talebin uygulanması.</summary>
internal sealed class CekiRevizyonService(
    IUnitOfWork unitOfWork,
    AppDbContext context,
    CekiRevizyonOnizleyici onizleyici,
    CekiRevizyonUygulayici uygulayici,
    CekiRevizyonSorgulari sorgular,
    ILogger<CekiRevizyonService> logger) : ICekiRevizyonService
{
    public async Task<CekiRevizyonOnizlemeSonuc> CekiRevizyonOnizleAsync(Stream excelDosya, string dosyaAdi)
    {
        var guvenliDosyaAdi = CekiRevizyonDosyasi.RevizyonDosyaAdiniDogrula(dosyaAdi);
        var dosya = await CekiRevizyonDosyasi.RevizyonDosyasiniOkuAsync(excelDosya, guvenliDosyaAdi);
        var baglam = await sorgular.RevizyonBaglaminiYukleAsync(dosya.Import);

        return await onizleyici.RevizyonOnizlemeOlusturAsync(
            dosya.Import,
            guvenliDosyaAdi,
            baglam.Proje,
            baglam.AnaCeki,
            baglam.AnaSatirlar);
    }

    public async Task<CekiRevizyonOnayTalebiSonuc> CekiRevizyonOnayaSunAsync(
        Stream excelDosya,
        string dosyaAdi,
        int kullaniciId,
        CancellationToken cancellationToken = default)
    {
        if (kullaniciId <= 0)
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon talebini oluşturan kullanıcı belirlenemedi.",
                CekiRevizyonSorunKodlari.GecersizDosya);
        }

        var guvenliDosyaAdi = CekiRevizyonDosyasi.RevizyonDosyaAdiniDogrula(dosyaAdi);
        var dosya = await CekiRevizyonDosyasi.RevizyonDosyasiniOkuAsync(
            excelDosya,
            guvenliDosyaAdi,
            cancellationToken);

        return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var baglam = await sorgular.RevizyonBaglaminiYukleAsync(dosya.Import);
            var onizleme = await onizleyici.RevizyonOnizlemeOlusturAsync(
                dosya.Import,
                guvenliDosyaAdi,
                baglam.Proje,
                baglam.AnaCeki,
                baglam.AnaSatirlar);

            RevizyonOnizlemesininUygulanabilirOldugunuDogrula(onizleme);
            var onizlemeJson = CekiRevizyonDosyasi.RevizyonOnizlemeJsoniniOlustur(onizleme);

            var talep = new CekiRevizyonTalebi
            {
                ProjeId = baglam.Proje.Id,
                AnaCekiId = baglam.AnaCeki.Id,
                TalepEdenKullaniciId = kullaniciId,
                DosyaAdi = guvenliDosyaAdi,
                DosyaIcerigi = dosya.DosyaBytes.ToArray(),
                DosyaSha256 = CekiRevizyonDosyasi.Sha256Olustur(dosya.DosyaBytes),
                OnizlemeJson = onizlemeJson,
                OnizlemeHash = CekiRevizyonOnizlemeButunlugu.HashOlustur(onizleme),
                OnizlemeSurumu = CekiRevizyonOnizlemeButunlugu.Surum,
                EklenenSatirSayisi = onizleme.EklenenSatirSayisi,
                GuncellenenSatirSayisi = onizleme.GuncellenenSatirSayisi,
                SilinenSatirSayisi = onizleme.SilinecekSatirSayisi,
                CreatedBy = kullaniciId.ToString(CultureInfo.InvariantCulture)
            };

            context.CekiRevizyonTalepleri.Add(talep);
            await context.SaveChangesAsync(transactionCancellationToken);

            return new CekiRevizyonOnayTalebiSonuc
            {
                TalepId = talep.Id,
                ProjeId = baglam.Proje.Id,
                ProjeNo = baglam.Proje.ProjeNo,
                AnaCekiId = baglam.AnaCeki.Id,
                DosyaAdi = talep.DosyaAdi,
                EklenenSatirSayisi = talep.EklenenSatirSayisi,
                GuncellenenSatirSayisi = talep.GuncellenenSatirSayisi,
                SilinenSatirSayisi = talep.SilinenSatirSayisi,
                Mesaj = $"{baglam.Proje.ProjeNo} revizyonu onaya sunulmaya hazırlandı."
            };
        }, cancellationToken);
    }

    public async Task<CekiRevizyonSonuc> OnayliCekiRevizyonunuUygulaAsync(
        int talepId,
        int uygulayanKullaniciId,
        CancellationToken cancellationToken = default)
    {
        if (talepId <= 0)
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Geçerli bir revizyon talebi belirtilmelidir.",
                CekiRevizyonSorunKodlari.GecersizDosya);
        }

        if (uygulayanKullaniciId <= 0)
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyonu uygulayan kullanıcı belirlenemedi.",
                CekiRevizyonSorunKodlari.GecersizDosya);
        }

        string? olusturulanDosyaYolu = null;

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
            {
                var talep = await RevizyonTalebiniKilitleAsync(
                    talepId,
                    transactionCancellationToken);

                if (talep == null)
                {
                    CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                        "Onaylanan revizyon talebi bulunamadı.",
                        CekiRevizyonSorunKodlari.GecersizDosya);
                }

                var projeNo = await context.Projeler
                    .Where(p => p.Id == talep.ProjeId)
                    .Select(p => p.ProjeNo)
                    .FirstOrDefaultAsync(transactionCancellationToken)
                    ?? string.Empty;

                if (talep.UygulananRevizyonCekiId.HasValue)
                    return UygulanmisRevizyonSonucunuOlustur(talep, projeNo);

                var talepDosyaIcerigi = CekiRevizyonDosyasi.RevizyonTalepDosyasiniDogrula(talep);

                using var excelStream = new MemoryStream(talepDosyaIcerigi, writable: false);
                var dosya = await CekiRevizyonDosyasi.RevizyonDosyasiniOkuAsync(
                    excelStream,
                    talep.DosyaAdi,
                    transactionCancellationToken);
                var baglam = await sorgular.RevizyonBaglaminiYukleAsync(dosya.Import);

                if (baglam.Proje.Id != talep.ProjeId || baglam.AnaCeki.Id != talep.AnaCekiId)
                {
                    CekiRevizyonHatalari.RevizyonOnayCakismasiFirlat(
                        "Revizyon dosyasının proje veya ana çeki bilgisi onay talebiyle artık eşleşmiyor.",
                        CekiRevizyonSorunKodlari.OnayOnizlemesiDegisti);
                }

                var guncelOnizleme = await onizleyici.RevizyonOnizlemeOlusturAsync(
                    dosya.Import,
                    talep.DosyaAdi,
                    baglam.Proje,
                    baglam.AnaCeki,
                    baglam.AnaSatirlar);
                if (talep.OnizlemeSurumu != CekiRevizyonOnizlemeButunlugu.Surum ||
                    !CekiRevizyonOnizlemeButunlugu.HashDogrula(
                        guncelOnizleme,
                        talep.OnizlemeHash))
                {
                    CekiRevizyonHatalari.RevizyonOnayCakismasiFirlat(
                        "Revizyon ön izlemesinden sonra proje verileri değişti. Onaylanan değişikliklerle güncel değişiklikler aynı olmadığı için işlem uygulanmadı; dosyayı yeniden ön izleyip onaya sunun.",
                        CekiRevizyonSorunKodlari.OnayOnizlemesiDegisti,
                        RevizyonEngelleriniGetir(guncelOnizleme));
                }

                RevizyonOnizlemesininUygulanabilirOldugunuDogrula(guncelOnizleme);

                using var uygulamaStream = new MemoryStream(talepDosyaIcerigi, writable: false);
                var sonuc = await uygulayici.CekiRevizyonunuUygulaAsync(
                    uygulamaStream,
                    talep.DosyaAdi,
                    uygulayanKullaniciId,
                    transactionCancellationToken,
                    dosyaYolu => olusturulanDosyaYolu = dosyaYolu);

                if (!string.IsNullOrWhiteSpace(olusturulanDosyaYolu))
                {
                    var rollbackDosyaYolu = olusturulanDosyaYolu;
                    unitOfWork.RegisterAfterRollback(_ =>
                    {
                        CekiRevizyonDosyasi.RevizyonDosyasiniGuvenleSil(logger, rollbackDosyaYolu);
                        return Task.CompletedTask;
                    });
                }

                talep.UygulananRevizyonCekiId = sonuc.RevizyonCekiId;
                talep.UygulamaTarihi = TurkeyTime.Now;
                talep.EklenenSatirSayisi = sonuc.EklenenSatirSayisi;
                talep.GuncellenenSatirSayisi = sonuc.GuncellenenSatirSayisi;
                talep.SilinenSatirSayisi = sonuc.SilinenSatirSayisi;
                talep.UpdatedDate = TurkeyTime.Now;
                talep.UpdatedBy = uygulayanKullaniciId.ToString(CultureInfo.InvariantCulture);
                await context.SaveChangesAsync(transactionCancellationToken);

                return sonuc;
            }, cancellationToken);
        }
        catch
        {
            CekiRevizyonDosyasi.RevizyonDosyasiniGuvenleSil(logger, olusturulanDosyaYolu);
            throw;
        }
    }

    private async Task<CekiRevizyonTalebi?> RevizyonTalebiniKilitleAsync(
        int talepId,
        CancellationToken cancellationToken)
    {
        return await context.CekiRevizyonTalepleri
            .FromSqlInterpolated($$"""
                SELECT *
                FROM "CekiRevizyonTalepleri"
                WHERE "Id" = {{talepId}}
                FOR UPDATE
                """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static CekiRevizyonSonuc UygulanmisRevizyonSonucunuOlustur(
        CekiRevizyonTalebi talep,
        string projeNo)
    {
        return new CekiRevizyonSonuc
        {
            ProjeId = talep.ProjeId,
            ProjeNo = projeNo,
            AnaCekiId = talep.AnaCekiId,
            RevizyonCekiId = talep.UygulananRevizyonCekiId!.Value,
            EklenenSatirSayisi = talep.EklenenSatirSayisi,
            GuncellenenSatirSayisi = talep.GuncellenenSatirSayisi,
            SilinenSatirSayisi = talep.SilinenSatirSayisi,
            Mesaj = $"{projeNo} revizyonu daha önce uygulanmış; mevcut sonuç döndürüldü."
        };
    }

    private static void RevizyonOnizlemesininUygulanabilirOldugunuDogrula(
        CekiRevizyonOnizlemeSonuc onizleme)
    {
        if (onizleme.UygulanabilirMi)
            return;

        var sorunlar = RevizyonEngelleriniGetir(onizleme);
        if (sorunlar.Count == 0)
        {
            sorunlar.Add(CekiRevizyonHatalari.GenelRevizyonSorunuOlustur(
                CekiRevizyonSorunKodlari.GecersizDosya,
                "Revizyon ön izlemesinde giderilmesi gereken engeller bulunuyor."));
        }

        throw new CekiRevizyonValidationException(
            string.IsNullOrWhiteSpace(onizleme.Mesaj)
                ? "Revizyon ön izlemesindeki engeller giderilmeden işlem onaya sunulamaz."
                : onizleme.Mesaj,
            sorunlar);
    }

    private static List<CekiRevizyonSorunu> RevizyonEngelleriniGetir(
        CekiRevizyonOnizlemeSonuc onizleme)
    {
        var sorunlar = new List<CekiRevizyonSorunu>();

        foreach (var satir in onizleme.Satirlar.Where(s => !s.UygulanabilirMi))
        {
            if (satir.Sorunlar.Count > 0)
            {
                sorunlar.AddRange(satir.Sorunlar);
                continue;
            }

            var engeller = satir.Engeller.Count > 0
                ? satir.Engeller
                : new List<string> { satir.Mesaj };

            foreach (var engel in engeller.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                sorunlar.Add(new CekiRevizyonSorunu
                {
                    Kod = CekiRevizyonSorunKodlari.GecersizDosya,
                    Mesaj = engel,
                    Kategori = CekiRevizyonSorunKategorileri.Dogrulama,
                    ExcelSatirNo = satir.ExcelSatirNo,
                    CheckKodu = satir.CheckKodu,
                    SiraNo = satir.YeniSiraNo,
                    BarkodNo = satir.BarkodNo,
                    PozNo = satir.PozNo,
                    Tanim = satir.Tanim,
                    SandikNo = satir.YeniKoliNo
                });
            }
        }

        return sorunlar
            .DistinctBy(s => new
            {
                s.Kod,
                s.Mesaj,
                s.ExcelSatirNo,
                s.CheckKodu,
                s.SiraNo,
                s.BarkodNo,
                s.SandikNo
            })
            .ToList();
    }
}
