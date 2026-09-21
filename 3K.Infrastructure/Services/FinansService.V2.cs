using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services;

public sealed partial class FinansService
{
    private static void Gerekce(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 5 || value.Length > 1000)
            throw new InvalidOperationException("İşlem gerekçesi 5–1000 karakter olmalıdır.");
    }

    public Task<FinansIsKaydiModel?> FinansTarihiDegistirAsync(int id, FinansTarihiDegistirModel model, CancellationToken cancellationToken)
        => ExecuteAtomicAsync(async () =>
        {
            Gerekce(model.Aciklama);
            if (model.FinansTarihi.Year is < 2000 or > 2200) throw new InvalidOperationException("Geçerli finans tarihi girin.");
            var work = await IsKaydiDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (work is null) return null;
            if (work.IptalEdildi) throw new InvalidOperationException("İptal edilmiş işin finans tarihi değiştirilemez.");
            AddAudit(nameof(FinansIsKaydi), id, "Finans Tarihi", nameof(work.FinansTarihi), work.FinansTarihi, model.FinansTarihi.Date, model.Aciklama);
            work.FinansTarihi = model.FinansTarihi.Date;
            work.FinansDonemi = FirstDayOfMonth(model.FinansTarihi);
            work.FinansTarihiManuel = true;
            await _context.SaveChangesAsync(cancellationToken);
            return MapIsKaydi(work);
        }, cancellationToken);

    public Task<FinansIsKaydiModel?> FiyatlandirAsync(int id, FinansFiyatlandirmaModel model, CancellationToken cancellationToken)
        => ExecuteAtomicAsync(async () =>
        {
            Gerekce(model.Aciklama);
            if (!Enum.IsDefined(model.FiyatlandirmaBirimi) || model.Adet <= 0 || model.BirimM3 < 0 || model.BirimFiyat < 0 || model.KdvOrani is < 0 or > 100)
                throw new InvalidOperationException("Fiyatlandırma yöntemi, miktar ve KDV değerlerini kontrol edin.");
            var work = await IsKaydiDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (work is null) return null;
            if (work.IptalEdildi || !work.KaynakAktif) throw new InvalidOperationException("Pasif iş fiyatlandırılamaz.");
            if (work.SiparisKalemleri.Count > 0)
                throw new InvalidOperationException("PO geçmişi bulunan işin fiyat snapshot'ı değiştirilemez; ek/düzeltme işi oluşturun.");
            var currency = NormalizeCurrency(model.ParaBirimi);
            if (currency is not ("EUR" or "USD" or "TRY")) throw new InvalidOperationException("Desteklenen para birimleri TRY, EUR ve USD'dir.");
            var componentTotal = ValidatePriceComponents(model.Bilesenler);
            if (model.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.ManuelToplam && model.ManuelNetTutar is not > 0 && componentTotal is null)
                throw new InvalidOperationException("Manuel toplam yönteminde pozitif net tutar zorunludur.");
            if (model.FiyatlandirmaBirimi != FinansFiyatlandirmaBirimi.ManuelToplam && (model.ManuelNetTutar.HasValue || componentTotal.HasValue))
                throw new InvalidOperationException("Manuel toplam veya bileşenler için ManuelToplam yöntemini seçin.");
            if (componentTotal.HasValue && model.ManuelNetTutar.HasValue && model.ManuelNetTutar.Value != componentTotal)
                throw new InvalidOperationException("Manuel net tutar bileşen toplamına eşit olmalıdır.");
            await ValidateTemplateValuesAsync(model, cancellationToken);
            var before = CaptureAuditState(work);
            work.FiyatlandirmaBirimiSnapshot = model.FiyatlandirmaBirimi;
            work.Adet = model.Adet;
            work.BirimM3 = model.BirimM3;
            work.ToplamM3 = decimal.Round(model.Adet * model.BirimM3, 6, MidpointRounding.AwayFromZero);
            work.BirimFiyatSnapshot = model.BirimFiyat;
            work.ManuelNetTutar = componentTotal ?? model.ManuelNetTutar;
            if (work.ManuelNetTutar.HasValue && work.ManuelNetTutar != FinansTutarKurallari.Para(work.ManuelNetTutar.Value))
                throw new InvalidOperationException("Net tutar en fazla iki ondalıklı olmalıdır.");
            work.ParaBirimiSnapshot = currency;
            work.KdvOraniSnapshot = model.KdvOrani;
            work.FinansMiktariManuel = true;
            work.SablonSurumId = model.SablonSurumId;
            work.AlanDegerleriJson = model.AlanDegerleri is null ? null : JsonSerializer.Serialize(model.AlanDegerleri);
            work.FiyatBilesenleriJson = model.Bilesenler is null ? null : JsonSerializer.Serialize(model.Bilesenler);
            if (FinansTutarKurallari.IsNet(work) <= 0) throw new InvalidOperationException("İş net bedeli sıfırdan büyük olmalıdır.");
            work.Durum = DetermineWorkStatus(work);
            AddAuditChanges(nameof(FinansIsKaydi), work, before);
            AddAudit(nameof(FinansIsKaydi), id, "Fiyatlandırma", "Gerekçe", null, null, model.Aciklama);
            await _context.SaveChangesAsync(cancellationToken);
            return MapIsKaydi(work);
        }, cancellationToken);

    internal static decimal? ValidatePriceComponents(IReadOnlyList<FinansFiyatBileseniModel>? components)
    {
        if (components is null || components.Count == 0) return null;
        if (components.Count > 100) throw new InvalidOperationException("En fazla 100 fiyat bileşeni tanımlanabilir.");
        if (components.Any(x => x is null || string.IsNullOrWhiteSpace(x.Ad) || x.Ad.Length > 150 || !Enum.IsDefined(x.Yontem) || x.Miktar <= 0 || x.BirimFiyat < 0))
            throw new InvalidOperationException("Bileşenin adı, yöntemi, pozitif miktarı ve geçerli fiyatı zorunludur.");
        return components.Sum(x => FinansTutarKurallari.Para((x.Yontem is FinansFiyatlandirmaBirimi.SabitTutar or FinansFiyatlandirmaBirimi.ManuelToplam ? 1 : x.Miktar) * x.BirimFiyat));
    }

    private async Task ValidateTemplateValuesAsync(FinansFiyatlandirmaModel model, CancellationToken cancellationToken)
    {
        if (!model.SablonSurumId.HasValue)
        {
            if (model.AlanDegerleri?.Count > 0) throw new InvalidOperationException("Dinamik alanlar için şablon sürümü seçilmelidir.");
            return;
        }
        var version = await _context.Set<FinansIsSablonSurumu>().AsNoTracking().Include(x => x.Sablon)
            .FirstOrDefaultAsync(x => x.Id == model.SablonSurumId, cancellationToken)
            ?? throw new InvalidOperationException("Şablon sürümü bulunamadı.");
        if (!version.Sablon.Aktif) throw new InvalidOperationException("Pasif şablonla yeni fiyatlandırma yapılamaz.");
        var fields = JsonSerializer.Deserialize<FinansSablonAlanModel[]>(version.AlanlarJson)!;
        var values = model.AlanDegerleri ?? new Dictionary<string, string?>();
        if (values.Keys.Any(key => fields.All(x => x.Kod != key))) throw new InvalidOperationException("Şablonda tanımlanmamış alan gönderildi.");
        foreach (var field in fields)
        {
            values.TryGetValue(field.Kod, out var value);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (field.Zorunlu) throw new InvalidOperationException($"{field.Ad} alanı zorunludur.");
                continue;
            }
            var valid = value.Length <= 2000 && (field.VeriTuru switch
            {
                "metin" => true,
                "sayi" => decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _),
                "tarih" => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                "mantiksal" => bool.TryParse(value, out _),
                _ => false
            });
            if (!valid) throw new InvalidOperationException($"{field.Ad} alanının veri türü geçersiz.");
        }
    }

    public async Task<IReadOnlyList<FinansSablonModel>> SablonlarAsync(CancellationToken cancellationToken)
    {
        var templates = await _context.Set<FinansIsSablonu>().AsNoTracking().Include(x => x.Surumler).OrderBy(x => x.Ad).ToListAsync(cancellationToken);
        return templates.Select(MapTemplate).ToArray();
    }

    private static FinansSablonModel MapTemplate(FinansIsSablonu template)
    {
        var version = template.Surumler.OrderByDescending(x => x.Surum).First();
        return new(template.Id, template.Kod, template.Ad, template.Aktif, version.Id, version.Surum,
            JsonSerializer.Deserialize<FinansSablonAlanModel[]>(version.AlanlarJson)!);
    }

    public Task<FinansSablonModel> SablonKaydetAsync(int? id, FinansSablonKaydetModel model, CancellationToken cancellationToken)
        => ExecuteAtomicAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(model.Kod) || model.Kod.Length > 80 || string.IsNullOrWhiteSpace(model.Ad) || model.Ad.Length > 200 || model.Alanlar is null || model.Alanlar.Count > 100)
                throw new InvalidOperationException("Şablon kodu, adı ve en fazla 100 alan gereklidir.");
            if (model.Alanlar.Any(x => x is null || string.IsNullOrWhiteSpace(x.Kod) || !Regex.IsMatch(x.Kod, "^[A-Za-z][A-Za-z0-9_]{0,49}$") || string.IsNullOrWhiteSpace(x.Ad) || x.Ad.Length > 150 || x.VeriTuru is not ("metin" or "sayi" or "tarih" or "mantiksal")) || model.Alanlar.GroupBy(x => x.Kod).Any(x => x.Count() > 1))
                throw new InvalidOperationException("Alan kodları tekil, adları dolu ve veri türleri metin/sayi/tarih/mantiksal olmalıdır.");
            var entity = id.HasValue ? await _context.Set<FinansIsSablonu>().Include(x => x.Surumler).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("Şablon bulunamadı.") : new FinansIsSablonu();
            entity.Kod = model.Kod.Trim().ToUpperInvariant();
            entity.Ad = model.Ad.Trim();
            entity.Aktif = model.Aktif;
            entity.Surumler.Add(new FinansIsSablonSurumu { Surum = entity.Surumler.Count == 0 ? 1 : entity.Surumler.Max(x => x.Surum) + 1, AlanlarJson = JsonSerializer.Serialize(model.Alanlar) });
            if (!id.HasValue) _context.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(nameof(FinansIsSablonu), entity.Id, "Şablon Sürümü", "Alanlar", null, JsonSerializer.Serialize(model.Alanlar));
            await _context.SaveChangesAsync(cancellationToken);
            return MapTemplate(entity);
        }, cancellationToken);

    private async Task<BaseEntity?> TargetAsync(string type, int id, CancellationToken cancellationToken) => type switch
    {
        "IsKaydi" => await _context.Set<FinansIsKaydi>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
        "Siparis" => await _context.Set<FinansSiparis>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
        "Fatura" => await _context.Set<FinansFatura>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
        "Gider" => await _context.Set<FinansGider>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
        _ => throw new InvalidOperationException("Varlık türü IsKaydi, Siparis, Fatura veya Gider olmalıdır.")
    };

    public async Task<FinansKaliciSilOnizlemeModel?> KaliciSilOnizlemeAsync(string varlikTuru, int id, CancellationToken cancellationToken)
    {
        var target = await TargetAsync(varlikTuru, id, cancellationToken);
        if (target is null) return null;
        var dependencies = new List<FinansBagimlilikModel>();
        if (target is FinansIsKaydi)
        {
            dependencies.AddRange(await _context.Set<FinansSiparisKalemi>().Where(x => x.FinansIsKaydiId == id)
                .Select(x => new FinansBagimlilikModel("Siparis", x.FinansSiparisId, x.FinansSiparis.PoNumarasi)).Distinct().ToListAsync(cancellationToken));
        }
        if (target is FinansSiparis)
        {
            dependencies.AddRange(await _context.Set<FinansFatura>().Where(x => x.FinansSiparisId == id)
                .Select(x => new FinansBagimlilikModel("Fatura", x.Id, x.FaturaNumarasi)).ToListAsync(cancellationToken));
            dependencies.AddRange(await _context.Set<FinansSiparisKalemi>().Where(x => x.FinansSiparisId == id)
                .Select(x => new FinansBagimlilikModel("SiparisKalemi", x.Id, x.FinansIsKaydi.ProjeNo)).ToListAsync(cancellationToken));
        }
        if (target is FinansFatura)
            dependencies.AddRange(await _context.Set<FinansFaturaKalemi>().Where(x => x.FinansFaturaId == id)
                .Select(x => new FinansBagimlilikModel("FaturaKalemi", x.Id, x.FinansSiparisKalemi.FinansIsKaydi.ProjeNo)).ToListAsync(cancellationToken));
        if (target is FinansGider)
            dependencies.AddRange(await _context.Set<FinansGider>().Where(x => x.MahsupEdilenAvansId == id)
                .Select(x => new FinansBagimlilikModel("Gider", x.Id, x.Aciklama)).ToListAsync(cancellationToken));
        dependencies.AddRange(await _context.Set<FinansBelge>().Where(x => x.HedefTuru == varlikTuru && x.HedefId == id)
            .Select(x => new FinansBagimlilikModel("Belge", x.Id, x.OrijinalAd)).ToListAsync(cancellationToken));
        var reference = target switch { FinansIsKaydi x => $"{x.ProjeNo} / {x.IsAdi}", FinansSiparis x => x.PoNumarasi, FinansFatura x => x.FaturaNumarasi, FinansGider x => x.Aciklama, _ => id.ToString() };
        var snapshot = JsonSerializer.Serialize(new { varlikTuru, id, State = CaptureAuditState(target), target.UpdatedDate, Dependencies = dependencies.OrderBy(x => x.VarlikTuru).ThenBy(x => x.Id) });
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
        return new(varlikTuru, id, reference, version, dependencies.Count == 0, dependencies,
            dependencies.Count == 0 ? Array.Empty<string>() : ["Bağlı kayıtlar bulundu. Ortak PO/fatura satırları birlikte silinmez; normal iptal/düzeltme akışını kullanın."]);
    }

    public Task<bool> KaliciSilAsync(FinansKaliciSilModel model, CancellationToken cancellationToken)
        => ExecuteAtomicAsync(async () =>
        {
            Gerekce(model.Aciklama);
            if (!model.IkinciOnay) throw new InvalidOperationException("Geri alınamaz kalıcı silme için ikinci onay zorunludur.");
            var preview = await KaliciSilOnizlemeAsync(model.VarlikTuru, model.Id, cancellationToken);
            if (preview is null) return false;
            if (preview.Surum != model.Surum) throw new InvalidOperationException("Kayıt veya ilişkileri değişti; bağımlılık önizlemesini yeniden açın.");
            if (!preview.Silinebilir) throw new InvalidOperationException(string.Join(" ", preview.Engeller));
            var entity = (await TargetAsync(model.VarlikTuru, model.Id, cancellationToken))!;
            if (entity is FinansIsKaydi work && work.KaynakKayitId is not null)
                _context.Add(new FinansKaynakBastirma { KaynakTuru = work.KaynakTuru, KaynakKayitId = work.KaynakKayitId, KaynakBileseni = work.KaynakBileseni, Aciklama = model.Aciklama.Trim() });
            AddAudit(entity.GetType().Name, model.Id, "Kalıcı Silme", "*", JsonSerializer.Serialize(CaptureAuditState(entity)), preview.Referans, model.Aciklama);
            _context.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);

    public async Task<IReadOnlyList<FinansBelgeModel>> BelgelerAsync(string hedefTuru, int hedefId, CancellationToken cancellationToken)
    {
        if (await TargetAsync(hedefTuru, hedefId, cancellationToken) is null) throw new InvalidOperationException("Belge hedefi bulunamadı.");
        return await _context.Set<FinansBelge>().AsNoTracking().Where(x => x.HedefTuru == hedefTuru && x.HedefId == hedefId).OrderByDescending(x => x.Surum)
            .Select(x => new FinansBelgeModel(x.Id, x.HedefTuru, x.HedefId, x.Surum, x.OrijinalAd, x.Boyut, x.IcerikTuru, x.Hash, x.Yukleyen, x.CreatedDate)).ToListAsync(cancellationToken);
    }

    internal static void ValidatePdf(byte[] bytes, string name)
    {
        if (bytes.Length is < 8 or > 10 * 1024 * 1024 || !name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
            !bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8) || !Encoding.ASCII.GetString(bytes.AsSpan(Math.Max(0, bytes.Length - 1024))).Contains("%%EOF", StringComparison.Ordinal))
            throw new InvalidOperationException("En fazla 10 MB boyutunda geçerli bir PDF yükleyin.");
    }

    public Task<FinansBelgeModel> BelgeYukleAsync(FinansBelgeYukleModel model, CancellationToken cancellationToken)
        => ExecuteAtomicAsync(async () =>
        {
            ValidatePdf(model.Icerik, model.OrijinalAd);
            if (model.HedefTuru == "IsKaydi") throw new InvalidOperationException("PDF yalnız PO, fatura veya gider belgesine bağlanabilir.");
            if (await TargetAsync(model.HedefTuru, model.HedefId, cancellationToken) is null) throw new InvalidOperationException("Belge hedefi bulunamadı.");
            var name = Path.GetFileName(model.OrijinalAd.Replace('\\', '/'));
            name = new string(name.Where(c => !char.IsControl(c)).ToArray());
            if (name.Length > 250) name = name[..246] + ".pdf";
            var entity = new FinansBelge
            {
                HedefTuru = model.HedefTuru, HedefId = model.HedefId,
                Surum = (await _context.Set<FinansBelge>().Where(x => x.HedefTuru == model.HedefTuru && x.HedefId == model.HedefId).MaxAsync(x => (int?)x.Surum, cancellationToken) ?? 0) + 1,
                OrijinalAd = name, GuvenliAd = $"{Guid.NewGuid():N}.pdf", Icerik = model.Icerik,
                Boyut = model.Icerik.Length, Hash = Convert.ToHexString(SHA256.HashData(model.Icerik)),
                Yukleyen = _currentUser.IslemKullaniciId?.ToString(CultureInfo.InvariantCulture) ?? "SYSTEM"
            };
            _context.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit(nameof(FinansBelge), entity.Id, "PDF Yükleme", "Sürüm", null, entity.Surum, $"{model.HedefTuru}/{model.HedefId}");
            await _context.SaveChangesAsync(cancellationToken);
            return new FinansBelgeModel(entity.Id, entity.HedefTuru, entity.HedefId, entity.Surum, entity.OrijinalAd, entity.Boyut, entity.IcerikTuru, entity.Hash, entity.Yukleyen, entity.CreatedDate);
        }, cancellationToken);

    public async Task<FinansBelgeIcerikModel?> BelgeIndirAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.Set<FinansBelge>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        if (await TargetAsync(entity.HedefTuru, entity.HedefId, cancellationToken) is null) throw new InvalidOperationException("Belge hedefi bulunamadı.");
        return new(entity.Icerik, entity.OrijinalAd, entity.IcerikTuru);
    }
}
