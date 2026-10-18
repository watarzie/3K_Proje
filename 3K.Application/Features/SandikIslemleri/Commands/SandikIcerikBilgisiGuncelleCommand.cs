using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Sandıktaki ürün satırının kalıcı "Ürün bilgisi" alanını (seri no, SKT, lot) günceller. Alan Grid/3K
    /// işlemlerinden bağımsızdır; boş değer bilgiyi siler. Grid ve 3K ekranları aynı komutu kullanır.
    /// </summary>
    public class SandikIcerikBilgisiGuncelleCommand : IRequest<Result>, ISecuredRequest
    {
        public int ProjeId { get; set; }
        public int SandikId { get; set; }
        public int SandikIcerikId { get; set; }
        public string? UrunBilgisi { get; set; }

        /// <summary>
        /// Kullanıcının ekranda düzenlemeye başladığı değer (boşsa null). Kayıtlı değer bundan farklıysa başka biri
        /// arada değiştirmiştir; istek 409 ile reddedilir ve kayıtlı bilgi ezilmez.
        /// </summary>
        public string? OncekiUrunBilgisi { get; set; }
    }
}
