using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    public class StokKaydiSilCommandHandler : IRequestHandler<StokKaydiSilCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public StokKaydiSilCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(StokKaydiSilCommand request, CancellationToken cancellationToken)
        {
            var stokRepo = _unitOfWork.GetRepository<StokKaydi>();
            var stok = await stokRepo.GetByIdAsync(request.Id);

            if (stok == null)
                return Result.Failure("Silinecek stok kaydı bulunamadı.", 404);

            var hareketler = await _unitOfWork.GetRepository<StokHareketi>()
                .FindAsync(h => h.StokKaydiId == request.Id);

            if (hareketler.Any())
                return Result.Failure("Bu stok kaydı işlem geçmişine bağlı olduğu için silinemez. Silmeden önce ilgili stok hareketlerini geri almanız gerekir.", 400);

            stokRepo.Remove(stok);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
    }
}
