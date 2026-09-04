using _3K.Core.Enums;

namespace _3K.Core.Entities;

/// <summary>
/// Sandik kaynaginda degisen bir projeyi kalici ve birlestirilebilir bicimde
/// senkronizasyon icin isaretler. ProjeId bilincli olarak FK degildir: proje
/// silme islemi sonrasi kalan is de guvenle sonlandirilabilmelidir.
/// </summary>
public sealed class AmbalajKaynakSenkronizasyonKuyrukKaydi
{
    public int ProjeId { get; set; }
    public long Surum { get; set; } = 1;
    public AmbalajKaynakSenkronizasyonKuyrukDurumu Durum { get; set; } =
        AmbalajKaynakSenkronizasyonKuyrukDurumu.Bekliyor;

    public DateTime TalepTarihiUtc { get; set; } = DateTime.UtcNow;
    public DateTime UygunTarihUtc { get; set; } = DateTime.UtcNow;
    public int DenemeSayisi { get; set; }

    public Guid? KilitKimligi { get; set; }
    public DateTime? KilitBitisTarihiUtc { get; set; }
    public DateTime? SonDenemeTarihiUtc { get; set; }
    public DateTime? SonBasariliTarihUtc { get; set; }
    public string? SonHata { get; set; }
    public DateTime? HataKuyrugunaAlindiTarihiUtc { get; set; }
}
