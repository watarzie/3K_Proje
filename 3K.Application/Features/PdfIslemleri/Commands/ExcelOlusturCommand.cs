using MediatR;

namespace _3K.Application.Features.PdfIslemleri.Commands
{
    /// <summary>
    /// İş akışı 9: Excel şablonunu operasyon verileriyle doldurarak indir
    /// "Excel neyse PDF odur" — orijinal şablon düzeni korunur
    /// </summary>
    public class ExcelOlusturCommand : IRequest<byte[]>
    {
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
    }
}
