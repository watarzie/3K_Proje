namespace _3K.Application.Features.AuthIslemleri.DTOs
{
    public sealed class IkiFaktorKurulumDto
    {
        public string ChallengeToken { get; set; } = string.Empty;
        public int ExpiresInSeconds { get; set; }
        public string QrCodeDataUri { get; set; } = string.Empty;
        public string ManuelAnahtar { get; set; } = string.Empty;
    }
}
