using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    /// <summary>Tek bir özel sandık türünün (ilave, iç, saha, yedek) aktif kayıtlarından PDF üretim raporu.</summary>
    public sealed class GetAmbalajRaporDosyasiQuery
        : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermissions
    {
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.RaporGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.PdfIndir)
        ];
        public AmbalajSandikTuru Tur { get; init; }
    }

    /// <summary>Projenin kaydedilmiş en güncel üretim formunu PDF olarak döndürür; formu yeniden hesaplamaz.</summary>
    public sealed class GetAmbalajUretimFormuDosyasiQuery
        : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermissions
    {
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.FormGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.FormIndir),
            MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.PdfIndir)
        ];
        public int ProjeId { get; init; }
        /// <summary>Boşsa formdaki bütün türler alınır.</summary>
        public AmbalajSandikTuru? Tur { get; init; }
        public bool BagimsizKayitMi { get; init; }
    }
}
