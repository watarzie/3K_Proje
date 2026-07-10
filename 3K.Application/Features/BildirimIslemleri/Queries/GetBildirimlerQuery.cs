using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.DTOs;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Queries
{
    public class GetBildirimlerQuery : IRequest<Result<BildirimListeSonucDto>>
    {
        public string Durum { get; set; } = "tumu";
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public int? TipId { get; set; }
        public string? Arama { get; set; }
        public int Sayfa { get; set; } = 1;
        public int SayfaBoyutu { get; set; } = 20;
    }
}
