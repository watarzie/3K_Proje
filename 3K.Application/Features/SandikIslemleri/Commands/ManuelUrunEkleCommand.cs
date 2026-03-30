using MediatR;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// İş akışı 9: Ekrandan sisteme "manuel" ürün satırı eklenmesi.
    /// </summary>
    public class ManuelUrunEkleCommand : IRequest<bool>
    {
        public int ProjeId { get; set; }
        public int SandikId { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public int IstenenAdet { get; set; }
        public string Birim { get; set; } = "ADET";
        public string? EklemeNedeni { get; set; }
        public int KullaniciId { get; set; }
    }
}
