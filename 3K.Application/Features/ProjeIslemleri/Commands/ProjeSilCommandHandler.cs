using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeSilCommandHandler : IRequestHandler<ProjeSilCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProjeSilCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(ProjeSilCommand request, CancellationToken cancellationToken)
        {
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var proje = await projeRepo.GetByIdAsync(request.ProjeId);

            if (proje == null)
                return Result<bool>.Failure("Proje bulunamadı.");

            // FK'ler Restrict olduğu için alt verileri sırayla silmeliyiz

            // 2. StokHareketleri
            var stokHareketRepo = _unitOfWork.GetRepository<StokHareketi>();
            var stokHareketleri = await stokHareketRepo.FindAsync(s => s.ProjeId == request.ProjeId);
            foreach (var sh in stokHareketleri) stokHareketRepo.Remove(sh);

            // 3. HareketGecmisi
            var hareketRepo = _unitOfWork.GetRepository<HareketGecmisi>();
            var hareketler = await hareketRepo.FindAsync(h => h.ProjeId == request.ProjeId);
            foreach (var h in hareketler) hareketRepo.Remove(h);

            // 4. ProjeTransfer
            var transferRepo = _unitOfWork.GetRepository<ProjeTransfer>();
            var transferler = await transferRepo.FindAsync(t => t.KaynakProjeId == request.ProjeId || t.HedefProjeId == request.ProjeId);
            foreach (var t in transferler) transferRepo.Remove(t);

            // 5. SandıkIcerik (sandıkların içerikleri)
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandiklar = await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId);
            var sandikIdler = sandiklar.Select(s => s.Id).ToList();

            if (sandikIdler.Count > 0)
            {
                var sevkiyatSandikRepo = _unitOfWork.GetRepository<SevkiyatSandik>();
                var sevkiyatSandiklari = await sevkiyatSandikRepo.FindAsync(ss => sandikIdler.Contains(ss.SandikId));
                foreach (var ss in sevkiyatSandiklari) sevkiyatSandikRepo.Remove(ss);
            }

            var sevkiyatRepo = _unitOfWork.GetRepository<Sevkiyat>();
            var sevkiyatlar = await sevkiyatRepo.FindAsync(s => s.ProjeId == request.ProjeId);
            foreach (var sevkiyat in sevkiyatlar) sevkiyatRepo.Remove(sevkiyat);

            if (sandikIdler.Count > 0)
            {
                var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
                var icerikler = await icerikRepo.FindAsync(i => sandikIdler.Contains(i.SandikId));
                foreach (var ic in icerikler) icerikRepo.Remove(ic);
            }

            // 6. Sandıklar
            foreach (var s in sandiklar) sandikRepo.Remove(s);

            // 7. CekiSatiri (çekinin satırları)
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiler = await cekiRepo.FindAsync(c => c.ProjeId == request.ProjeId);
            var cekiIdler = cekiler.Select(c => c.Id).ToList();

            if (cekiIdler.Count > 0)
            {
                var satirRepo = _unitOfWork.GetRepository<CekiSatiri>();
                var satirlar = await satirRepo.FindAsync(s => cekiIdler.Contains(s.CekiId));
                foreach (var sat in satirlar) satirRepo.Remove(sat);
            }

            // 8. Cekiler
            foreach (var c in cekiler) cekiRepo.Remove(c);

            // 9. Proje
            projeRepo.Remove(proje);

            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
    }
}
