using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    public class ProjeListeleQueryHandler : IRequestHandler<ProjeListeleQuery, Result<IEnumerable<ProjeDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProjeListeleQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IEnumerable<ProjeDto>>> Handle(ProjeListeleQuery request, CancellationToken cancellationToken)
        {
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var projeler = await projeRepo.GetAllAsync();

            var result = projeler.Select(p => new ProjeDto
            {
                Id = p.Id,
                ProjeNo = p.ProjeNo,
                Musteri = p.Musteri,
                Durum = p.Durum.ToString(),
                PlanlananSevkTarihi = p.PlanlananSevkTarihi,
                SorumluKisi = p.SorumluKisi,
                SandikSayisi = p.Sandiklar?.Count ?? 0,
                FBNo = p.FBNo,
                Guc = p.Guc,
                Gerilim = p.Gerilim,
                Lokasyon = p.Lokasyon,
                OlcuResmiNo = p.OlcuResmiNo,
                NakilOlcuResmiNo = p.NakilOlcuResmiNo,
                SonMontajResmiNo = p.SonMontajResmiNo,
                ProjeMuduru = p.ProjeMuduru
            });

            return Result<IEnumerable<ProjeDto>>.Success(result);
        }
    }
}
