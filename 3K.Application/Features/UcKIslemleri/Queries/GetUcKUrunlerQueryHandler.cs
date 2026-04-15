using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Queries
{
    public class GetUcKUrunlerQueryHandler : IRequestHandler<GetUcKUrunlerQuery, Result<List<UcKUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetUcKUrunlerQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
                    var kalan = cs.UcKKarsilamaTipi == StatusConstants.UcKDurum.BaskaProyeVerildi
                        ? 0
                        : cs.IstenenAdet - cs.GelenMiktar - cs.TrafoSevkAdet;
                    if (kalan < 0) kalan = 0;

                    return new UcKUrunDto
                    {
                        CekiSatiriId = cs.Id,
                        SiraNo = cs.SiraNo,
                        BarkodNo = cs.BarkodNo,
                        Aciklama = cs.Aciklama,
                        SandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo,
                        IstenenAdet = cs.IstenenAdet,
                        Birim = cs.Birim,
                        GridDurumu = cs.GridDurumu,
                        GridGelenAdet = cs.GridGelenAdet,
                        TrafoSevkAdet = cs.TrafoSevkAdet,
                        UcKKarsilamaTipi = cs.UcKKarsilamaTipi,
                        GelenMiktar = cs.GelenMiktar,
                        KaynakHedefProjeNo = cs.KaynakHedefProjeNo,
                        UcKAciklama = cs.UcKAciklama,
                        UcKNotu = cs.UcKNotu,
                        Kalan = kalan,
                        KontrolUyari = HesaplaKontrolUyari(cs),
                        GenelDurum = cs.Durum
                    };
                })
                .ToList();

            return Result<List<UcKUrunDto>>.Success(result);
        }

        private static string HesaplaKontrolUyari(CekiSatiri cs)
        {
            var tip = cs.UcKKarsilamaTipi;
            var kalan = cs.IstenenAdet - cs.GelenMiktar - cs.TrafoSevkAdet;

            return tip switch
            {
                StatusConstants.UcKDurum.TamGeldi when kalan <= 0 => "TAMAMLANDI",
                StatusConstants.UcKDurum.TamGeldi => "TAM GELDİ",
                StatusConstants.UcKDurum.EksikGeldi => "EKSİK GELDİ",
                StatusConstants.UcKDurum.ProjedenKarsilandi => $"PROJEDEN KARŞILANDI – {cs.KaynakHedefProjeNo ?? ""}",
                StatusConstants.UrunDurum.StoktanKarsilandi => "STOKTAN KARŞILANDI",
                StatusConstants.UcKDurum.TedarikcidenGeldi => "TEDARİKÇİDEN GELDİ",
                StatusConstants.UcKDurum.BaskaProyeVerildi => $"BAŞKA PROJEYE VERİLDİ – {cs.KaynakHedefProjeNo ?? ""}",
                StatusConstants.UcKDurum.HataliUrun => "HATALI ÜRÜN GELDİ",
                _ when cs.GridDurumu == StatusConstants.UcKDurum.TamGeldi && cs.GelenMiktar < cs.IstenenAdet =>
                    "UYARI: GRİD TAM SEVK, 3K EKSİK GELİŞ",
                _ => "BEKLİYOR"
            };
        }
    }
}
