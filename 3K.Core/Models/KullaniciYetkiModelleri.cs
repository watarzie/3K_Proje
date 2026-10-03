using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Rol atama denetiminin sonucu; başarısızsa kullanıcıya gösterilecek mesaj ve HTTP durum kodu.</summary>
public sealed record RolAtamaSonucu(bool Basarili, string? Hata = null, int DurumKodu = 200);

/// <summary>Yetki hesabı için menü ağacındaki bir düğüm.</summary>
public sealed record YetkiDugumu(int Id, string Kod, int? UstId);

/// <summary>
/// Menü ağacı üzerinde etkin yetki hesabının tek uygulaması. Bütün ekran, işlem ve alan
/// kodları aynı kuralla değerlendirilir: rolün atadığı seviye üst düğümün etkin seviyesiyle sınırlanır
/// (üst düğüm tavandır). Yetki yalnız rolden gelir; kullanıcıya özel izin veya ret yoktur.
/// Üst W alt izni kendiliğinden vermez; kaydı olmayan düğüm N'dir. Döngü veya kayıp üst düğüm N'dir.
/// </summary>
public sealed class YetkiAgaci
{
    private readonly IReadOnlyDictionary<int, YetkiDugumu> _dugumler;

    public YetkiAgaci(IEnumerable<YetkiDugumu> dugumler)
    {
        _dugumler = dugumler.ToDictionary(x => x.Id);
    }

    public IEnumerable<YetkiDugumu> Dugumler => _dugumler.Values;

    /// <summary>Rolün atanmış seviyelerini ağaçta sınırlar; kullanıcının etkin seviyeleri budur.</summary>
    public IReadOnlyDictionary<int, YetkiTipi> RolSeviyeleri(IReadOnlyDictionary<int, int> atanmisSeviyeler) =>
        Sinirla(id => atanmisSeviyeler.TryGetValue(id, out var seviye) && Enum.IsDefined((YetkiTipi)seviye)
            ? (YetkiTipi)seviye
            : YetkiTipi.N);

    private IReadOnlyDictionary<int, YetkiTipi> Sinirla(Func<int, YetkiTipi> yerelSeviye)
    {
        var sonuc = new Dictionary<int, YetkiTipi>(_dugumler.Count);
        var ziyaret = new HashSet<int>();

        YetkiTipi Hesapla(int id)
        {
            if (sonuc.TryGetValue(id, out var hesaplanan)) return hesaplanan;
            if (!_dugumler.TryGetValue(id, out var dugum) || !ziyaret.Add(id)) return YetkiTipi.N;
            var ust = dugum.UstId is int ustId ? Hesapla(ustId) : YetkiTipi.W;
            ziyaret.Remove(id);
            var yerel = yerelSeviye(id);
            return sonuc[id] = yerel < ust ? yerel : ust;
        }

        foreach (var id in _dugumler.Keys) Hesapla(id);
        return sonuc;
    }
}

/// <summary>Bir kullanıcının istek boyunca geçerli, kod bazlı etkin yetkileri.</summary>
public sealed class KullaniciYetkiSeti
{
    public static readonly KullaniciYetkiSeti Bos = new(new Dictionary<string, YetkiTipi>());

    private readonly IReadOnlyDictionary<string, YetkiTipi> _seviyeler;

    public KullaniciYetkiSeti(IReadOnlyDictionary<string, YetkiTipi> seviyeler)
    {
        _seviyeler = seviyeler;
    }

    public YetkiTipi Seviye(string kod) => _seviyeler.GetValueOrDefault(kod, YetkiTipi.N);

    /// <summary>Tanımsız kod hiçbir izin vermez.</summary>
    public bool Saglar(string kod, YetkiTipi gereken) =>
        !string.IsNullOrWhiteSpace(kod) && gereken is YetkiTipi.R or YetkiTipi.W && Seviye(kod) >= gereken;
}
