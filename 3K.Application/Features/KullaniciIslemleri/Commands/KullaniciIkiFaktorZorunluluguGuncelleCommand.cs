using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.KullaniciIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public sealed class KullaniciIkiFaktorZorunluluguGuncelleCommand
        : IRequest<Result<KullaniciIkiFaktorDurumDto>>,
          ISecuredRequest,
          IRequiresMenuPermission
    {
        public string RequiredMenuKod => "kullanicilar";
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public int KullaniciId { get; set; }
        public bool? ZorunluMu { get; set; }
    }
}
