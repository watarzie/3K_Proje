using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Application.Features.ProjeIslemleri.Services;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    /// <summary>
    /// Sevk edilmiş normal projenin kalanlarını otomatik sahaya aktarım için planlar; veri değiştirmez.
    /// </summary>
    public sealed class OtomatikSahaAktarimOnizleQuery :
        IRequest<Result<OtomatikSahaAktarimOnizlemeDto>>,
        ISecuredRequest,
        IRequiresMenuPermission
    {
        public string RequiredMenuKod => YetkiKodlari.Saha.SahayaAktar;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public int ProjeId { get; set; }
        public int? HedefSahaProjeId { get; set; }
        public bool YeniProje { get; set; }
    }

    public sealed class OtomatikSahaAktarimOnizleQueryHandler(
        IUnitOfWork unitOfWork,
        ISandikService sandikService,
        ISahaTamamlamaService sahaTamamlamaService,
        ILookupCacheService lookupCache) :
        IRequestHandler<OtomatikSahaAktarimOnizleQuery, Result<OtomatikSahaAktarimOnizlemeDto>>
    {
        public async Task<Result<OtomatikSahaAktarimOnizlemeDto>> Handle(
            OtomatikSahaAktarimOnizleQuery request,
            CancellationToken cancellationToken)
        {
            var hazirlik = await new OtomatikSahaAktarimHazirlayici(unitOfWork, sandikService, sahaTamamlamaService)
                .HazirlaAsync(request.ProjeId, request.HedefSahaProjeId, request.YeniProje, cancellationToken);
            if (!hazirlik.IsSuccess)
                return SahaAktarimSonucEslemesi.Basarisiz<OtomatikSahaAktarimOnizlemeDto>(hazirlik);

            var h = hazirlik.Value!;
            var plan = h.Plan;

            return Result<OtomatikSahaAktarimOnizlemeDto>.Success(new OtomatikSahaAktarimOnizlemeDto
            {
                ProjeId = h.Proje.Id,
                ProjeNo = h.Proje.ProjeNo,
                PlanOzeti = h.OnizlemeOzeti,
                AktarilacakVarMi = !plan.BosMu,
                AktarilacakSandikSayisi = plan.SandikAktarimlari.Count,
                EksikSatirSayisi = plan.EksikKalemleri.Count,
                EksikToplamAdet = plan.EksikKalemleri.Sum(e => e.Miktar),
                AtlananSayisi = plan.Atlananlar.Count,
                HedefSahaProjeId = h.HedefSahaProje?.Id,
                HedefSahaProjeNo = h.HedefSahaProje?.ProjeNo ?? (h.YeniSahaProjesi ? h.YeniSahaProjeNoOnerisi : null),
                YeniSahaProjesi = h.YeniSahaProjesi,
                HedefSecimiGerekli = h.HedefSecimiGerekli,
                YeniSahaProjeNoOnerisi = h.YeniSahaProjeNoOnerisi,
                HedefSecenekleri = h.HedefSecenekleri
                    .Select(p => new OtomatikSahaHedefDto
                    {
                        SahaProjeId = p.Id,
                        SahaProjeNo = p.ProjeNo,
                        DurumMetni = lookupCache.GetDeger<LookupProjeDurum>(p.DurumId),
                        SandikSayisi = h.HedefSandikSayilari.GetValueOrDefault(p.Id)
                    })
                    .ToList(),
                Sandiklar = plan.SandikAktarimlari
                    .Select(aktarim =>
                    {
                        var hedef = h.SandikHedefleri[aktarim.KaynakSandik.Id];
                        return new OtomatikSahaSandikDto
                        {
                            KaynakSandikId = aktarim.KaynakSandik.Id,
                            KaynakSandikNo = aktarim.KaynakSandik.SandikNo,
                            SandikAdi = aktarim.KaynakSandik.Ad,
                            HedefSandikNo = hedef.SandikNo,
                            MevcutSandigaEklenir = hedef.HedefSandikId.HasValue,
                            NumaraDegisti = hedef.NumaraDegisti,
                            UrunSayisi = aktarim.Urunler.Count,
                            ToplamAdet = aktarim.Urunler.Sum(u => u.Miktar),
                            Urunler = aktarim.Urunler.Select(UrunDto).ToList()
                        };
                    })
                    .ToList(),
                EksiklerSandigi = h.EksiklerHedefi == null
                    ? null
                    : new OtomatikSahaEksiklerSandigiDto
                    {
                        SandikNo = h.EksiklerHedefi.SandikNo,
                        SandikAdi = h.EksiklerHedefi.HedefSandikId.HasValue ? null : SahaAktarimConstants.EksiklerSandikAdi,
                        MevcutSandigaEklenir = h.EksiklerHedefi.HedefSandikId.HasValue,
                        ToplamAdet = plan.EksikKalemleri.Sum(e => e.Miktar),
                        Urunler = plan.EksikKalemleri.Select(UrunDto).ToList()
                    },
                Atlananlar = plan.Atlananlar
                    .Select(a => new OtomatikSahaAtlananDto
                    {
                        SandikNo = a.SandikNo,
                        CekiSatiriId = a.Satir?.Id,
                        Urun = a.Satir == null ? null : $"#{a.Satir.SiraNo} {a.Satir.Aciklama}".Trim(),
                        Neden = a.Neden,
                        Ayrintilar = a.Ayrintilar?.ToList() ?? []
                    })
                    .ToList()
            });
        }

        private OtomatikSahaUrunDto UrunDto(OtomatikAktarimUrunu urun) => new()
        {
            CekiSatiriId = urun.Satir.Id,
            SiraNo = urun.Satir.SiraNo,
            BarkodNo = urun.Satir.BarkodNo,
            Aciklama = urun.Satir.Aciklama,
            KaynakSandikNo = urun.KaynakSandikNo,
            IstenenAdet = urun.Satir.IstenenAdet,
            Miktar = urun.Miktar,
            Birim = lookupCache.GetDeger<LookupBirim>(urun.Satir.BirimId)
        };
    }
}
