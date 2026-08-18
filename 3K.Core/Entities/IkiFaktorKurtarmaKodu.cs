namespace _3K.Core.Entities
{
    /// <summary>
    /// Kullanıcının tek kullanımlık iki faktörlü doğrulama kurtarma kodudur.
    /// Kodun kendisi değil yalnızca güvenli özeti saklanmalıdır.
    /// </summary>
    public class IkiFaktorKurtarmaKodu
    {
        public int Id { get; set; }
        public int KullaniciId { get; set; }
        public string KodHash { get; set; } = string.Empty;
        public DateTime? KullanildiTarihiUtc { get; set; }

        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
