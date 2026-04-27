using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Queries
{
    public class GetUcKUrunlerQueryHandler : IRequestHandler<GetUcKUrunlerQuery, Result<List<UcKUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public GetUcKUrunlerQueryHandler(IUnitOfWork unitOfWork, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
        }

        public async Task<Result<List<UcKUrunDto>>> Handle(GetUcKUrunlerQuery request, CancellationToken cancellationToken)
        {
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiler = await cekiRepo.FindAsync(c => c.ProjeId == request.ProjeId);

            if (!cekiler.Any())
                return Result<List<UcKUrunDto>>.Failure("Bu projeye ait çeki bulunamadı.", 404);

            var cekiIdler = cekiler.Select(c => c.Id).ToList();

            var satiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = await satiriRepo.FindAsync(cs => cekiIdler.Contains(cs.CekiId));

            var result = satirlar
                .OrderBy(cs => cs.SiraNo)
                .Select(cs =>
                {
                    return new UcKUrunDto
                    {
                        CekiSatiriId = cs.Id,
                        SiraNo = cs.SiraNo,
                        BarkodNo = cs.BarkodNo,
                        Aciklama = cs.Aciklama,
                        SandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo,
                        IstenenAdet = cs.IstenenAdet,
                        Birim = ((Birim)cs.BirimId).ToString(),
                        GridDurumuId = cs.GridDurumuId,
                        GridDurumuMetni = _lookupCache.GetDeger<LookupGridDurum>(cs.GridDurumuId),
                        GridGelenAdet = cs.GridGelenAdet,
                        TrafoSevkAdet = cs.TrafoSevkAdet,
                        GridSevkDurumuId = cs.GridSevkDurumuId,
                        GridSevkDurumuMetni = _lookupCache.GetDeger<LookupGridSevkDurum>(cs.GridSevkDurumuId),
                        UcKKarsilamaTipiId = cs.UcKKarsilamaTipiId,
                        UcKKarsilamaTipiMetni = _lookupCache.GetDeger<LookupUcKDurum>(cs.UcKKarsilamaTipiId),
                        GelenMiktar = cs.GelenMiktar,
                        KarsilananMiktar = cs.KarsilananMiktar,
                        HataliMiktar = cs.HataliMiktar,
                        KaynakHedefProjeNo = cs.KaynakHedefProjeNo,
                        GeriGonderilmeSebebiId = cs.GeriGonderilmeSebebiId,
                        GeriGonderilmeSebebiMetni = cs.GeriGonderilmeSebebiId.HasValue
                            ? _lookupCache.GetDeger<LookupGeriGonderilmeSebebi>(cs.GeriGonderilmeSebebiId.Value)
                            : null,
                        UcKAciklama = cs.UcKAciklama,
                        GridAciklama = cs.GridAciklama,
                        Kalan = cs.KalanMiktar,
                        KontrolUyari = HesaplaKontrolUyari(cs),
                        GenelDurumId = cs.DurumId,
                        GenelDurumMetni = _lookupCache.GetDeger<LookupUrunDurum>(cs.DurumId),
                        // Madde 2: Parçalı karşılama
                        StokKarsilanan = cs.StokKarsilanan,
                        ProjeKarsilanan = cs.ProjeKarsilanan,
                        TedarikciKarsilanan = cs.TedarikciKarsilanan,
                        EksikMiktar = cs.EksikMiktar
                    };
                })
                .ToList();

            return Result<List<UcKUrunDto>>.Success(result);
        }

        private string HesaplaKontrolUyari(CekiSatiri cs)
        {
            var tip = cs.UcKKarsilamaTipiId;

            return tip switch
            {
                (int)UcKDurum.TamGeldi when cs.KalanMiktar <= 0 => "TAMAMLANDI",
                (int)UcKDurum.TamGeldi => "TAM GELDİ",
                (int)UcKDurum.EksikGeldi => "EKSİK GELDİ",
                (int)UcKDurum.Gelmedi => "GELMEDİ",
                (int)UcKDurum.GeriGonderildi => $"GERİ GÖNDERİLDİ – {(cs.GeriGonderilmeSebebiId.HasValue ? _lookupCache.GetDeger<LookupGeriGonderilmeSebebi>(cs.GeriGonderilmeSebebiId.Value) : "Belirtilmemiş")}",
                (int)UcKDurum.ProjedenKarsilandi => $"PROJEDEN KARŞILANDI – {cs.KaynakHedefProjeNo ?? ""}",
                (int)UcKDurum.StoktanKarsilandi => "STOKTAN KARŞILANDI",
                (int)UcKDurum.TedarikcidenGeldi => "TEDARİKÇİDEN GELDİ",
                (int)UcKDurum.HataliUrun => $"HATALI ÜRÜN – {cs.HataliMiktar} adet",
                _ when cs.GridDurumuId == (int)GridDurum.Iptal => "GRİD İPTAL – İŞLEM YAPILAMAZ",
                _ when cs.GridDurumuId == (int)GridDurum.TrafoSevk => "TRAFO SEVK – İŞLEM YAPILAMAZ",
                _ when cs.GridDurumuId == (int)GridDurum.TamGeldi && cs.GelenMiktar < cs.IstenenAdet =>
                    "UYARI: GRİD TAM SEVK, 3K EKSİK GELİŞ",
                _ => "BEKLİYOR"
            };
        }
    }
}
