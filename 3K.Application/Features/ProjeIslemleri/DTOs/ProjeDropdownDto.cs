namespace _3K.Application.Features.ProjeIslemleri.DTOs
{
    /// <summary>
    /// Dropdown'lar için hafif proje DTO — Include yok, sadece temel alanlar.
    /// </summary>
    public class ProjeDropdownDto
    {
        public int Id { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
    }
}
