using MediatR;
using _3K.Application.DTOs;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    /// <summary>
    /// İş akışı 6: Stoktan eksik karşılama
    /// UML Sequence Diagram: EksikService → StokService → HareketService
    /// </summary>
    public class StokKarsilaCommand : IRequest<bool>
    {
        public int CekiSatiriId { get; set; }
        public int StokKaydiId { get; set; }
        public int Miktar { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
    }
}
