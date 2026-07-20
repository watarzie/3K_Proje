using MediatR;
using _3K.Application.Common;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    /// <summary>
    /// Revizyon dosyasını değişmez bir artifact ve güvenli ön izleme olarak
    /// hazırlar. Operasyon kuralına göre yetkili onayına sunar veya aynı
    /// doğrulama akışı üzerinden doğrudan uygular.
    /// </summary>
    public sealed class CekiRevizyonYukleCommand : IRequest<Result<CekiRevizyonOnayTalebiSonuc>>, ISecuredRequest, IRequiresMenuPermission
    {
        public Stream ExcelDosya { get; set; } = null!;
        public string DosyaAdi { get; set; } = string.Empty;
        public int KullaniciId { get; set; }

        public string RequiredMenuKod => "ceki-revizyon-yukle";
    }
}
