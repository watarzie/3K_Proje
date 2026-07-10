using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Commands
{
    public class BildirimAbonelikleriniGuncelleCommandHandler
        : IRequestHandler<BildirimAbonelikleriniGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public BildirimAbonelikleriniGuncelleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(
            BildirimAbonelikleriniGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();
            var mevcutKullaniciIdleri = (await kullaniciRepo.GetAllAsync())
                .Select(kullanici => kullanici.Id)
                .ToHashSet();
            var istenenKullaniciIdleri = request.CekiYuklendiAliciIdleri
                .Concat(request.CekiRevizyonuAliciIdleri)
                .Distinct()
                .ToList();

            if (istenenKullaniciIdleri.Any(id => !mevcutKullaniciIdleri.Contains(id)))
                return Result.Failure("Bildirim alıcı listesinde geçersiz kullanıcı bulunuyor.");

            var abonelikRepo = _unitOfWork.GetRepository<BildirimAboneligi>();
            var mevcutAbonelikler = await abonelikRepo.FindAsync(abonelik =>
                abonelik.TipId == (int)BildirimTipi.CekiYuklendi ||
                abonelik.TipId == (int)BildirimTipi.CekiRevizyonuYuklendi);
            var istenenAbonelikler = request.CekiYuklendiAliciIdleri
                .Distinct()
                .Select(kullaniciId => (KullaniciId: kullaniciId, TipId: (int)BildirimTipi.CekiYuklendi))
                .Concat(request.CekiRevizyonuAliciIdleri
                    .Distinct()
                    .Select(kullaniciId => (KullaniciId: kullaniciId, TipId: (int)BildirimTipi.CekiRevizyonuYuklendi)))
                .ToHashSet();
            var mevcutAbonelikAnahtarlari = mevcutAbonelikler
                .Select(abonelik => (abonelik.KullaniciId, abonelik.TipId))
                .ToHashSet();

            foreach (var abonelik in mevcutAbonelikler.Where(abonelik =>
                         !istenenAbonelikler.Contains((abonelik.KullaniciId, abonelik.TipId))))
            {
                abonelikRepo.Remove(abonelik);
            }

            foreach (var abonelik in istenenAbonelikler.Where(abonelik =>
                         !mevcutAbonelikAnahtarlari.Contains(abonelik)))
            {
                await abonelikRepo.AddAsync(new BildirimAboneligi
                {
                    KullaniciId = abonelik.KullaniciId,
                    TipId = abonelik.TipId
                });
            }

            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
    }
}
