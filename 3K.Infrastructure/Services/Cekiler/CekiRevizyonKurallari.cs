using ClosedXML.Excel;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

namespace _3K.Infrastructure.Services.Cekiler;

/// <summary>Çeki revizyonunun satır eşleştirme, işlem görmüşlük, sandık taşınabilirliği ve stok geri alma kuralları; ön izleme ile uygulama aynı kuralları kullanır.</summary>
internal static class CekiRevizyonKurallari
{
    internal const string RevizyonAktifSahaKaynakGuncellemeMesaji =
        "Bu revizyon U satırı aktif bir saha aktarımının normal proje kaynağıdır. Kaynak miktar ve işlem geçmişi korunacağı için güncellenemez. Sevk kilidini açmak saha aktarım korumasını kaldırmaz; kaynak/saha ilişkisi birlikte incelenmelidir.";

    internal static CekiSatiri? EslesenAnaSatiriBul(
        CiktiSatirImportBilgisi revizyonSatiri,
        Dictionary<int, List<CekiSatiri>> siraGruplari,
        List<CekiSatiri> anaSatirlar)
    {
        var barkod = CekiExcelOkuyucu.NormalizeTextKey(revizyonSatiri.BarkodNo);
        var poz = CekiExcelOkuyucu.NormalizeTextKey(revizyonSatiri.OlcuResmiPozNo);
        var koli = CekiExcelOkuyucu.NormalizeKoliNo(revizyonSatiri.KoliNo);

        var adaylar = new List<CekiSatiri>();

        if (!string.IsNullOrWhiteSpace(barkod))
        {
            adaylar = anaSatirlar
                .Where(s =>
                    CekiExcelOkuyucu.NormalizeTextKey(s.BarkodNo) == barkod &&
                    CekiExcelOkuyucu.NormalizeTextKey(s.OlcuResmiPozNo) == poz &&
                    CekiExcelOkuyucu.NormalizeKoliNo(s.CekideGecenSandikNo) == koli)
                .ToList();

            if (adaylar.Count == 1)
                return adaylar[0];

            adaylar = anaSatirlar
                .Where(s =>
                    CekiExcelOkuyucu.NormalizeTextKey(s.BarkodNo) == barkod &&
                    CekiExcelOkuyucu.NormalizeTextKey(s.OlcuResmiPozNo) == poz)
                .ToList();

            if (adaylar.Count == 1)
                return adaylar[0];

            adaylar = anaSatirlar
                .Where(s => CekiExcelOkuyucu.NormalizeTextKey(s.BarkodNo) == barkod)
                .ToList();

            if (adaylar.Count == 1)
                return adaylar[0];
        }

        if (siraGruplari.TryGetValue(revizyonSatiri.SiraNo, out var siraEslesmeleri) && siraEslesmeleri.Count == 1)
            return siraEslesmeleri[0];

        return adaylar.Count == 1 ? adaylar[0] : null;
    }

    internal static bool SandikSevkKilitliMi(Sandik? sandik)
    {
        return sandik?.DurumId == (int)SandikDurum.Sevkedildi && !sandik.SevkiyatDuzeltmeAcikMi;
    }

    internal static bool RevizyonIcerikleriTasinabilirMi(List<SandikIcerik> icerikler)
    {
        return icerikler.All(i =>
            !SandikSevkKilitliMi(i.Sandik) &&
            i.KonulanAdet == 0 &&
            i.EksikAdet == 0 &&
            i.StokKarsilanan == 0 &&
            i.ProjeKarsilanan == 0 &&
            i.TedarikciKarsilanan == 0);
    }

