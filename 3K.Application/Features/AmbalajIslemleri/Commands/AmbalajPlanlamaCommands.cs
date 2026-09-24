using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;

namespace _3K.Application.Features.AmbalajIslemleri.Commands;

public abstract class AmbalajPlanlamaCommand<T> : IRequest<Result<T>>, ISecuredRequest,
    IRequiresMenuPermissions
{
    public string? Gerekce { get; set; }
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [AmbalajMenuKodlari.Write(this switch
        {
            AmbalajPlanKaydetCommand => AmbalajMenuKodlari.PlanOlustur,
            AmbalajKarariKaydetCommand c => c.AmbalajaDahilMi ? AmbalajMenuKodlari.DahilEt : AmbalajMenuKodlari.HaricTut,
            AmbalajPlanKalemKaydetCommand c => c.KalemId.HasValue ? AmbalajMenuKodlari.KayitDuzenle : AmbalajMenuKodlari.KayitEkle,
            AmbalajBagimsizSandikKaydetCommand c => c.SandikId.HasValue ? AmbalajMenuKodlari.KayitDuzenle : AmbalajMenuKodlari.KayitEkle,
            _ => AmbalajMenuKodlari.KayitDuzenle
        })];
}

public abstract class AmbalajPlanlamaCommand : IRequest<Result>, ISecuredRequest,
    IRequiresMenuPermissions
{
    public string? Gerekce { get; set; }
    public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [AmbalajMenuKodlari.Write(AmbalajMenuKodlari.KayitSil)];
}

public sealed class AmbalajPlanKaydetCommand : AmbalajPlanlamaCommand<AmbalajPlanlamaPlanDto>
{
    public int ProjeId { get; set; }
    public int? KaynakProjeTipiId { get; set; }
    public string? FirinPartiNo { get; init; }
    public IReadOnlyList<int> SeciliKaynakSandikIds { get; init; } = [];
    public int Grup { get; init; } = 1;
    // Eski istemcilerin payload'ları için korunur; plan kaydı artık durum değiştirmez.
    // İlk başlangıç FormOlustur, sonraki geçişler UretimDurumuGuncelle ile yapılır.
    public int DurumId { get; init; } = 1;
}

public sealed class AmbalajKarariKaydetCommand : AmbalajPlanlamaCommand<AmbalajPlanlamaPlanDto>
{
    public int SandikId { get; set; }
    public bool AmbalajaDahilMi { get; init; }
}

public sealed class AmbalajPlanKalemKaydetCommand : AmbalajPlanlamaCommand<AmbalajPlanlamaKalemDto>
{
    public int? KalemId { get; set; }
    public int? ProjeId { get; set; }
    public int Tur { get; init; }
    public int? UstKalemId { get; init; }
    public int? UstKaynakSandikId { get; init; }
    public int? IcSandikSablonId { get; init; }
    public bool UretimeAlindi { get; init; }
    public string SandikNo { get; init; } = string.Empty;
    public string? Ad { get; init; }
    public string SandikTipi { get; init; } = "Ahşap Kapalı";
    public int Adet { get; init; } = 1;
    public decimal Boy { get; init; }
    public decimal En { get; init; }
    public decimal Yukseklik { get; init; }
    public string? KullanimAmaci { get; init; }
    public string? TalimatVeren { get; init; }
    public string? Aciklama { get; init; }
}

public sealed class AmbalajPlanKalemSilCommand : AmbalajPlanlamaCommand
{
    public int KalemId { get; init; }
}

public sealed class AmbalajIcSandikSablonuEkleCommand
    : AmbalajPlanlamaCommand<AmbalajIcSandikSablonDto>
{
    public string Ad { get; init; } = string.Empty;
    public string SandikTipi { get; init; } = "Ahşap Kapalı";
    public decimal Boy { get; init; }
    public decimal En { get; init; }
    public decimal Yukseklik { get; init; }
}

public sealed class AmbalajIcSandikSablonuSilCommand : AmbalajPlanlamaCommand
{
    public int SablonId { get; init; }
}

public sealed class AmbalajTalepEdenEkleCommand : AmbalajPlanlamaCommand<AmbalajTalepEdenDto>
{
    public string? Ad { get; init; }
}

public sealed class AmbalajBagimsizSandikKaydetCommand
    : AmbalajPlanlamaCommand<AmbalajBagimsizSandikDto>
{
    public int? SandikId { get; set; }
    public int Tur { get; init; }
    public int ProjeId { get; init; }
    public int? KaynakSandikId { get; init; }
    public int? UstKaynakSandikId { get; init; }
    public int? IcSandikSablonId { get; init; }
    public bool UretimeAlindi { get; init; } = true;
    public string SandikNo { get; init; } = string.Empty;
    public string? Ad { get; init; }
    public string SandikTipi { get; init; } = "Ahşap Kapalı";
    public int Adet { get; init; } = 1;
    public decimal Boy { get; init; }
    public decimal En { get; init; }
    public decimal Yukseklik { get; init; }
    public string? TalimatVeren { get; init; }
    public string? Aciklama { get; init; }
}

public sealed class AmbalajBagimsizSandikSilCommand : AmbalajPlanlamaCommand
{
    public int SandikId { get; init; }
}
