using System.Globalization;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// Finans değişiklik geçmişi. Kayıtlar izlenen bağlama eklenir ve işlemin kendi kaydıyla aynı
/// transaction'da yazılır. Aynı kapsamdaki (istek ya da arka plan turu) kayıtlar tek işlem
/// grubunu paylaşır.
/// </summary>
internal sealed class FinansDenetimKaydi(
    AppDbContext context,
    ICurrentUserService currentUser)
{
    private readonly Guid _islemGrubu = Guid.NewGuid();

    /// <summary>Kayıt ekler; referans, bağlamda izlenen hedef varlıktan (iş, PO, fatura, gider) türetilir.</summary>
    public void Ekle(
        string varlikTuru,
        int varlikId,
        string islem,
        string alanAdi,
        object? eskiDeger,
        object? yeniDeger,
        string? aciklama = null)
    {
        var target = context.ChangeTracker.Entries<BaseEntity>().Select(x => x.Entity)
            .FirstOrDefault(x => x.Id == varlikId && x.GetType().Name == varlikTuru);
        var reference = target switch { FinansIsKaydi x => $"{x.ProjeNo} / {x.IsAdi}", FinansSiparis x => x.PoNumarasi,
            FinansFatura x => x.FaturaNumarasi, FinansGider x => x.BelgeNo ?? x.Aciklama, _ => $"{varlikTuru}/{varlikId}" };
        context.Set<FinansDegisiklikGecmisi>().Add(new FinansDegisiklikGecmisi
        {
            VarlikTuru = varlikTuru,
            VarlikId = varlikId,
            Islem = islem,
            AlanAdi = alanAdi,
            EskiDeger = DegerMetni(eskiDeger),
            YeniDeger = DegerMetni(yeniDeger),
            Aciklama = aciklama,
            IslemYapan = currentUser.IslemKullaniciId?.ToString(CultureInfo.InvariantCulture) ?? "SYSTEM",
            IslemGrubu = _islemGrubu,
            Referans = reference.Length <= 500 ? reference : reference[..500],
            CreatedBy = currentUser.IslemKullaniciId?.ToString(CultureInfo.InvariantCulture)
        });
    }

    /// <summary>Değer değiştiyse "Güncelleme" kaydı ekler.</summary>
    public void DegisiklikEkle<T>(string varlikTuru, int varlikId, string alan, T oldValue, T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            return;
        Ekle(varlikTuru, varlikId, "Güncelleme", alan, oldValue, newValue);
    }

    /// <summary><see cref="AnlikGoruntu"/> ile alınan önceki duruma göre değişen her alan için kayıt ekler.</summary>
    public void DegisiklikleriEkle(string varlikTuru, BaseEntity entity, IReadOnlyDictionary<string, object?> before)
    {
        var type = entity.GetType();
        foreach (var pair in before)
        {
            var current = type.GetProperty(pair.Key)?.GetValue(entity);
            if (Equals(pair.Value, current))
                continue;
            Ekle(varlikTuru, entity.Id, "Güncelleme", pair.Key, pair.Value, current);
        }
    }

    /// <summary>Varlığın denetlenen skaler alanları; kimlik ve zaman damgaları hariç.</summary>
    public static IReadOnlyDictionary<string, object?> AnlikGoruntu(BaseEntity entity)
        => entity.GetType()
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(property => property.CanRead && IsAuditScalar(property.PropertyType))
            .Where(property => property.Name is not nameof(BaseEntity.Id)
                and not nameof(BaseEntity.CreatedDate)
                and not nameof(BaseEntity.UpdatedDate)
                and not nameof(BaseEntity.CreatedBy)
                and not nameof(BaseEntity.UpdatedBy))
            .ToDictionary(property => property.Name, property => property.GetValue(entity));

    /// <summary>Denetim kaydındaki değerin kültürden bağımsız metni.</summary>
    public static string? DegerMetni(object? value)
        => value switch
        {
            null => null,
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            decimal number => number.ToString(CultureInfo.InvariantCulture),
            bool flag => flag ? "true" : "false",
            Enum enumValue => $"{Convert.ToInt32(enumValue, CultureInfo.InvariantCulture)}:{enumValue}",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };

    private static bool IsAuditScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsEnum || actual == typeof(string) || actual == typeof(bool) ||
               actual == typeof(byte) || actual == typeof(short) || actual == typeof(int) ||
               actual == typeof(long) || actual == typeof(decimal) || actual == typeof(float) ||
               actual == typeof(double) || actual == typeof(DateTime) || actual == typeof(Guid);
    }
}
