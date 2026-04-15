namespace _3K.Application.Features.AuthIslemleri.DTOs
{
    public class LoginResultDto
    {
        public string Token { get; set; } = string.Empty;
        public KullaniciDto Kullanici { get; set; } = null!;
    }
}
