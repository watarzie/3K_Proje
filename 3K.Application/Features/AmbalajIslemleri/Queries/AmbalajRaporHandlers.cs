using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Core.Constants;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    public sealed class GetAmbalajRaporDosyasiQueryHandler
        : IRequestHandler<GetAmbalajRaporDosyasiQuery, Result<DosyaIcerigi>>
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

        public async Task<Result<DosyaIcerigi>> Handle(
            GetAmbalajRaporDosyasiQuery request,
            CancellationToken cancellationToken)
        {
            var yetkiler = await AmbalajRaporVerisi.RaporYetkileriAsync(
                _rolService, _currentUserService, cancellationToken);
            var kayitlar = AmbalajRaporVerisi.KayitlariGetir(_unitOfWork, request.Tur, request.IzinliDurumlar);
            var dtolar = AmbalajRaporVerisi.DtolariOlustur(_unitOfWork, kayitlar);
            foreach (var dto in dtolar)
                AmbalajYetkilendirmeYardimcisi.DtoyuMaskele(dto, yetkiler);

            var zaman = TurkeyTime.Now.ToString("yyyyMMdd-HHmmss");
            return Result<DosyaIcerigi>.Success(new DosyaIcerigi(
                _dosyaService.PdfOlustur(AmbalajRaporVerisi.RaporSatirlariOlustur(dtolar), AmbalajRaporVerisi.OzetOlustur(dtolar)),
                "application/pdf",
                $"ambalaj-uretim-raporu-{zaman}.pdf"));
        }
    }

    internal static class AmbalajRaporVerisi
    {
        public static async Task<AmbalajGorunumYetkileri> RaporYetkileriAsync(IRolService roles, ICurrentUserService user, CancellationToken ct)
        {
            var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(roles, user, ct);
            var m3Rapor = await AmbalajYetkilendirmeYardimcisi.YetkiliMiAsync(roles, user, YetkiKodlari.Ambalaj.M3RaporGoruntule, ct, YetkiTipi.R);
            return izin with { M3Gorunur = izin.M3Gorunur && m3Rapor, SarfGorunur = izin.SarfGorunur && m3Rapor };
        }

        /// <summary>Aktif kayıtlar proje ve doğal sandık numarası sırasıyla döner; görülemeyen durumlar dahil edilmez.</summary>
        public static List<AmbalajUretimKaydi> KayitlariGetir(
            IUnitOfWork unitOfWork,
            AmbalajSandikTuru tur,
            int[]? izinliDurumlar) =>
            AmbalajDurumErisimi.Filtrele(unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable(), izinliDurumlar)
                .Where(k => !k.IptalMi && k.Tur == tur)
                .AsEnumerable()
                .OrderBy(k => k.ProjeId ?? int.MaxValue)
                .ThenBy(k => k.ManuelProjeNo)
                .ThenBy(k => k.SandikNo, SandikNumarasiComparer.Instance)
                .ThenBy(k => k.Id)
                .ToList();

        public static IReadOnlyList<AmbalajUretimKaydiDto> DtolariOlustur(
            IUnitOfWork unitOfWork,
            IReadOnlyList<AmbalajUretimKaydi> kayitlar)
        {
            var projeIds = kayitlar.Where(k => k.ProjeId.HasValue).Select(k => k.ProjeId!.Value).Distinct().ToList();
            var projeler = projeIds.Count == 0
                ? new Dictionary<int, Proje>()
                : unitOfWork.GetRepository<Proje>().Queryable()
                    .Where(p => projeIds.Contains(p.Id))
                    .ToList()
                    .ToDictionary(p => p.Id);
            var ustIds = kayitlar.Where(k => k.UstKayitId.HasValue).Select(k => k.UstKayitId!.Value).Distinct().ToList();
            var ustler = ustIds.Count == 0
                ? new Dictionary<int, string>()
                : unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
                    .Where(k => ustIds.Contains(k.Id))
                    .Select(k => new { k.Id, k.SandikNo })
                    .ToList()
                    .ToDictionary(k => k.Id, k => k.SandikNo);

            return kayitlar.Select(k =>
            {
                projeler.TryGetValue(k.ProjeId ?? 0, out var proje);
                ustler.TryGetValue(k.UstKayitId ?? 0, out var ustSandikNo);
                return AmbalajUretimYardimcilari.DtoOlustur(k, proje?.ProjeNo, proje?.Musteri, ustSandikNo);
            }).ToList();
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
