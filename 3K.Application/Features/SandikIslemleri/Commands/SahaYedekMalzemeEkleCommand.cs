using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Saha/Yedek projelerinde, çeki olmadan doğrudan sandık içerisine
    /// malzeme ekler. SandikIcerik tablosuna CekiSatiriId=null kayıt oluşturur.
    /// </summary>
    public class SahaYedekMalzemeEkleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };
        public string? RequiredMenuKod => "sandik-yonetimi";

        public int ProjeId { get; set; }
        public int SandikId { get; set; }
        public string? BarkodNo { get; set; }
        public string Isim { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public int? BirimId { get; set; }
        /// <summary>Projeden seçildi ise kaynak çeki satırı ID'si (opsiyonel)</summary>
        public int? CekiSatiriId { get; set; }
        /// <summary>Projeden seçildi ise kaynak proje numarası (opsiyonel)</summary>
        public string? KaynakProjeNo { get; set; }
        /// <summary>Malzeme için ekstra not/açıklama</summary>
        public string? Aciklama { get; set; }
    }
}

