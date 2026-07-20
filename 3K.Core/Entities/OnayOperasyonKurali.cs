namespace _3K.Core.Entities
{
    /// <summary>
    /// Lookup durumlarından bağımsız, operasyon kodu bazında yönetilen onay
    /// zorunluluğunu saklar. Onaycı roller OnayIslemYetki tablosunda kalır.
    /// </summary>
    public sealed class OnayOperasyonKurali : BaseEntity
    {
        public string IslemKodu { get; set; } = string.Empty;
        public bool OnayGerektirirMi { get; set; } = true;
    }
}
