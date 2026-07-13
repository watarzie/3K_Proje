using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Sandık Bölme/Taşıma: Bir sandıktaki ürünlerin bir kısmını başka bir sandığa taşır.
    /// Örn: 2 nolu sandıktaki 4 ürünün 2'si, 67 nolu sandığa aktarılır.
    /// DB: Kaynak SandikIcerik.KonulanAdet UPDATE (düşülür), Hedef sandıkta INSERT/UPDATE.
    /// </summary>
    public class SandikUrunTasiCommand : IRequest<Result>, ISecuredRequest
    {

        public int KaynakSandikIcerikId { get; set; }
        public int HedefSandikId { get; set; }
        public decimal TasinanAdet { get; set; }
        public int ProjeId { get; set; }

        /// <summary>
        /// Ağ/istemci tekrarlarında aynı taşımanın ikinci kez uygulanmasını engelleyen zorunlu anahtar.
        /// İstemci her kullanıcı işlemi için yeni bir değer üretmeli, tekrar denemelerde aynı değeri kullanmalıdır.
        /// </summary>
        public Guid IslemAnahtari { get; set; }
    }
}
