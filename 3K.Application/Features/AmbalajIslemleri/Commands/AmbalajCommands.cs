using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Enums;
using _3K.Core.Constants;

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
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.KayitEkle;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions
        {
            get
            {
                var requirements = new List<MenuPermissionRequirement>
                {
                    MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.KayitEkle)
                };
                var turKodu = AmbalajYetkilendirmeYardimcisi.TurOlusturmaKodu(Tur);
                if (turKodu != null)
                    requirements.Add(MenuPermissionRequirement.Write(turKodu));
                if (!ProjeId.HasValue)
                    requirements.Add(MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.ManuelProje));
                if (!AmbalajaDahil)
                    requirements.Add(MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.HaricTut));
                if (UretimeAlindi)
                    requirements.Add(MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.UretimeAl));
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
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.KayitDuzenle;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.KayitDuzenle)];
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
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.Listele;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Read(YetkiKodlari.Ambalaj.Listele)];
        public int Id { get; set; }
        public bool AmbalajaDahil { get; set; }
        public bool UretimeAlindi { get; set; }
        public string? Aciklama { get; set; }
    }

    public sealed class AmbalajUretimDurumuGuncelleCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.DurumDuzenle;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.DurumDuzenle)];
        public int Id { get; set; }
        public AmbalajUretimDurumu Durum { get; set; }
        public DateTime? UretimTarihi { get; set; }
        public string? FirinPartiNo { get; set; }
        public string? Aciklama { get; set; }
    }

    public sealed class AmbalajM3OverrideGuncelleCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.M3Duzenle;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.M3Duzenle)];
        public int Id { get; set; }
        /// <summary>Null gönderildiğinde manuel override kaldırılır ve hesaplanan m³ kullanılır.</summary>
        public decimal? M3Override { get; set; }
        public string Neden { get; set; } = string.Empty;
    }

    public sealed class AmbalajSarfOraniGuncelleCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.SarfDuzenle;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.SarfDuzenle)];
        public int Id { get; set; }
        /// <summary>Örn. %11 için 0.11.</summary>
        public decimal SarfOrani { get; set; }
        public string Neden { get; set; } = string.Empty;
    }

    public sealed class AmbalajUretimKaydiIptalEtCommand
        : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.Iptal;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.Iptal)];
        public int Id { get; set; }
        public string Neden { get; set; } = string.Empty;
    }

    public sealed class AmbalajUretimKaydiAktiflestirCommand
        : IRequest<Result<AmbalajUretimKaydiDto>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.GeriYukle;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.GeriYukle)];
        public int Id { get; set; }
        public string? Aciklama { get; set; }
    }

    public sealed class AmbalajKaynaklariSenkronizeEtCommand
        : IRequest<Result<AmbalajSenkronizasyonSonucuDto>>, ISecuredRequest, IRequiresMenuPermission,
          IRequiresMenuPermissions
    {
        public string RequiredMenuKod => YetkiKodlari.Ambalaj.KaynakSenkronizeEt;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Write(YetkiKodlari.Ambalaj.KaynakSenkronizeEt)];
        public int ProjeId { get; set; }
    }

    public sealed record AmbalajSenkronizasyonSonucuDto(
        int Eklenen,
        int Guncellenen,
        int Degismeyen,
        int EksikOlculu,
        IReadOnlyList<AmbalajUretimKaydiDto> Kayitlar);
}
