using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.UcKIslemleri.Queries
{
    /// <summary>
    /// 3K "Projeden Karşılandı" panelinde kaynak projeden seçilebilecek ürünler.
    /// Hedef projede 3K yazma izniyle açılır: karşılamayı kaydeden <c>UcKDurumGuncelleCommand</c>
    /// da yalnız bu izni ister. Kaynak projenin Grid ekranı izni bu işlemin parçası değildir;
    /// Grid listesini ödünç almak 3K personelinin karşılamasını 403 ile kapatıyordu.
    /// </summary>
    public class GetProjedenKarsilamaKaynaklariQuery
        : IRequest<Result<List<ProjedenKarsilamaKaynakDto>>>, ISecuredRequest, IRequiresProjectMenuPermission
    {
        public int HedefProjeId { get; set; }
        public int KaynakProjeId { get; set; }

        public int PermissionProjectId => HedefProjeId;
        public ProjectMenuOperation PermissionOperation => ProjectMenuOperation.UcK;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
    }
}
