using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Cekiler;

/// <summary>Çeki revizyonu doğrulama ve onay çakışması hataları; kullanıcıya satır ve kod bazlı sorun listesiyle döner.</summary>
internal static class CekiRevizyonHatalari
{
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    internal static void RevizyonValidationHatasiFirlat(string mesaj, string kod)
    {
        throw new CekiRevizyonValidationException(
            mesaj,
            new[] { GenelRevizyonSorunuOlustur(kod, mesaj) });
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    internal static void RevizyonValidationHatasiFirlat(
        string mesaj,
        CekiRevizyonSorunu sorun)
    {
        throw new CekiRevizyonValidationException(mesaj, new[] { sorun });
    }

    internal static CekiRevizyonSorunu GenelRevizyonSorunuOlustur(
        string kod,
        string mesaj,
        string kategori = CekiRevizyonSorunKategorileri.Dogrulama)
    {
        return new CekiRevizyonSorunu
        {
            Kod = kod,
            Mesaj = mesaj,
            Kategori = kategori
        };
    }

    internal static CekiRevizyonSorunu RevizyonSorunuOlustur(
        CiktiSatirImportBilgisi satir,
        string kod,
        string mesaj,
        string kategori = CekiRevizyonSorunKategorileri.Dogrulama)
    {
        return new CekiRevizyonSorunu
        {
            Kod = kod,
            Mesaj = mesaj,
            Kategori = kategori,
            ExcelSatirNo = satir.ExcelSatirNo,
            CheckKodu = NullIfEmpty(satir.CheckKodu),
            SiraNo = satir.SiraNo,
            BarkodNo = NullIfEmpty(satir.BarkodNo),
            PozNo = NullIfEmpty(satir.OlcuResmiPozNo),
            Tanim = NullIfEmpty(satir.Aciklama),
            SandikNo = NullIfEmpty(satir.KoliNo)
        };
    }

    internal static CekiRevizyonSorunu RevizyonSorunuOlustur(
        CekiSatiri satir,
        string kod,
        string mesaj)
    {
        return new CekiRevizyonSorunu
        {
            Kod = kod,
            Mesaj = mesaj,
            Kategori = CekiRevizyonSorunKategorileri.DurumCakismasi,
            SiraNo = satir.SiraNo,
            BarkodNo = NullIfEmpty(satir.BarkodNo),
            PozNo = NullIfEmpty(satir.OlcuResmiPozNo),
            Tanim = NullIfEmpty(satir.Aciklama),
            SandikNo = NullIfEmpty(satir.CekideGecenSandikNo)
        };
    }

    internal static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    internal static void RevizyonOnayCakismasiFirlat(
        string mesaj,
        string kod,
        IReadOnlyCollection<CekiRevizyonSorunu>? mevcutSorunlar = null)
    {
        var sorunlar = new List<CekiRevizyonSorunu>
        {
            GenelRevizyonSorunuOlustur(
                kod,
                mesaj,
                CekiRevizyonSorunKategorileri.DurumCakismasi)
        };

        if (mevcutSorunlar != null)
            sorunlar.AddRange(mevcutSorunlar);

        throw new CekiRevizyonConflictException(mesaj, sorunlar);
    }
}
