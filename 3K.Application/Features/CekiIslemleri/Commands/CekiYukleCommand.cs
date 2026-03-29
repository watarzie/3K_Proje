using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    /// <summary>
    /// İş akışı 2: Excel çeki dosyasını yükle
    /// </summary>
    public class CekiYukleCommand : IRequest<CekiYuklemeResultDto>
    {
        public Stream ExcelDosya { get; set; } = null!;
        public string DosyaAdi { get; set; } = string.Empty;
        public int KullaniciId { get; set; }
    }
}
