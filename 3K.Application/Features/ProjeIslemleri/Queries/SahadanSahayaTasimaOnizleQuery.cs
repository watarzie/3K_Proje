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
    /// <summary>Sahadan sahaya taşımanın önizlemesi: taşınacak satırlar, hedef sandık numarası ve engeller (veri değiştirmez).</summary>
    public sealed class SahadanSahayaTasimaOnizleQuery :
        IRequest<Result<SahadanSahayaTasimaOnizlemeDto>>,
        ISecuredRequest,
        IRequiresMenuPermission
    {
        public string RequiredMenuKod => YetkiKodlari.Saha.SahayaAktar;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public int KaynakSahaProjeId { get; set; }
        public List<int> SahaCekiSatiriIds { get; set; } = [];
        public int? KaynakSandikId { get; set; }
        public int HedefSahaProjeId { get; set; }
        public int? HedefSandikId { get; set; }
        public string? YeniSandikNo { get; set; }
    }

    public sealed class SahadanSahayaTasimaOnizleQueryHandler(
        IUnitOfWork unitOfWork,
        ILookupCacheService lookupCache) :
        IRequestHandler<SahadanSahayaTasimaOnizleQuery, Result<SahadanSahayaTasimaOnizlemeDto>>
    {
        public async Task<Result<SahadanSahayaTasimaOnizlemeDto>> Handle(
            SahadanSahayaTasimaOnizleQuery request,
            CancellationToken cancellationToken)
        {
            var hazirlik = await SahadanSahayaTasimaIslemi.HazirlaAsync(unitOfWork, new SahadanSahayaTasimaTalebi(
                    request.KaynakSahaProjeId,
                    request.SahaCekiSatiriIds,
                    request.KaynakSandikId,
                    request.HedefSahaProjeId,
                    request.HedefSandikId,
                    request.YeniSandikNo), cancellationToken);
            if (!hazirlik.IsSuccess)
                return SahaAktarimSonucEslemesi.Basarisiz<SahadanSahayaTasimaOnizlemeDto>(hazirlik);

            var h = hazirlik.Value!;
            return Result<SahadanSahayaTasimaOnizlemeDto>.Success(new SahadanSahayaTasimaOnizlemeDto
            {
                KaynakSahaProjeNo = h.KaynakProje.ProjeNo,
                HedefSahaProjeNo = h.HedefProje.ProjeNo,
                HedefSandikNo = h.HedefSandikNo,
                YeniSandik = h.HedefSandik == null,
                TasinabilirMi = h.Engeller.Count == 0 && (h.Satirlar.Count > 0 || h.ManuelIcerikler.Count > 0),
                ManuelIcerikSayisi = h.ManuelIcerikler.Count,
                ToplamMiktar = h.Satirlar.Sum(s => s.Kalem.Miktar),
                BosalacakSandikNolari = h.BosalacakSandikNolari.ToList(),
                Engeller = h.Engeller.ToList(),
                Satirlar = h.Satirlar
                    .Select(s => new SahadanSahayaTasimaSatiriDto
                    {
                        SahaCekiSatiriId = s.Satir.Id,
                        KaynakProjeNo = h.KokProjeNolari.GetValueOrDefault(s.Satir.KaynakCekiSatiriId!.Value, "?"),
                        SiraNo = h.KokSatirlar.TryGetValue(s.Satir.KaynakCekiSatiriId!.Value, out var kok) ? kok.SiraNo : s.Satir.SiraNo,
                        BarkodNo = s.Satir.BarkodNo,
                        Aciklama = s.Satir.Aciklama,
                        KaynakSandikNo = s.KaynakSandik.SandikNo,
                        Miktar = s.Kalem.Miktar,
                        KonulanAdet = s.Icerik.KonulanAdet,
                        Birim = lookupCache.GetDeger<LookupBirim>(s.Satir.BirimId)
                    })
                    .ToList()
            });
        }
    }
}
