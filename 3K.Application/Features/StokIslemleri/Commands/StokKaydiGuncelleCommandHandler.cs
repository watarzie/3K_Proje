using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    public class StokKaydiGuncelleCommandHandler : IRequestHandler<StokKaydiGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public StokKaydiGuncelleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(StokKaydiGuncelleCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<StokKaydi>();
            var stok = await repo.GetByIdAsync(request.Id);

            if (stok == null)
            {
                return Result.Failure("Güncellenmek istenen stok kaydı bulunamadı.");
            }

            stok.MalzemeKodu = request.MalzemeKodu ?? string.Empty;
            stok.MalzemeAdi = request.MalzemeAdi;
            stok.Miktar = request.Miktar;
            stok.Birim = request.Birim ?? string.Empty;
            stok.Lokasyon = request.DepoLokasyonu;
            stok.KaynakProje = request.KaynakProje;
            stok.StokGirisNedeni = request.StokGirisNedeni;

            // Eğer miktar 0 yapıldıysa durumu güncelle, isteğe bağlı
            if (stok.Miktar == 0) stok.Durum = "Ölü Kayıt";
            else stok.Durum = "Aktif";

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
    }
}
