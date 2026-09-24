using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Common;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    public sealed class GetAmbalajRaporQueryHandler
        : IRequestHandler<GetAmbalajRaporQuery, Result<AmbalajRaporDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRolService _rolService;
        private readonly ICurrentUserService _currentUserService;

        public GetAmbalajRaporQueryHandler(
            IUnitOfWork unitOfWork,
            IRolService rolService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _rolService = rolService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<AmbalajRaporDto>> Handle(GetAmbalajRaporQuery request, CancellationToken cancellationToken)
        {
            var yetkiler = await AmbalajRaporVerisi.RaporYetkileriAsync(
                _rolService, _currentUserService, cancellationToken);
            if (!yetkiler.KaynakGorunur)
                request.KaynakModul = null;
            var kayitlar = AmbalajRaporVerisi.KayitlariGetir(_unitOfWork, request);
            var dtolar = AmbalajSorguYardimcisi.DtolariOlustur(_unitOfWork, kayitlar).ToList();
            dtolar.ForEach(dto => AmbalajYetkilendirmeYardimcisi.DtoyuMaskele(dto, yetkiler));
            return Result<AmbalajRaporDto>.Success(AmbalajRaporVerisi.OzetDtoOlustur(dtolar, yetkiler));
        }
    }

    public sealed class GetAmbalajRaporDosyasiQueryHandler
        : IRequestHandler<GetAmbalajRaporDosyasiQuery, Result<AmbalajDosyaDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAmbalajRaporDosyaService _dosyaService;
        private readonly IRolService _rolService;
        private readonly ICurrentUserService _currentUserService;

        public GetAmbalajRaporDosyasiQueryHandler(
            IUnitOfWork unitOfWork,
            IAmbalajRaporDosyaService dosyaService,
            IRolService rolService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _dosyaService = dosyaService;
            _rolService = rolService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<AmbalajDosyaDto>> Handle(
            GetAmbalajRaporDosyasiQuery request,
            CancellationToken cancellationToken)
        {
            var yetkiler = await AmbalajRaporVerisi.RaporYetkileriAsync(
                _rolService, _currentUserService, cancellationToken);
            if (!yetkiler.KaynakGorunur)
                request.KaynakModul = null;
            var kayitlar = AmbalajRaporVerisi.KayitlariGetir(_unitOfWork, request);
            var dtolar = AmbalajSorguYardimcisi.DtolariOlustur(_unitOfWork, kayitlar).ToList();
            dtolar.ForEach(dto => AmbalajYetkilendirmeYardimcisi.DtoyuMaskele(dto, yetkiler));
            var satirlar = AmbalajRaporVerisi.RaporSatirlariOlustur(dtolar);
            var ozet = AmbalajRaporVerisi.OzetOlustur(dtolar);
            var zaman = TurkeyTime.Now.ToString("yyyyMMdd-HHmmss");

            if (string.Equals(request.Format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Result<AmbalajDosyaDto>.Success(new AmbalajDosyaDto(
                    _dosyaService.PdfOlustur(satirlar, ozet),
                    "application/pdf",
                    $"ambalaj-uretim-raporu-{zaman}.pdf"));
            }

            return Result<AmbalajDosyaDto>.Success(new AmbalajDosyaDto(
                _dosyaService.ExcelOlustur(satirlar, ozet),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ambalaj-uretim-raporu-{zaman}.xlsx"));
        }
    }

    internal static class AmbalajRaporVerisi
    {
        public static async Task<AmbalajGorunumYetkileri> RaporYetkileriAsync(IRolService roles, ICurrentUserService user, CancellationToken ct)
        {
            var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(roles, user, ct);
            var m3Rapor = await AmbalajYetkilendirmeYardimcisi.YetkiliMiAsync(roles, user, AmbalajMenuKodlari.M3RaporGoruntule, ct, _3K.Core.Enums.YetkiTipi.R);
            return izin with { M3Gorunur = izin.M3Gorunur && m3Rapor, SarfGorunur = izin.SarfGorunur && m3Rapor };
        }
        public static List<AmbalajUretimKaydi> KayitlariGetir(
            IUnitOfWork unitOfWork,
            IAmbalajRaporFiltresi filtre) =>
            AmbalajSorguYardimcisi.Filtrele(
                    unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable(), filtre)
                .AsEnumerable()
                .OrderBy(k => k.ProjeId ?? int.MaxValue)
                .ThenBy(k => k.ManuelProjeNo)
                .ThenBy(k => k.SandikNo, SandikNumarasiComparer.Instance)
                .ThenBy(k => k.Id)
                .ToList();

        public static AmbalajRaporDto OzetDtoOlustur(
            IReadOnlyList<AmbalajUretimKaydiDto> dtolar,
            AmbalajGorunumYetkileri yetkiler)
        {
            var ozet = OzetOlustur(dtolar);
            return new AmbalajRaporDto
            {
                M3BilgisiGorunurMu = yetkiler.M3Gorunur,
                SarfBilgisiGorunurMu = yetkiler.SarfGorunur,
                KaynakBilgisiGorunurMu = yetkiler.KaynakGorunur,
                Kayitlar = dtolar,
                KayitSayisi = ozet.KayitSayisi,
                ToplamSandikAdedi = ozet.ToplamSandikAdedi,
                NetM3 = yetkiler.M3Gorunur ? ozet.NetM3 : null,
                SarfM3 = yetkiler.SarfGorunur ? ozet.SarfM3 : null,
                ToplamM3 = yetkiler.M3Gorunur && yetkiler.SarfGorunur ? ozet.ToplamM3 : null
            };
        }

        public static AmbalajRaporOzeti OzetOlustur(IReadOnlyList<AmbalajUretimKaydiDto> dtolar)
        {
            var dahil = dtolar.Where(k => !k.IptalMi && k.AmbalajaDahil && k.UretimeAlindi).ToList();
            return new AmbalajRaporOzeti(
                dtolar.Count,
                dahil.Sum(k => k.Adet),
                dahil.Any(k => k.NetM3.HasValue) ? dahil.Sum(k => k.NetM3) : null,
                dahil.Any(k => k.SarfM3.HasValue) ? dahil.Sum(k => k.SarfM3) : null,
                dahil.Any(k => k.ToplamM3.HasValue) ? dahil.Sum(k => k.ToplamM3) : null);
        }

        public static IReadOnlyList<AmbalajRaporSatiri> RaporSatirlariOlustur(
            IReadOnlyList<AmbalajUretimKaydiDto> dtolar) =>
            dtolar.Select(k => new AmbalajRaporSatiri
            {
                KayitId = k.Id,
                IsAkisKimligi = k.IsAkisKimligi,
                ProjeNo = k.ProjeNo ?? string.Empty,
                ProjeAdi = k.ProjeAdi,
                SandikNo = k.SandikNo,
                SandikAdi = k.Ad,
                Tur = k.Tur,
                KaynakModul = k.KaynakModul,
                SandikCinsi = k.SandikCinsiMetni,
                Adet = k.Adet,
                Boy = k.Boy,
                En = k.En,
                Yukseklik = k.Yukseklik,
                BirimM3 = k.Adet > 0 && k.NetM3.HasValue ? decimal.Round(k.NetM3.Value / k.Adet, 6) : null,
                NetM3 = k.NetM3,
                SarfOrani = k.SarfOrani,
                SarfM3 = k.SarfM3,
                ToplamM3 = k.ToplamM3,
                AmbalajaDahil = k.AmbalajaDahil,
                UretimeAlindi = k.UretimeAlindi,
                UretimDurumu = k.UretimDurumu,
                UretimTarihi = k.UretimTarihi,
                TalepEdenKisi = k.TalepEdenKisi,
                TalepEdenBolum = k.TalepEdenBolum,
                TalimatVeren = k.TalimatVeren,
                FirinPartiNo = k.FirinPartiNo,
                Aciklama = k.Aciklama,
                IptalMi = k.IptalMi,
                IptalNedeni = k.IptalNedeni,
                CreatedDate = k.CreatedDate
            }).ToList();
    }
}
