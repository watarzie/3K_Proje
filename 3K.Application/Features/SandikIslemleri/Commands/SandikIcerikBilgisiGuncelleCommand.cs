using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Sandıktaki ürün satırının kalıcı "Ürün bilgisi" alanını (seri no, SKT, lot) ve isteğe bağlı olarak ekranın
    /// açıklamasını (Grid ekranında Grid, 3K ekranında 3K açıklaması) birlikte günceller. Alanlar Grid/3K
    /// işlemlerinden bağımsızdır; boş değer bilgiyi/açıklamayı siler. Grid ve 3K ekranları aynı komutu kullanır.
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

        /// <summary>
        /// Düzenlenecek açıklama; boşsa açıklamaya dokunulmaz. Grid açıklaması Grid, 3K açıklaması 3K yazma izni ister.
        /// </summary>
        public UrunAciklamaAlani? AciklamaAlani { get; set; }

        public string? Aciklama { get; set; }

        /// <summary>Açıklamanın ekranda görülen eski değeri; kayıtlı değerden farklıysa 409.</summary>
        public string? OncekiAciklama { get; set; }
    }
}
