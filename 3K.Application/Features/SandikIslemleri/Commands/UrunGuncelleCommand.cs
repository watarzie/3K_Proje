using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 3: Ürün güncelleme (konulan adet, eksik, paketleyen, kontrol, açıklama)
    /// </summary>
    public class UrunGuncelleCommand : IRequest<bool>
    {
        public int CekiSatiriId { get; set; }
        public int SandikId { get; set; }
        public int? KonulanAdet { get; set; }
        public int? EksikAdet { get; set; }
        public int? PaketleyenId { get; set; }
        public int? KontrolEdenId { get; set; }
        public string? Aciklama { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
    }
}
