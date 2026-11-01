using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Etiketlerin ortak proje alanları; değerler ekranda olduğu gibi (Türkçe karakterli) gösterilir.</summary>
public sealed record BarkodEtiketProjesi(int ProjeId, string ProjeNo, int ProjeTipiId, string SeriNo, string Musteri, string Lokasyon);

/// <summary>Etiket ekranındaki sandık satırı; <see cref="Barkod"/> etikete basılacak 12 haneli sandık barkodudur.</summary>
public sealed record BarkodEtiketSandikSatiri(
    int SandikId,
    string SandikNo,
    string? Ad,
    string? AdIngilizce,
    string Durum,
    string Barkod,
    int UrunSayisi);

/// <summary>
/// Etiket ekranındaki ürün satırı. <see cref="EtiketBarkodu"/> etikete basılacak 16 haneli barkoddur; barkod boşsa,
/// 16 karakterden uzunsa veya Code 128 ile basılamayan karakter içeriyorsa <c>null</c> olur ve satır seçilemez.
/// <see cref="Miktar"/> çeki satırında istenen adet (bütün sandıklar), elle eklenen malzemede kendi miktarıdır;
/// <see cref="SandiktakiMiktar"/> liste sandıkla süzülünce o sandığa tahsis edilen miktardır (süzgeçsiz listede boş).
/// </summary>
public sealed record BarkodEtiketUrunSatiri(
    BarkodEtiketUrunKaynagi Kaynak,
    int Id,
    string BarkodNo,
    string? EtiketBarkodu,
    string Tanim,
    string Aciklama,
    string? Remarks,
    string SandikNolari,
    decimal Miktar,
    decimal? SandiktakiMiktar,
    string Birim);

/// <summary>Basılacak sandık ve kaç etiket basılacağı.</summary>
public sealed record BarkodEtiketSandikSecimi(int SandikId, int Adet);

/// <summary>Basılacak ürün, kaç etiket basılacağı ve etiketteki REMARKS (boşsa "-").</summary>
public sealed record BarkodEtiketUrunSecimi(BarkodEtiketUrunKaynagi Kaynak, int Id, int Adet, string? Remarks);

/// <summary>Basıma hazır sandık etiketi: metinler <see cref="BarkodEtiketKurali.Metin"/> ile ASCII'ye çevrilmiştir.</summary>
public sealed record SandikEtiketi(string Barkod, string SeriNo, string SandikNo, string Musteri, string Lokasyon, string Tarih, int Adet);

/// <summary>Basıma hazır ürün etiketi: metinler <see cref="BarkodEtiketKurali.Metin"/> ile ASCII'ye çevrilmiştir.</summary>
public sealed record UrunEtiketi(
    string Barkod,
    string SeriNo,
    string Musteri,
    string Lokasyon,
    string Tanim,
    string Aciklama,
    string Remarks,
    int Adet);
