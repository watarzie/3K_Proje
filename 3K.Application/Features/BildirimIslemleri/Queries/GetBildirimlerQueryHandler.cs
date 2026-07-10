using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.DTOs;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Queries
{
    public class GetBildirimlerQueryHandler
        : IRequestHandler<GetBildirimlerQuery, Result<BildirimListeSonucDto>>
    {
        private readonly IBildirimRepository _bildirimRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetBildirimlerQueryHandler(
            IBildirimRepository bildirimRepository,
            ICurrentUserService currentUserService)
        {
            _bildirimRepository = bildirimRepository;
            _currentUserService = currentUserService;
        }

        public async Task<Result<BildirimListeSonucDto>> Handle(
            GetBildirimlerQuery request,
            CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result<BildirimListeSonucDto>.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var filtre = new BildirimListeFiltresi
            {
                OkunduMu = DurumFiltresiniCoz(request.Durum),
                BaslangicTarihi = request.BaslangicTarihi,
                BitisTarihiHaric = BitisTarihiniHaricSiniraCevir(request.BitisTarihi),
                TipId = request.TipId,
                Arama = request.Arama?.Trim(),
                Sayfa = request.Sayfa,
                SayfaBoyutu = request.SayfaBoyutu
            };

            var sonuc = await _bildirimRepository.GetSayfaliAsync(
                kullaniciId.Value,
                filtre,
                cancellationToken);

            var toplamSayfa = sonuc.ToplamKayit == 0
                ? 0
                : (int)Math.Ceiling(sonuc.ToplamKayit / (double)request.SayfaBoyutu);

            return Result<BildirimListeSonucDto>.Success(new BildirimListeSonucDto
            {
                Bildirimler = sonuc.Bildirimler.Select(bildirim => bildirim.ToDto()).ToList(),
                ToplamKayit = sonuc.ToplamKayit,
                ToplamOkunmamis = sonuc.ToplamOkunmamis,
                Sayfa = request.Sayfa,
                SayfaBoyutu = request.SayfaBoyutu,
                ToplamSayfa = toplamSayfa
            });
        }

        private static bool? DurumFiltresiniCoz(string durum)
        {
            return durum.Trim().ToLowerInvariant() switch
            {
                "okunmus" => true,
                "okunmamis" => false,
                _ => null
            };
        }

        private static DateTime? BitisTarihiniHaricSiniraCevir(DateTime? bitisTarihi)
        {
            if (!bitisTarihi.HasValue)
                return null;

            var deger = bitisTarihi.Value;
            if (deger.TimeOfDay == TimeSpan.Zero && deger.Date < DateTime.MaxValue.Date)
                return deger.Date.AddDays(1);

            return deger < DateTime.MaxValue ? deger.AddTicks(1) : deger;
        }
    }
}
