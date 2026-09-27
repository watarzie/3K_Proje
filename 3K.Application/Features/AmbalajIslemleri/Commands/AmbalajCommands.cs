using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Enums;
using _3K.Core.Constants;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public sealed class AmbalajUretimDurumuGuncelleCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.DurumDuzenle;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.DurumDuzenle)];
        public int Id { get; set; }
        public AmbalajUretimDurumu Durum { get; set; }
        public string? Aciklama { get; set; }
    }
}
