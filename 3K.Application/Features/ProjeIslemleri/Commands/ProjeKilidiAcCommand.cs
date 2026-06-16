using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeKilidiAcCommand : IRequest<Result>, ISecuredRequest, IAlwaysRequireApproval
    {
        public int ProjeId { get; set; }
        public string? ProjeNo { get; set; }
        public int KilitAcmaTipiId { get; set; } = (int)SevkiyatKilitAcmaTipi.SevkiyatGeriAlinarakAc;
        public string? Aciklama { get; set; }

        public string GetApprovalDescription()
        {
            var tip = KilitAcmaTipiId == (int)SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc
                ? "Sevkiyat kaydı korunarak proje kilidi açma"
                : "Sevkiyat geri alınarak proje kilidi açma";

            var projeBilgisi = string.IsNullOrWhiteSpace(ProjeNo)
                ? $"ProjeId: {ProjeId}"
                : $"Proje: {ProjeNo}";

            var gerekceBilgisi = string.IsNullOrWhiteSpace(Aciklama)
                ? string.Empty
                : $" Gerekçe: {TekSatir(Aciklama)}";

            return $"{tip} talebi. {projeBilgisi}.{gerekceBilgisi}";
        }

        private static string TekSatir(string value)
        {
            var normalized = string.Join(" ", value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return normalized.Length > 250 ? normalized[..250] + "..." : normalized;
        }
    }

    public class ProjeKilidiAcCommandHandler : IRequestHandler<ProjeKilidiAcCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public ProjeKilidiAcCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(ProjeKilidiAcCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Proje>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sevkiyatSandikRepo = _unitOfWork.GetRepository<SevkiyatSandik>();
            var proje = await repo.GetByIdAsync(request.ProjeId);

            if (proje == null)
                return Result.Failure("Proje bulunamadı.");

            if (proje.DurumId != (int)ProjeDurum.SevkEdildi && proje.DurumId != (int)ProjeDurum.EksikSevkEdildi)
                return Result.Failure("Proje sevk edilmediği için kilidi açılamaz.");

            if (!Enum.IsDefined(typeof(SevkiyatKilitAcmaTipi), request.KilitAcmaTipiId))
                return Result.Failure("Geçersiz kilit açma tipi.");

            var kilitAcmaTipi = (SevkiyatKilitAcmaTipi)request.KilitAcmaTipiId;
            int eskiDurum = proje.DurumId;

            // ===== Kilit açma tipine göre sandıkları geri al veya düzeltmeye aç =====
            var sandiklar = (await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId)).ToList();
            int geriDondurulenSandik = 0;
            foreach (var sandik in sandiklar)
            {
                if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                {
                    if (kilitAcmaTipi == SevkiyatKilitAcmaTipi.SevkiyatGeriAlinarakAc)
                    {
                        var sevkiyatBaglari = (await sevkiyatSandikRepo.FindAsync(ss => ss.SandikId == sandik.Id)).ToList();
                        foreach (var sevkiyatBag in sevkiyatBaglari)
                            sevkiyatSandikRepo.Remove(sevkiyatBag);
                    }

                    if (kilitAcmaTipi == SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc)
                    {
                        sandik.SevkiyatDuzeltmeAcikMi = true;
                    }
                    else
                    {
                        sandik.DurumId = sandik.SevkOncesiDurumId ?? (int)SandikDurum.Kapandi;
                        sandik.SevkOncesiDurumId = null;
                        sandik.SevkiyatDuzeltmeAcikMi = false;
                    }

                    sandikRepo.Update(sandik);
                    geriDondurulenSandik++;
                }
            }

            if (geriDondurulenSandik == 0)
                return Result.Failure("Kilidi açılacak sevk edilmiş sandık bulunamadı.");

            if (kilitAcmaTipi == SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc)
            {
                proje.DurumId = ProjeSevkDurumHelper.Hesapla(sandiklar, proje.DurumId);
            }
            else
            {
                proje.DurumId = ProjeSevkDurumHelper.Hesapla(sandiklar, proje.DurumId);
                if (sandiklar.All(s => s.DurumId != (int)SandikDurum.Sevkedildi))
                    proje.GerceklesenSevkTarihi = null;
            }
            repo.Update(proje);

            await _unitOfWork.SaveChangesAsync();

            var aciklamaNotu = string.IsNullOrWhiteSpace(request.Aciklama)
                ? "Açıklama yok"
                : request.Aciklama.Trim();

            var islemAdi = kilitAcmaTipi == SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc
                ? "Proje Kilidi Açıldı - Sevkiyat Kaydı Korundu"
                : "Proje Kilidi Açıldı - Sevkiyat Geri Alındı";

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = proje.Id,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Proje",
                ReferansId = proje.Id.ToString(),
                Islem = islemAdi,
                IslemTipiId = null,
                EskiDeger = eskiDurum.ToString(),
                YeniDeger = proje.DurumId.ToString(),
                Aciklama = $"Proje kilidi açıldı. Mod: {GetKilitAcmaTipiMetni(kilitAcmaTipi)}. {geriDondurulenSandik} sandık işlem için açıldı. Not: {aciklamaNotu}"
            });

            return Result.Success();
        }

        private static string GetKilitAcmaTipiMetni(SevkiyatKilitAcmaTipi tip)
        {
            return tip == SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc
                ? "Sevkiyat kaydı korundu"
                : "Sevkiyat geri alındı";
        }
    }
}
