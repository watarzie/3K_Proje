using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services;

public sealed class CekiRevizyonGecmisiService : ICekiRevizyonGecmisiService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext context;
    private readonly string _uploadsRoot;

    public CekiRevizyonGecmisiService(AppDbContext context)
        : this(context, Path.Combine(Directory.GetCurrentDirectory(), "Uploads")) { }

    public CekiRevizyonGecmisiService(AppDbContext context, string uploadsRoot)
    {
        this.context = context;
        _uploadsRoot = Path.GetFullPath(uploadsRoot);
    }

    // Büyük Excel ve snapshot alanları liste sorgusunda seçilmez. Legacy çeki,
    // talebi varsa ikinci kez listelenmez; bugün değişmiş satırlardan tarihçe üretilmez.
    private IQueryable<GecmisSatiri> Sorgu(int projeId)
    {
        var talepler = context.CekiRevizyonTalepleri.AsNoTracking().Where(t => t.ProjeId == projeId)
            .Select(t => new GecmisSatiri
            {
                Kaynak = "talep", KayitId = t.Id, ProjeId = t.ProjeId,
                RevizyonCekiId = t.UygulananRevizyonCekiId, DosyaAdi = t.DosyaAdi,
                Yukleyen = t.TalepEdenKullanici.AdSoyad, YuklemeTarihi = t.CreatedDate,
                UygulamaTarihi = t.UygulamaTarihi, Eklenen = t.EklenenSatirSayisi,
                Guncellenen = t.GuncellenenSatirSayisi, Silinen = t.SilinenSatirSayisi,
                BlobMevcut = t.DosyaIcerigi != null, SnapshotMevcut = t.OnizlemeJson != null,
                DosyaYolu = t.UygulananRevizyonCeki != null ? t.UygulananRevizyonCeki.OrijinalDosyaYolu : null
            });
        var eski = context.Cekiler.AsNoTracking()
            .Where(c => c.ProjeId == projeId && c.CekiTipiId == (int)CekiTipi.Revizyon &&
                !context.CekiRevizyonTalepleri.Any(t => t.UygulananRevizyonCekiId == c.Id))
            .Select(c => new GecmisSatiri
            {
                Kaynak = "ceki", KayitId = c.Id, ProjeId = c.ProjeId,
                RevizyonCekiId = c.Id, DosyaAdi = c.Aciklama ?? "", Yukleyen = c.CreatedBy,
                YuklemeTarihi = c.YuklemeTarihi, UygulamaTarihi = null,
                Eklenen = null, Guncellenen = null, Silinen = null,
                BlobMevcut = false, SnapshotMevcut = false, DosyaYolu = c.OrijinalDosyaYolu
            });
        return talepler.Concat(eski);
    }

    public async Task<CekiRevizyonGecmisiSayfa> ListeleAsync(int projeId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = Sorgu(projeId);
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.YuklemeTarihi).ThenBy(x => x.Kaynak)
            .ThenByDescending(x => x.KayitId).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        await EskiYukleyenleriCozAsync(rows, cancellationToken);
        var ids = rows.Where(r => r.Kaynak == "talep").Select(r => r.KayitId).ToList();
        var approvals = await context.OnayBekleyenIslemler.AsNoTracking()
            .Where(o => o.ProjeId == projeId && o.IslemKodu == OnayIslemKodlari.CekiRevizyonuUygula &&
                o.ReferansTipi == OnayReferansTipleri.CekiRevizyonTalebi && o.ReferansId.HasValue && ids.Contains(o.ReferansId.Value))
            .OrderByDescending(o => o.Id)
            .Select(o => new OnayOzeti(o.ReferansId!.Value, o.Durum, o.CalistirmaDurumu,
                o.OnaylayanKullanici != null ? o.OnaylayanKullanici.AdSoyad : null, o.KararTarihi))
            .ToListAsync(cancellationToken);
        return new CekiRevizyonGecmisiSayfa
        {
            TotalCount = count,
            Items = rows.Select(row => Donustur(row, approvals.FirstOrDefault(o => o.TalepId == row.KayitId && row.Kaynak == "talep"))).ToList()
        };
    }

    public async Task<CekiRevizyonGecmisiDetayi?> DetayAsync(int projeId, string kaynak, int kayitId, CancellationToken cancellationToken)
    {
        var row = await Sorgu(projeId).FirstOrDefaultAsync(r => r.Kaynak == kaynak && r.KayitId == kayitId, cancellationToken);
        if (row == null) return null;
        await EskiYukleyenleriCozAsync([row], cancellationToken);
        var approval = kaynak == "talep" ? await context.OnayBekleyenIslemler.AsNoTracking()
            .Where(o => o.ProjeId == projeId && o.ReferansId == kayitId && o.ReferansTipi == OnayReferansTipleri.CekiRevizyonTalebi && o.IslemKodu == OnayIslemKodlari.CekiRevizyonuUygula)
            .OrderByDescending(o => o.Id)
            .Select(o => new OnayOzeti(kayitId, o.Durum, o.CalistirmaDurumu, o.OnaylayanKullanici != null ? o.OnaylayanKullanici.AdSoyad : null, o.KararTarihi))
            .FirstOrDefaultAsync(cancellationToken) : null;
        var result = new CekiRevizyonGecmisiDetayi { Kayit = Donustur(row, approval) };
        if (kaynak != "talep") return result;
        var snapshot = await context.CekiRevizyonTalepleri.AsNoTracking().Where(t => t.Id == kayitId && t.ProjeId == projeId)
            .Select(t => new { t.OnizlemeJson, t.OnizlemeHash, t.OnizlemeSurumu, t.AnaCekiId }).SingleAsync(cancellationToken);
        result.Onizleme = SnapshotOku(snapshot.OnizlemeJson, snapshot.OnizlemeHash, snapshot.OnizlemeSurumu, projeId, snapshot.AnaCekiId);
        result.Kayit.DetayMevcut = result.Onizleme != null;
        if (result.Onizleme == null) result.Kayit.Bilgi = "Tarihsel detay yok veya snapshot bütünlüğü doğrulanamadı. Güncel veriden geçmiş üretilmedi.";
        return result;
    }

    public async Task<CekiRevizyonDosyasi?> DosyaAsync(int projeId, string kaynak, int kayitId, CancellationToken cancellationToken)
    {
        var row = await Sorgu(projeId).FirstOrDefaultAsync(r => r.Kaynak == kaynak && r.KayitId == kayitId, cancellationToken);
        if (row == null) return null;
        byte[]? bytes = null;
        string? expectedHash = null;
        if (kaynak == "talep")
        {
            var file = await context.CekiRevizyonTalepleri.AsNoTracking().Where(t => t.Id == kayitId && t.ProjeId == projeId)
                .Select(t => new { t.DosyaIcerigi, t.DosyaSha256 }).SingleAsync(cancellationToken);
            bytes = file.DosyaIcerigi;
            expectedHash = file.DosyaSha256;
        }
        if (bytes == null)
        {
            var path = RevizyonDosyaDeposu.GuvenliYol(_uploadsRoot, projeId, row.DosyaYolu);
            if (path == null) return null;
            try
            {
                // Yüklemedeki sınırı arşiv okumalarında da koru.
                if (new FileInfo(path).Length > 20 * 1024 * 1024) return null;
                bytes = await File.ReadAllBytesAsync(path, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return null; }
        }
        if (expectedHash != null && !string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), expectedHash, StringComparison.OrdinalIgnoreCase)) return null;
        return new CekiRevizyonDosyasi(bytes, DosyaAdi(row));
    }

    public static CekiRevizyonOnizlemeSonuc? SnapshotOku(string json, string hash, int surum, int projeId, int anaCekiId)
    {
        try
        {
            if (surum != CekiRevizyonOnizlemeButunlugu.Surum || string.IsNullOrWhiteSpace(json)) return null;
            var value = JsonSerializer.Deserialize<CekiRevizyonOnizlemeSonuc>(json, JsonOptions);
            if (value?.SandikEtkileri?.Any(x => x == null) == true || value?.Satirlar?.Any(x => x == null) == true) return null;
            return value != null && value.ProjeId == projeId && value.AnaCekiId == anaCekiId &&
                CekiRevizyonOnizlemeButunlugu.HashDogrula(value, hash) ? value : null;
        }
        catch (Exception exception) when (exception is JsonException or NullReferenceException or ArgumentException) { return null; }
    }

    public static string DurumBelirle(bool uygulandi, bool legacy, OnayDurumu? karar, OnayCalistirmaDurumu? calistirma)
    {
        if (uygulandi) return "Uygulandi";
        if (legacy) return "EskiKayit";
        if (karar == OnayDurumu.Reddedildi) return "Reddedildi";
        if (calistirma == OnayCalistirmaDurumu.Basarisiz) return "Basarisiz";
        if (calistirma == OnayCalistirmaDurumu.Calisiyor) return "Uygulaniyor";
        if (karar == OnayDurumu.Bekliyor) return "OnayBekliyor";
        if (karar == OnayDurumu.Onaylandi) return "Onaylandi";
        return "Uygulanmadi";
    }

    private CekiRevizyonGecmisiKaydi Donustur(GecmisSatiri row, OnayOzeti? approval) => new()
    {
        Kaynak = row.Kaynak, KayitId = row.KayitId, ProjeId = row.ProjeId, RevizyonCekiId = row.RevizyonCekiId,
        DosyaAdi = DosyaAdi(row), Yukleyen = row.Yukleyen, YuklemeTarihi = row.YuklemeTarihi,
        UygulamaTarihi = row.UygulamaTarihi, Onaylayan = approval?.Onaylayan, KararTarihi = approval?.KararTarihi,
        Durum = DurumBelirle(row.UygulamaTarihi.HasValue && row.RevizyonCekiId.HasValue, row.Kaynak == "ceki", approval?.Durum, approval?.CalistirmaDurumu),
        EklenenSatirSayisi = row.Eklenen, GuncellenenSatirSayisi = row.Guncellenen, SilinenSatirSayisi = row.Silinen,
        DosyaMevcut = row.BlobMevcut || RevizyonDosyaDeposu.GuvenliYol(_uploadsRoot, row.ProjeId, row.DosyaYolu) != null,
        DetayMevcut = row.SnapshotMevcut,
        Bilgi = row.Kaynak == "ceki" ? "Eski revizyon kaydı: değişmez detay, yükleyen veya uygulama zamanı kaydedilmemiş olabilir." : null
    };

    private static string DosyaAdi(GecmisSatiri row) => RevizyonDosyaDeposu.IndirmeAdi(row.Kaynak == "ceki"
        ? row.DosyaAdi.StartsWith("Revizyon dosyası: ", StringComparison.Ordinal) ? row.DosyaAdi["Revizyon dosyası: ".Length..] : "revizyon.xlsx"
        : row.DosyaAdi);

    private async Task EskiYukleyenleriCozAsync(List<GecmisSatiri> rows, CancellationToken cancellationToken)
    {
        var ids = rows.Where(row => row.Kaynak == "ceki").Select(row => int.TryParse(row.Yukleyen, out var id) ? id : 0)
            .Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0) return;
        var names = await context.Kullanicilar.AsNoTracking().Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, user.AdSoyad }).ToDictionaryAsync(user => user.Id, user => user.AdSoyad, cancellationToken);
        foreach (var row in rows.Where(row => row.Kaynak == "ceki"))
            if (int.TryParse(row.Yukleyen, out var id)) row.Yukleyen = names.GetValueOrDefault(id);
    }

    private sealed record OnayOzeti(int TalepId, OnayDurumu Durum, OnayCalistirmaDurumu CalistirmaDurumu, string? Onaylayan, DateTime? KararTarihi);
    private sealed class GecmisSatiri
    {
        public string Kaynak { get; set; } = "";
        public int KayitId { get; set; }
        public int ProjeId { get; set; }
        public int? RevizyonCekiId { get; set; }
        public string DosyaAdi { get; set; } = "";
        public string? Yukleyen { get; set; }
        public DateTime YuklemeTarihi { get; set; }
        public DateTime? UygulamaTarihi { get; set; }
        public int? Eklenen { get; set; }
        public int? Guncellenen { get; set; }
        public int? Silinen { get; set; }
        public bool BlobMevcut { get; set; }
        public bool SnapshotMevcut { get; set; }
        public string? DosyaYolu { get; set; }
    }
}
