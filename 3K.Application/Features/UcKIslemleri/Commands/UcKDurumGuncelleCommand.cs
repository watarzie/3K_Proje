using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// 3K personeli ürün karşılama durumunu günceller.
    /// Karşılama Tipleri: TamGeldi, EksikGeldi, Gelmedi, ProjedenKarsilandi, StoktanKarsilandi,
    /// TedarikcidenGeldi, BaskaProyeVerildi, GeriGonderildi, HataliUrun
    /// </summary>
    public class UcKDurumGuncelleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public string KarsilamaTipi { get; set; } = string.Empty;

        /// <summary>
        /// TamGeldi hariç, kullanıcının girdiği adet.
        /// </summary>
        public int? GelenAdet { get; set; }

        /// <summary>
        /// ProjedenKarsilandi / BaskaProyeVerildi için referans proje no.
        /// </summary>
        public string? KaynakHedefProjeNo { get; set; }

        /// <summary>
        /// ProjedenKarsilandi için referans ürünün CekiSatiriId'si.
        /// </summary>
        public int? KaynakCekiSatiriId { get; set; }

        /// <summary>
        /// Açıklama (zorunluluk karşılama tipine göre değişir).
        /// </summary>
        public string? Aciklama { get; set; }

        /// <summary>
        /// GeriGonderildi durumunda zorunlu: "Tadilat" veya "Iptal"
        /// </summary>
        public string? GeriGonderilmeSebebi { get; set; }

        public string? Not { get; set; }
    }
}