    internal static bool RevizyonTekTamTahsisSandigiUyumsuzMu(
        int projeId, string? eskiKoliNo, string? eskiFiiliKoliNo, decimal eskiIstenenAdet,
        string? yeniKoliNo, IReadOnlyList<SandikIcerik> icerikler)
    {
        var plan = CekiExcelOkuyucu.NormalizeKoliNo(eskiKoliNo);
        var fiili = CekiExcelOkuyucu.NormalizeKoliNo(eskiFiiliKoliNo);
        var hedef = CekiExcelOkuyucu.NormalizeKoliNo(yeniKoliNo);
        if (icerikler.Count != 1 || string.IsNullOrWhiteSpace(hedef) ||
            !string.Equals(plan, hedef, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(fiili) && !string.Equals(fiili, plan, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Eski revizyon plan/fiili numarasını yazıp FK'yı eski sandıkta bırakmış olabilir.
        // Yalnız planı izleyen tek tam tahsisi düzelt; bölünmüş/kısmi dağılımı tahmin etme.
        var icerik = icerikler[0];
        return (icerik.TahsisMiktari <= 0 || icerik.TahsisMiktari == eskiIstenenAdet) &&
            icerik.Sandik?.ProjeId == projeId &&
            !string.IsNullOrWhiteSpace(CekiExcelOkuyucu.NormalizeKoliNo(icerik.Sandik.SandikNo)) &&
            !string.Equals(CekiExcelOkuyucu.NormalizeKoliNo(icerik.Sandik.SandikNo), hedef, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool CekiSatiriIslemGormusMu(CekiSatiri satir)
    {
        return satir.GridDurumuId != (int)GridDurum.Bekliyor
            || satir.GridGelenAdet > 0
            || satir.TrafoSevkAdet > 0
            || (satir.GridSevkMiktari ?? 0) > 0
            || satir.UcKDurumuId != (int)UcKDurum.Bekliyor
            || satir.GelenMiktar > 0
            || satir.StokKarsilanan > 0
            || satir.ProjeKarsilanan > 0
            || satir.ProjeGonderilen > 0
            || satir.TedarikciKarsilanan > 0
            || satir.HataliMiktar > 0
            || satir.GeriGonderilenMiktar > 0
            || satir.SandikIcerikleri.Any(i => i.KonulanAdet > 0 || i.EksikAdet > 0 || i.StokKarsilanan > 0 || i.ProjeKarsilanan > 0 || i.TedarikciKarsilanan > 0);
    }

    internal static decimal CekiSatiriIslemMiktari(CekiSatiri satir)
    {
        var gridMiktari = Math.Max(satir.GridGelenAdet, satir.GridSevkMiktari ?? 0) + satir.TrafoSevkAdet;
        var ucKMiktari = satir.GelenMiktar + satir.StokKarsilanan + satir.ProjeKarsilanan + satir.TedarikciKarsilanan - satir.ProjeGonderilen;
        var sandikMiktari = satir.SandikIcerikleri.Sum(i => i.KonulanAdet + i.StokKarsilanan + i.ProjeKarsilanan + i.TedarikciKarsilanan);
        return Math.Max(Math.Max(gridMiktari, ucKMiktari), sandikMiktari);
    }

    internal static HashSet<int> RevizyonNormalKaynakSatirIds(
        int projeTipiId,
        IEnumerable<CekiSatiri> satirlar)
    {
        // Hedef saha satırları ve normal projedeki türemiş satırlar bu U korumasının
        // kapsamı değildir; D/silme koruması mevcut kapsamıyla ayrıca çalışır.
        return projeTipiId == (int)ProjeTipi.Normal
            ? satirlar.Where(s => s.Id > 0 && !s.KaynakCekiSatiriId.HasValue).Select(s => s.Id).ToHashSet()
            : new HashSet<int>();
    }

    internal static bool RevizyonBenzerSatirVarMi(CiktiSatirImportBilgisi revizyonSatiri, List<CekiSatiri> anaSatirlar)
    {
        var barkod = CekiExcelOkuyucu.NormalizeTextKey(revizyonSatiri.BarkodNo);
        if (string.IsNullOrWhiteSpace(barkod))
            return false;

        var poz = CekiExcelOkuyucu.NormalizeTextKey(revizyonSatiri.OlcuResmiPozNo);
        var koli = CekiExcelOkuyucu.NormalizeKoliNo(revizyonSatiri.KoliNo);

        return anaSatirlar.Any(s =>
            CekiExcelOkuyucu.NormalizeTextKey(s.BarkodNo) == barkod &&
            CekiExcelOkuyucu.NormalizeTextKey(s.OlcuResmiPozNo) == poz &&
            CekiExcelOkuyucu.NormalizeKoliNo(s.CekideGecenSandikNo) == koli);
    }

    internal static string FormatRevizyonDecimal(decimal value)
    {
        return decimal.Truncate(value) == value
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    internal static List<RevizyonStokEngeli> RevizyonStokGeriAlEngelleriniBul(
        IReadOnlyCollection<StokHareketi> stokHareketleri,
        RevizyonStokKontrolBaglami kontrolBaglami)
    {
        if (stokHareketleri.Count == 0)
            return new List<RevizyonStokEngeli>();

        var geriAlinacakHareketIdleri = stokHareketleri.Select(h => h.Id).ToHashSet();
        var engeller = new List<RevizyonStokEngeli>();

        foreach (var grup in stokHareketleri.GroupBy(h => h.StokKaydiId))
        {
            if (!kontrolBaglami.Stoklar.TryGetValue(grup.Key, out var stok))
                continue;

            var fazlaTeslimdenAktarilan = grup
                .Where(h => h.IslemTipiId == (int)IslemTipi.FazlaTeslimStogaAktarildi)
                .Sum(h => Math.Abs(h.Miktar));

            if (fazlaTeslimdenAktarilan <= 0)
                continue;

            var etkilenenCekiSatiriIds = grup
                .Select(h => h.CekiSatiriId)
                .Distinct()
                .ToHashSet();
            var stokEtiketi = string.IsNullOrWhiteSpace(stok.MalzemeKodu)
                ? stok.MalzemeAdi
                : $"{stok.MalzemeKodu} - {stok.MalzemeAdi}";
            var baskaHareketVarMi = kontrolBaglami.TumStokHareketleri.Any(h =>
                h.StokKaydiId == grup.Key &&
                !geriAlinacakHareketIdleri.Contains(h.Id));

            if (baskaHareketVarMi)
            {
                engeller.Add(new RevizyonStokEngeli(
                    CekiRevizyonSorunKodlari.StokHareketiKullanilmis,
                    $"Fazla teslimden oluşan stok ({stokEtiketi}) başka bir işlemde kullanılmış. Önce bu stoku kullanan işlemleri geri alın.",
                    etkilenenCekiSatiriIds));
                continue;
            }

            if (stok.Miktar < fazlaTeslimdenAktarilan)
            {
                engeller.Add(new RevizyonStokEngeli(
                    CekiRevizyonSorunKodlari.StokMiktariYetersiz,
                    $"Fazla teslimden oluşan stok ({stokEtiketi}) geri alma için yetersiz. Gerekli: {FormatRevizyonDecimal(fazlaTeslimdenAktarilan)}, mevcut: {FormatRevizyonDecimal(stok.Miktar)}.",
                    etkilenenCekiSatiriIds));
            }
        }

        return engeller;
    }
}
