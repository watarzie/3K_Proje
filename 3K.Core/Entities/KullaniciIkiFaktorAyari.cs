namespace _3K.Core.Entities
{
    /// <summary>
    /// Kullanıcının TOTP authenticator kurulumunu tutar.
    /// Gizli anahtar uygulama katmanında şifrelenmeden bu entity'ye yazılmamalıdır.
    /// </summary>
    public class KullaniciIkiFaktorAyari
    {
        public int KullaniciId { get; set; }
        public string SifreliGizliAnahtar { get; set; } = string.Empty;
        public bool EtkinMi { get; set; }
        public DateTime? DogrulandiTarihiUtc { get; set; }
        public long? SonKullanilanTotpAdimi { get; set; }

        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
