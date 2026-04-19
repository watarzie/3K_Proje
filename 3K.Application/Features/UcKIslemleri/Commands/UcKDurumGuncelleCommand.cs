using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// 3K personeli ürün karşılama durumunu günceller.
    /// Karşılama Tipleri: TamGeldi, EksikGeldi, Gelmedi, ProjedenKarsilandi, StoktanKarsilandi,
    /// TedarikcidenGeldi, BaskaProyeVerildi, GeriGonderildi, HataliUrun
    /// </summary>
    public class UcKDurumGuncelleCommand : IRequest<Result>, ISecuredRequest, IRequireApproval
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public string GetApprovalKarsilamaTipi()
        {
            return KarsilamaTipi ?? "";
        }

        public string GetApprovalDescription()
        {
            var asGelenAdet = GelenAdet.HasValue ? $"{GelenAdet} adet" : "tümü";
            var m_proje = !string.IsNullOrEmpty(MevcutProjeNo) ? MevcutProjeNo : $"ID-{ProjeId}";
            var m_sandik = !string.IsNullOrEmpty(MevcutSandikNo) ? $"{MevcutSandikNo} numaralı sandıktaki" : "";
            var m_urun = !string.IsNullOrEmpty(UrunAdi) ? UrunAdi : $"Ürün(ID:{CekiSatiriId})";

            if (KarsilamaTipi == StatusConstants.UcKDurum.ProjedenKarsilandi)
            {
                var k_urun = !string.IsNullOrEmpty(KaynakUrunAdi) ? KaynakUrunAdi : "ürünü";
                return $"{m_proje} projesi {m_sandik} {m_urun} ürünü için, {KaynakHedefProjeNo} projesinden {k_urun} {asGelenAdet} çekilecek.";
            }
            
            if (KarsilamaTipi == StatusConstants.UcKDurum.StoktanKarsilandi)
                return $"{m_proje} projesi {m_sandik} {m_urun} ürünü için, stok deposundan (Kayıt No: {StokKaydiId}) {asGelenAdet} kullanılacak.";

            if (KarsilamaTipi == StatusConstants.UcKDurum.TedarikcidenGeldi)
                return $"{m_proje} projesi {m_sandik} {m_urun} ürünü için, harici tedarikçiden {asGelenAdet} teslim alınacak.";

            return $"3K Kritik Durum Güncellemesi: {m_proje} / {m_urun} -> {KarsilamaTipi}";
        }

        public string? UrunAdi { get; set; }
        public string? MevcutProjeNo { get; set; }
        public string? MevcutSandikNo { get; set; }
        public string? KaynakUrunAdi { get; set; }

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

        /// <summary>
        /// StoktanKarsilandi durumunda zorunlu: Kullanılan stoğun ID'si.
        /// </summary>
        public int? StokKaydiId { get; set; }
    }
}
