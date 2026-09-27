using _3K.Core.Enums;

namespace _3K.Core.Models;

public sealed record KullaniciYetkiKarari(int MenuTanimiId, bool? IzinVerildi);
public sealed record KullaniciYetkiModel(int MenuTanimiId, string Kod, string LabelKey,
    int RolYetkiTipiId, int EtkinYetkiTipiId, bool? IzinVerildi);
public sealed record KullaniciYetkiSonucu(bool Basarili, string? Hata = null, int DurumKodu = 200);

/// <summary>Yetki hesabı için menü ağacındaki bir düğüm.</summary>
public sealed record YetkiDugumu(int Id, string Kod, int? UstId);

/// <summary>
/// Menü ağacı üzerinde etkin yetki hesabının tek uygulaması. Bütün ekran, işlem ve alan
/// kodları aynı kuralla değerlendirilir:
/// 1) Rol seviyesi üst düğümün rol seviyesiyle sınırlanır (üst düğüm tavandır).
/// 2) Kişisel karar varsa uygulanır: açık ret N, açık izin W; yoksa rolden gelen seviye kalır.
/// 3) Sonuç tekrar üst düğümün etkin seviyesiyle sınırlanır.
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

    /// <summary>Rolün atanmış seviyelerini ağaçta sınırlar (kişisel karar uygulanmadan).</summary>
    public IReadOnlyDictionary<int, YetkiTipi> RolSeviyeleri(IReadOnlyDictionary<int, int> atanmisSeviyeler) =>
        Sinirla(id => atanmisSeviyeler.TryGetValue(id, out var seviye) && Enum.IsDefined((YetkiTipi)seviye)
            ? (YetkiTipi)seviye
            : YetkiTipi.N);

    /// <summary>Kullanıcının etkin seviyeleri: rol seviyeleri + kişisel ret/izin kararları.</summary>
    public IReadOnlyDictionary<int, YetkiTipi> KullaniciSeviyeleri(
        IReadOnlyDictionary<int, int> rolAtamalari,
        IReadOnlyDictionary<int, bool>? kisiselKararlar = null)
    {
        var rolSeviyeleri = RolSeviyeleri(rolAtamalari);
        if (kisiselKararlar == null || kisiselKararlar.Count == 0)
            return rolSeviyeleri;

        return Sinirla(id => kisiselKararlar.TryGetValue(id, out var izinVerildi)
            ? izinVerildi ? YetkiTipi.W : YetkiTipi.N
            : rolSeviyeleri.GetValueOrDefault(id, YetkiTipi.N));
    }

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
