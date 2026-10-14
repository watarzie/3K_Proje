using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Application.Features.UcKIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Queries
{
    public class GetProjedenKarsilamaKaynaklariQueryHandler
        : IRequestHandler<GetProjedenKarsilamaKaynaklariQuery, Result<List<ProjedenKarsilamaKaynakDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly GridUrunListesiOkuyucu _okuyucu;

        public GetProjedenKarsilamaKaynaklariQueryHandler(
            IUnitOfWork unitOfWork,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _okuyucu = new GridUrunListesiOkuyucu(unitOfWork, lookupCache, sahaTamamlamaService);
        }

        public async Task<Result<List<ProjedenKarsilamaKaynakDto>>> Handle(
            GetProjedenKarsilamaKaynaklariQuery request,
            CancellationToken cancellationToken)
        {
            var kaynakProje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.KaynakProjeId);
            if (kaynakProje == null)
                return Result<List<ProjedenKarsilamaKaynakDto>>.Failure("Kaynak proje bulunamadı.", 404);

            // Ürünsüz kaynak proje hata değildir; panel "eşleşen ürün yok" bilgisini gösterir.
            var urunler = await _okuyucu.OkuAsync(kaynakProje, null, null, cancellationToken) ?? [];

            // Manuel saha/yedek içeriğinin çeki satırı yoktur (negatif kimlik); karşılama komutu
            // pozitif kaynak çeki satırı ister, bu yüzden seçenek olarak sunulmaz.
            var kaynaklar = urunler
                .Where(u => !u.IsSahaManuelSandikIcerigi)
                .Select(u => new ProjedenKarsilamaKaynakDto
                {
                    CekiSatiriId = u.CekiSatiriId,
                    SiraNo = u.SiraNo,
                    BarkodNo = u.BarkodNo,
                    OlcuResmiPozNo = u.OlcuResmiPozNo,
                    Aciklama = u.Aciklama,
                    GelenMiktar = u.GelenMiktar,
                    NetKullanilabilir = u.NetKullanilabilir
                })
                .ToList();

            return Result<List<ProjedenKarsilamaKaynakDto>>.Success(kaynaklar);
        }
    }
}
