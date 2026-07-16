namespace _3K.Application.Features.ProjeIslemleri.DTOs
{
    public sealed class SahaSandikAktarimlariGeriAlResultDto
    {
        public int SahaSandikId { get; init; }
        public string SahaSandikNo { get; init; } = string.Empty;
        public int GeriAlinanSatirSayisi { get; init; }
        public decimal GeriAlinanToplamMiktar { get; init; }
        public bool SandikBosaldiMi { get; init; }
        public int SandikDurumId { get; init; }
        public string SandikDurumu { get; init; } = string.Empty;
    }
}
