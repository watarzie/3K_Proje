namespace _3K.Application.Features.AuthIslemleri.DTOs
{
    public static class LoginNextSteps
    {
        public const string Authenticated = "authenticated";
        public const string TwoFactorRequired = "twoFactorRequired";
        public const string TwoFactorSetupRequired = "twoFactorSetupRequired";
    }

    public class LoginResultDto
    {
        public string NextStep { get; set; } = LoginNextSteps.Authenticated;
        public string? Token { get; set; }
        public KullaniciDto? Kullanici { get; set; }
        public string? ChallengeToken { get; set; }
        public int? ExpiresInSeconds { get; set; }

        /// <summary>
        /// Yalnızca ilk TOTP kurulumu başarıyla tamamlandığında bir kez döner.
        /// </summary>
        public IReadOnlyList<string>? KurtarmaKodlari { get; set; }
    }
}
