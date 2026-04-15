namespace _3K.Application.Features.LookupIslemleri.DTOs
{
    /// <summary>
    /// Tüm Lookup tablolarının frontend'e döndüğü ortak DTO.
    /// </summary>
    public class LookupItemDto
    {
        public int Id { get; set; }
        public int Anahtar { get; set; }
        public string Deger { get; set; } = string.Empty;
    }
}
