using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Grid personeli ürün durumunu günceller.
    /// Yeni durum seçenekleri: TamGeldi, EksikGeldi, Gelmedi, TrafoSevk, Iptal, Sipariste
    /// </summary>
    public class GridDurumGuncelleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.PersonelGrid };

        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public int YeniDurumId { get; set; }

        /// <summary>
        /// EksikGeldi durumunda zorunlu. TamGeldi'de otomatik miktar kadar.
        /// </summary>
        public int? GridGelenAdet { get; set; }

        /// <summary>
        /// Sadece TrafoSevk durumunda zorunlu.
        /// </summary>
        public int? TrafoSevkAdet { get; set; }

        /// <summary>
        /// Grid Sevk Durumu: SevkEdildi, Bekliyor, SevkEdilmedi
        /// </summary>
        public int? GridSevkDurumuId { get; set; }

        /// <summary>
        /// Grid'den 3K'ya sevk edilen adet.
        /// </summary>
        public int? SevkMiktari { get; set; }

        public string? Aciklama { get; set; }
    }
}
