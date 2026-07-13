namespace _3K.Application.Features.UcKIslemleri.Commands
{
    public class UcKSandikSecimDto
    {
        public int CekiSatiriId { get; set; }
        public int? SandikIcerikId { get; set; }
    }

    internal static class UcKSandikSecimHelper
    {
        public static List<UcKSandikSecimDto> Olustur(
            IReadOnlyCollection<int>? cekiSatiriIdler,
            IReadOnlyCollection<UcKSandikSecimDto>? secimler)
        {
            var sonuc = secimler?.Where(s => s.CekiSatiriId > 0).ToList()
                ?? new List<UcKSandikSecimDto>();

            if (!sonuc.Any())
            {
                sonuc = cekiSatiriIdler?
                    .Where(id => id > 0)
                    .Select(id => new UcKSandikSecimDto { CekiSatiriId = id })
                    .ToList()
                    ?? new List<UcKSandikSecimDto>();
            }

            return sonuc
                .GroupBy(s => new { s.CekiSatiriId, s.SandikIcerikId })
                .Select(g => g.First())
                .ToList();
        }
    }
}
