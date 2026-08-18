using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Parola doğrulaması ile tam oturum arasında kullanılan, kısa ömürlü ve
    /// tek kullanımlık iki faktörlü doğrulama talebidir.
    /// </summary>
    public class IkiFaktorGirisTalebi
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TokenHash { get; set; } = string.Empty;
        public int KullaniciId { get; set; }
        public IkiFaktorTalepAmaci Amac { get; set; }
        public DateTime SonKullanmaTarihiUtc { get; set; }
        public DateTime? TuketildiTarihiUtc { get; set; }
        public int BasarisizDenemeSayisi { get; set; }
        public bool BeniHatirla { get; set; }

        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
