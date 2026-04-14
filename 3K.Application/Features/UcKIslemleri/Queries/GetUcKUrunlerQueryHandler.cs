using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;
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
                    var kalan = cs.UcKKarsilamaTipi == "BaskaProyeVerildi"
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
                "TamGeldi" when kalan <= 0 => "TAMAMLANDI",
                "TamGeldi" => "TAM GELDİ",
                "EksikGeldi" => "EKSİK GELDİ",
                "ProjedenKarsilandi" => $"PROJEDEN KARŞILANDI – {cs.KaynakHedefProjeNo ?? ""}",
                "StoktanKarsilandi" => "STOKTAN KARŞILANDI",
                "TedarikcidenGeldi" => "TEDARİKÇİDEN GELDİ",
                "BaskaProyeVerildi" => $"BAŞKA PROJEYE VERİLDİ – {cs.KaynakHedefProjeNo ?? ""}",
                "HataliUrun" => "HATALI ÜRÜN GELDİ",
                _ when cs.GridDurumu == "TamGeldi" && cs.GelenMiktar < cs.IstenenAdet =>
                    "UYARI: GRİD TAM SEVK, 3K EKSİK GELİŞ",
                _ => "BEKLİYOR"
            };
        }
    }
}
