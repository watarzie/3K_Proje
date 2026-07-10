using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.Events;
using _3K.Application.Features.CekiIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiYukleCommandHandler : IRequestHandler<CekiYukleCommand, Result<CekiYuklemeResultDto>>
    {
        private readonly ICekiService _cekiService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;

        public CekiYukleCommandHandler(
            ICekiService cekiService,
            IUnitOfWork unitOfWork,
            IPublisher publisher)
        {
            _cekiService = cekiService;
            _unitOfWork = unitOfWork;
            _publisher = publisher;
        }

        public async Task<Result<CekiYuklemeResultDto>> Handle(CekiYukleCommand request, CancellationToken cancellationToken)
        {
            var ceki = await _cekiService.CekiYukleAsync(request.ExcelDosya, request.DosyaAdi);

            var satirlar = await _cekiService.GetCekiSatirlariAsync(ceki.Id);
            var satirList = satirlar.ToList();
            var benzersizSandikSayisi = satirList.Select(s => s.CekideGecenSandikNo).Distinct().Count();
            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(ceki.ProjeId);
            var projeNo = proje?.ProjeNo ?? ceki.ProjeId.ToString();

            var sonuc = new CekiYuklemeResultDto
            {
                CekiId = ceki.Id,
                ProjeId = ceki.ProjeId,
                ProjeNo = projeNo,
                SatirSayisi = satirList.Count,
                SandikSayisi = benzersizSandikSayisi,
                Mesaj = $"{satirList.Count} ürün satırı okundu, {benzersizSandikSayisi} benzersiz sandık oluşturuldu."
            };

            await _publisher.Publish(new CekiDosyasiYuklendiEvent(
                ceki.Id,
                ceki.ProjeId,
                projeNo,
                request.DosyaAdi,
                request.KullaniciId,
                RevizyonMu: false,
                SatirSayisi: satirList.Count,
                SandikSayisi: benzersizSandikSayisi), CancellationToken.None);

            return Result<CekiYuklemeResultDto>.Success(sonuc);
        }
    }
}
