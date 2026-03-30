using MediatR;
using _3K.Application.DTOs;

using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 3: Ürün güncelleme (konulan adet, eksik, paketleyen, kontrol, açıklama, grid/3k durumları)
    /// </summary>
    public class UrunGuncelleCommand : IRequest<bool>
    {
        public int CekiSatiriId { get; set; }
        public int SandikId { get; set; }
        public int? KonulanAdet { get; set; }
        public int? EksikAdet { get; set; }
        public GridDurum? GridDurumu { get; set; }
        public UcKDurum? UcKDurumu { get; set; }
        public int? PaketleyenId { get; set; }
        public int? KontrolEdenId { get; set; }
        public string? Aciklama { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
    }
}
