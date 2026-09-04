using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public interface IAmbalajKayitAlanlari
    {
        int? ProjeId { get; }
        string? ManuelProjeNo { get; }
        string? ManuelProjeAdi { get; }
        int? UstKayitId { get; }
        AmbalajSandikTuru Tur { get; }
        string SandikNo { get; }
        string? Ad { get; }
        AmbalajSandikCinsi SandikCinsi { get; }
        string? DigerSandikCinsi { get; }
        int Adet { get; }
        decimal Boy { get; }
        decimal En { get; }
        decimal Yukseklik { get; }
        string? KullanimAmaci { get; }
        string? TalepEdenKisi { get; }
        string? TalepEdenBolum { get; }
        string? TalimatVeren { get; }
        string? FirinPartiNo { get; }
        string? Aciklama { get; }
    }

    public sealed class AmbalajUretimKaydiOlusturCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions, IAmbalajKayitAlanlari
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.KayitDuzenle;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions
        {
            get
            {
                var requirements = new List<MenuPermissionRequirement>
                {
                    AmbalajMenuKodlari.Write(AmbalajMenuKodlari.KayitDuzenle)
                };
                var turKodu = AmbalajMenuKodlari.TurOlusturmaKodu(Tur);
                if (turKodu != null)
                    requirements.Add(AmbalajMenuKodlari.Write(turKodu));
                if (!ProjeId.HasValue)
                    requirements.Add(AmbalajMenuKodlari.Write(AmbalajMenuKodlari.ManuelProje));
                if (!AmbalajaDahil)
                    requirements.Add(AmbalajMenuKodlari.Write(AmbalajMenuKodlari.HaricTut));
                if (UretimeAlindi)
                    requirements.Add(AmbalajMenuKodlari.Write(AmbalajMenuKodlari.UretimeAl));
                return requirements;
            }
        }
        public int? ProjeId { get; set; }
        public string? ManuelProjeNo { get; set; }
        public string? ManuelProjeAdi { get; set; }
        public int? UstKayitId { get; set; }
        public AmbalajSandikTuru Tur { get; set; } = AmbalajSandikTuru.Normal;
        public AmbalajKaynakModulu KaynakModul { get; set; } = AmbalajKaynakModulu.Manuel;
        public string SandikNo { get; set; } = string.Empty;
        public string? Ad { get; set; }
        public AmbalajSandikCinsi SandikCinsi { get; set; } = AmbalajSandikCinsi.AhsapKapali;
        public string? DigerSandikCinsi { get; set; }
        public int Adet { get; set; } = 1;
        public decimal Boy { get; set; }
        public decimal En { get; set; }
        public decimal Yukseklik { get; set; }
        public bool AmbalajaDahil { get; set; } = true;
        public bool UretimeAlindi { get; set; }
        public string? KullanimAmaci { get; set; }
        public string? TalepEdenKisi { get; set; }
        public string? TalepEdenBolum { get; set; }
        public string? TalimatVeren { get; set; }
        public string? FirinPartiNo { get; set; }
        public string? Aciklama { get; set; }
        public DateTime? UretimTarihi { get; set; }
    }

    public sealed class AmbalajUretimKaydiGuncelleCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions, IAmbalajKayitAlanlari
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.KayitDuzenle;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Write(AmbalajMenuKodlari.KayitDuzenle)];
        public int Id { get; set; }
        public int? ProjeId { get; set; }
        public string? ManuelProjeNo { get; set; }
        public string? ManuelProjeAdi { get; set; }
        public int? UstKayitId { get; set; }
        public AmbalajSandikTuru Tur { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string? Ad { get; set; }
        public AmbalajSandikCinsi SandikCinsi { get; set; }
        public string? DigerSandikCinsi { get; set; }
        public int Adet { get; set; }
        public decimal Boy { get; set; }
        public decimal En { get; set; }
        public decimal Yukseklik { get; set; }
        public string? KullanimAmaci { get; set; }
        public string? TalepEdenKisi { get; set; }
        public string? TalepEdenBolum { get; set; }
        public string? TalimatVeren { get; set; }
        public string? FirinPartiNo { get; set; }
        public string? Aciklama { get; set; }
        public DateTime? UretimTarihi { get; set; }
    }

    public sealed class AmbalajUretimSecimGuncelleCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        // Gerçek geçiş (dahil/haric, üretime al/çıkar) mevcut kayıt okunduktan
        // sonra handler içinde ayrıca doğrulanır.
        public string RequiredMenuKod => AmbalajMenuKodlari.Listele;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Read(AmbalajMenuKodlari.Listele)];
        public int Id { get; set; }
        public bool AmbalajaDahil { get; set; }
        public bool UretimeAlindi { get; set; }
        public string? Aciklama { get; set; }
    }

    public sealed class AmbalajUretimDurumuGuncelleCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.DurumDuzenle;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Write(AmbalajMenuKodlari.DurumDuzenle)];
        public int Id { get; set; }
        public AmbalajUretimDurumu Durum { get; set; }
        public DateTime? UretimTarihi { get; set; }
        public string? FirinPartiNo { get; set; }
        public string? Aciklama { get; set; }
    }

    public sealed class AmbalajM3OverrideGuncelleCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.M3Duzenle;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Write(AmbalajMenuKodlari.M3Duzenle)];
        public int Id { get; set; }
        /// <summary>Null gönderildiğinde manuel override kaldırılır ve hesaplanan m³ kullanılır.</summary>
        public decimal? M3Override { get; set; }
        public string Neden { get; set; } = string.Empty;
    }

    public sealed class AmbalajSarfOraniGuncelleCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.SarfDuzenle;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Write(AmbalajMenuKodlari.SarfDuzenle)];
        public int Id { get; set; }
        /// <summary>Örn. %11 için 0.11.</summary>
        public decimal SarfOrani { get; set; }
        public string Neden { get; set; } = string.Empty;
    }

    public sealed class AmbalajUretimKaydiIptalEtCommand
        : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.Iptal;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Write(AmbalajMenuKodlari.Iptal)];
        public int Id { get; set; }
        public string Neden { get; set; } = string.Empty;
    }

    public sealed class AmbalajUretimKaydiAktiflestirCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.GeriYukle;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Write(AmbalajMenuKodlari.GeriYukle)];
        public int Id { get; set; }
        public string? Aciklama { get; set; }
    }

    public sealed class AmbalajKaynaklariSenkronizeEtCommand
        : IRequest<Result<AmbalajSenkronizasyonSonucuDto>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions
    {
        public string RequiredMenuKod => AmbalajMenuKodlari.KaynakSenkronizeEt;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [AmbalajMenuKodlari.Write(AmbalajMenuKodlari.KaynakSenkronizeEt)];
        public int ProjeId { get; set; }
    }

    public sealed record AmbalajSenkronizasyonSonucuDto(
        int Eklenen,
        int Guncellenen,
        int Degismeyen,
        int EksikOlculu,
        IReadOnlyList<AmbalajUretimKaydiDto> Kayitlar);
}
