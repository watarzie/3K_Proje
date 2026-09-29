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

    /// <summary>
    /// Projenin ambalaja dahil bütün aktif sandıklarının üretimini tek işlemde tamamlar (A2: FB listesindeki
    /// "Tamamla") veya tamamlanmayı geri alır (A3: "Üretimi Tamamlanan" sekmesi). Kayda bağlı izin, ilk form
    /// ve finans (PO/fatura) kuralları her sandıkta tek kayıtlı durum değişikliğiyle aynıdır.
    /// </summary>
    public sealed class AmbalajProjeUretimDurumuCommand
        : IRequest<Result<AmbalajProjeUretimDurumuSonucDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.DurumDuzenle;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.DurumDuzenle)];
        public int ProjeId { get; set; }
        /// <summary>true: tamamla; false: tamamlanan sandıkları üretimde durumuna geri al.</summary>
        public bool Tamamla { get; set; } = true;
        public string? Aciklama { get; set; }
    }
}
