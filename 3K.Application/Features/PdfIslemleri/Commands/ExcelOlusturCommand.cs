using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.PdfIslemleri.Commands
{
    /// <summary>
    /// İş akışı 9: Excel şablonunu operasyon verileriyle doldurarak indir.
    /// </summary>
    public class ExcelOlusturCommand : IRequest<Result<byte[]>>, ISecuredRequest
    {

        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
    }
}
