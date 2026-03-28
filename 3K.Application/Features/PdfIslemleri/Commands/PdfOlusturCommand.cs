using MediatR;

namespace _3K.Application.Features.PdfIslemleri.Commands
{
    /// <summary>
    /// İş akışı 9: PDF oluşturma
    /// </summary>
    public class PdfOlusturCommand : IRequest<byte[]>
    {
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
    }
}
