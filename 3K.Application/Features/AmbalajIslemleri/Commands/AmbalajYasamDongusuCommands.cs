using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Constants;

namespace _3K.Application.Features.AmbalajIslemleri.Commands;

public sealed class AmbalajFormOlusturCommand : IRequest<Result<AmbalajFormSurumuDto>>, ISecuredRequest, IRequiresMenuPermissions
{
    public List<int> KayitIdleri { get; set; } = [];
    public int? ProjeId { get; set; }
    public List<int> KaynakSandikIdleri { get; set; } = [];
    public Guid IdempotencyAnahtari { get; set; }
    public bool YenidenOlustur { get; set; }
    public string? Aciklama { get; set; }
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [MenuPermissionRequirement.Write(YenidenOlustur ? YetkiKodlari.Ambalaj.FormYenidenOlustur : YetkiKodlari.Ambalaj.FormOlustur)];
}

public sealed class AmbalajGerceklesmeDuzeltCommand : IRequest<Result<AmbalajGerceklesmeDto>>, ISecuredRequest, IRequiresMenuPermissions
{
    public int Id { get; set; }
    public int BeklenenSurum { get; set; }
    public DateTime Tarih { get; set; }
    public int Adet { get; set; }
    public decimal? NetM3 { get; set; }
    public decimal? SarfM3 { get; set; }
    public string Gerekce { get; set; } = string.Empty;
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.GerceklesmeDuzelt)];
}
