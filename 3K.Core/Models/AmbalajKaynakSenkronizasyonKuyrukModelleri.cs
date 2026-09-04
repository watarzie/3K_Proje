namespace _3K.Core.Models;

/// <summary>
/// Bir worker tarafindan sureli olarak sahiplenilmis senkronizasyon isi.
/// Surum ve KilitKimligi, gecikmis bir worker'in daha yeni isi kapatmasini engeller.
/// </summary>
public sealed record AmbalajKaynakSenkronizasyonKuyrukIsi(
    int ProjeId,
    long Surum,
    Guid KilitKimligi,
    int DenemeSayisi);

public enum AmbalajKaynakSenkronizasyonSonlandirmaDurumu
{
    Tamamlandi = 1,
    YenidenKuyrugaAlindi = 2,
    HataKuyrugunaAlindi = 3,
    SahiplikKaybedildi = 4
}

public sealed record AmbalajKaynakSenkronizasyonSonlandirmaSonucu(
    AmbalajKaynakSenkronizasyonSonlandirmaDurumu Durum,
    int DenemeSayisi);

public sealed record AmbalajKaynakSenkronizasyonKuyrukIstatistigi(
    int Bekleyen,
    int Isleniyor,
    int Tamamlanan,
    int HataKuyrugunda,
    int YenidenDenenecek,
    DateTime? EnEskiBekleyenTalepTarihiUtc,
    DateTime? SonBasariliTarihUtc);
