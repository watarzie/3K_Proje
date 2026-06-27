using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikKilidiAcCommand : IRequest<Result>, ISecuredRequest, IAlwaysRequireApproval, IApprovalOperation
    {
        public int ProjeId { get; set; }
        public int SandikId { get; set; }
        public string? ProjeNo { get; set; }
        public string? SandikNo { get; set; }
        public int KilitAcmaTipiId { get; set; } = (int)SevkiyatKilitAcmaTipi.SevkiyatGeriAlinarakAc;
        public string? Aciklama { get; set; }

        public string GetApprovalOperationCode()
        {
            return OnayIslemKodlari.SandikKilidiAc;
        }

        public string GetApprovalDescription()
        {
            var tip = KilitAcmaTipiId == (int)SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc
                ? "Sevkiyat kaydı korunarak sandık kilidi açma"
                : "Sevkiyat geri alınarak sandık kilidi açma";

            var projeBilgisi = string.IsNullOrWhiteSpace(ProjeNo)
                ? $"ProjeId: {ProjeId}"
                : $"Proje: {ProjeNo}";
            var sandikBilgisi = string.IsNullOrWhiteSpace(SandikNo)
                ? $"SandikId: {SandikId}"
                : $"Sandık: {SandikNo}";

            var gerekceBilgisi = string.IsNullOrWhiteSpace(Aciklama)
                ? string.Empty
                : $" Gerekçe: {TekSatir(Aciklama)}";

            return $"{tip} talebi. {projeBilgisi}, {sandikBilgisi}.{gerekceBilgisi}";
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

    public class SandikKilidiAcCommandHandler : IRequestHandler<SandikKilidiAcCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public SandikKilidiAcCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(SandikKilidiAcCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var sevkiyatSandikRepo = _unitOfWork.GetRepository<SevkiyatSandik>();

            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);

            if (sandik.DurumId != (int)SandikDurum.Sevkedildi)
                return Result.Failure("Sandık sevk edilmiş durumda olmadığı için kilidi açılamaz.");

            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result.Failure("Proje bulunamadı.", 404);

            if (!Enum.IsDefined(typeof(SevkiyatKilitAcmaTipi), request.KilitAcmaTipiId))
                return Result.Failure("Geçersiz kilit açma tipi.");

            var kilitAcmaTipi = (SevkiyatKilitAcmaTipi)request.KilitAcmaTipiId;
            var eskiSandikDurum = sandik.DurumId;
            var eskiProjeDurum = proje.DurumId;
            var yeniSandikDurum = kilitAcmaTipi == SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc
                ? (int)SandikDurum.Sevkedildi
                : sandik.SevkOncesiDurumId ?? (int)SandikDurum.Kapandi;

            sandik.DurumId = yeniSandikDurum;
            if (kilitAcmaTipi == SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc)
                sandik.SevkiyatDuzeltmeAcikMi = true;
            else
            {
                sandik.SevkOncesiDurumId = null;
                sandik.SevkiyatDuzeltmeAcikMi = false;
            }
            sandikRepo.Update(sandik);

            if (kilitAcmaTipi == SevkiyatKilitAcmaTipi.SevkiyatGeriAlinarakAc)
            {
                var sevkiyatBaglari = (await sevkiyatSandikRepo.FindAsync(ss => ss.SandikId == sandik.Id)).ToList();
                foreach (var sevkiyatBag in sevkiyatBaglari)
                    sevkiyatSandikRepo.Remove(sevkiyatBag);
            }

            var sandiklar = (await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId)).ToList();
            var guncelSandik = sandiklar.FirstOrDefault(s => s.Id == sandik.Id);
            if (guncelSandik != null)
            {
                guncelSandik.DurumId = yeniSandikDurum;
                if (kilitAcmaTipi == SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc)
                    guncelSandik.SevkiyatDuzeltmeAcikMi = true;
                else
                {
                    guncelSandik.SevkOncesiDurumId = null;
                    guncelSandik.SevkiyatDuzeltmeAcikMi = false;
                }
            }

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
            projeRepo.Update(proje);

            await _unitOfWork.SaveChangesAsync();

            if (proje.ProjeTipiId == (int)ProjeTipi.Saha)
            {
                await _sahaTamamlamaService.SenkronizeKaynakProjelerBySahaSandikIdsAsync(
                    new[] { sandik.Id },
                    cancellationToken);
            }

            var aciklamaNotu = string.IsNullOrWhiteSpace(request.Aciklama)
                ? "Açıklama yok"
                : request.Aciklama.Trim();

            var islemAdi = kilitAcmaTipi == SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc
                ? "Sandık Kilidi Açıldı - Sevkiyat Kaydı Korundu"
                : "Sandık Kilidi Açıldı - Sevkiyat Geri Alındı";

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "Sandik",
                ReferansId = sandik.Id.ToString(),
                Islem = islemAdi,
                IslemTipiId = null,
                EskiDeger = $"SandikDurum:{eskiSandikDurum}, ProjeDurum:{eskiProjeDurum}",
                YeniDeger = $"SandikDurum:{sandik.DurumId}, ProjeDurum:{proje.DurumId}",
                Aciklama = $"Sandık {sandik.SandikNo} sevk kilidi açıldı. Mod: {GetKilitAcmaTipiMetni(kilitAcmaTipi)}. Not: {aciklamaNotu}"
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
