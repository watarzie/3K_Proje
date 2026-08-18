using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.KullaniciIslemleri.DTOs;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public sealed class KullaniciIkiFaktorZorunluluguGuncelleCommand
        : IRequest<Result<KullaniciIkiFaktorDurumDto>>,
          ISecuredRequest,
          IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
        public int KullaniciId { get; set; }
        public bool? ZorunluMu { get; set; }
    }
}
