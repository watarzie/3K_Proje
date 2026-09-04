namespace _3K.Core.Enums;

/// <summary>
/// Kaynak sandik degisikliklerinin kalici senkronizasyon kuyrugundaki durumudur.
/// Kuyruk proje bazinda birlestirilir; ayni projeye ait birden fazla degisiklik
/// ayri ayri is olusturmaz.
/// </summary>
public enum AmbalajKaynakSenkronizasyonKuyrukDurumu : short
{
    Bekliyor = 0,
    Isleniyor = 1,
    Tamamlandi = 2,
    HataKuyrugunda = 3
}
