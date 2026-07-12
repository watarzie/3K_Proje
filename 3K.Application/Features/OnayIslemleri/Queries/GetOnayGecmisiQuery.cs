using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.OnayIslemleri.DTOs;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public sealed class GetOnayGecmisiQuery : IRequest<Result<OnayGecmisiListeDto>>
    {
        public string Kapsam { get; set; } = "tumu";
        public string Durum { get; set; } = "tumu";
        public string CalistirmaDurumu { get; set; } = "tumu";
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public string? Arama { get; set; }
        public int Sayfa { get; set; } = 1;
        public int SayfaBoyutu { get; set; } = 20;
    }

    public sealed class GetOnayGecmisiDetayiQuery : IRequest<Result<OnayGecmisiKayitDto>>
    {
        public int Id { get; set; }
    }
}
