using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Application.Features.ProjeIslemleri.Services;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Saha satırlarını ya da bir saha sandığının bütün içeriğini başka bir saha projesine taşır. Engel varsa 409 ve
    /// madde listesi döner, hiçbir kayıt değişmez. Aynı <see cref="IslemAnahtari"/> ile gelen tekrar aynı sonucu alır;
    /// anahtar başka bir taşıma için kullanılmışsa 409 döner.
    /// </summary>
    public sealed class SahadanSahayaTasiCommand :
        IRequest<Result<SahadanSahayaTasimaSonucDto>>,
        ISecuredRequest,
        IRequiresMenuPermission
    {
        public string RequiredMenuKod => YetkiKodlari.Saha.SahayaAktar;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public Guid IslemAnahtari { get; set; }
        public int KaynakSahaProjeId { get; set; }
        public List<int> SahaCekiSatiriIds { get; set; } = [];
        public int? KaynakSandikId { get; set; }
        public int HedefSahaProjeId { get; set; }
        public int? HedefSandikId { get; set; }
        public string? YeniSandikNo { get; set; }
        public string? Aciklama { get; set; }
    }

    public sealed class SahadanSahayaTasiCommandHandler(
        IUnitOfWork unitOfWork,
        IHareketService hareketService,
        ICurrentUserService currentUserService,
        ISahaTamamlamaService sahaTamamlamaService) :
        IRequestHandler<SahadanSahayaTasiCommand, Result<SahadanSahayaTasimaSonucDto>>
    {
        public Task<Result<SahadanSahayaTasimaSonucDto>> Handle(
            SahadanSahayaTasiCommand request,
            CancellationToken cancellationToken) =>
            unitOfWork.ExecuteAtomicAsync(token => TasiAsync(request, token), cancellationToken);

        private async Task<Result<SahadanSahayaTasimaSonucDto>> TasiAsync(
            SahadanSahayaTasiCommand request,
            CancellationToken cancellationToken)
        {
            var oncekiSonuc = await OncekiSonucAsync(request);
            if (oncekiSonuc != null)
                return oncekiSonuc;

            var islem = new SahadanSahayaTasimaIslemi(unitOfWork, hareketService, currentUserService, sahaTamamlamaService);
            var hazirlik = await SahadanSahayaTasimaIslemi.HazirlaAsync(unitOfWork, new SahadanSahayaTasimaTalebi(
                request.KaynakSahaProjeId,
                request.SahaCekiSatiriIds,
                request.KaynakSandikId,
                request.HedefSahaProjeId,
                request.HedefSandikId,
                request.YeniSandikNo), cancellationToken);
            if (!hazirlik.IsSuccess)
                return SahaAktarimSonucEslemesi.Basarisiz<SahadanSahayaTasimaSonucDto>(hazirlik);

            var h = hazirlik.Value!;
            if (h.Engeller.Count > 0)
            {
                return Result<SahadanSahayaTasimaSonucDto>.Failure(
                    "Taşıma yapılamadı; hiçbir kayıt değiştirilmedi.",
                    409,
                    h.Engeller.ToList());
            }

            if (h.Satirlar.Count == 0 && h.ManuelIcerikler.Count == 0)
                return Result<SahadanSahayaTasimaSonucDto>.Failure("Taşınacak ürün yok.");

            var sonuc = await islem.UygulaAsync(h, request.IslemAnahtari, request.Aciklama, cancellationToken);
            return Result<SahadanSahayaTasimaSonucDto>.Success(new SahadanSahayaTasimaSonucDto
            {
                SahaAktarimId = sonuc.Baslik.Id,
                HedefSahaProjeId = h.HedefProje.Id,
                HedefSahaProjeNo = h.HedefProje.ProjeNo,
                HedefSandikId = sonuc.HedefSandik.Id,
                HedefSandikNo = sonuc.HedefSandik.SandikNo,
                TasinanSatirSayisi = sonuc.TasinanSatirSayisi,
                TasinanManuelIcerikSayisi = sonuc.TasinanManuelIcerikSayisi,
                ToplamMiktar = sonuc.ToplamMiktar,
                BosalanSandikNolari = sonuc.BosalanSandikNolari.ToList()
            });
        }

        /// <summary>
        /// Aynı anahtarlı taşıma yazılmışsa: aynı istekse (kaynak, hedef ve satırlar/sandık aynı) önceki sonuç döner,
        /// farklıysa 409. Eşzamanlı iki istekte benzersiz indeks ikincisini reddeder.
        /// </summary>
        private async Task<Result<SahadanSahayaTasimaSonucDto>?> OncekiSonucAsync(SahadanSahayaTasiCommand request)
        {
            var anahtar = request.IslemAnahtari;
            var baslik = (await unitOfWork.GetRepository<SahaAktarim>().FindAsync(a => a.IslemAnahtari == anahtar))
                .SingleOrDefault();
            if (baslik == null)
                return null;

            var izler = (await unitOfWork.GetRepository<SahaTasimaKalemi>().FindAsync(t => t.SahaAktarimId == baslik.Id)).ToList();
            var satirlarAyni = request.KaynakSandikId.HasValue
                ? izler.All(t => t.KaynakSahaSandikId == request.KaynakSandikId)
                : izler.Select(t => t.SahaCekiSatiriId ?? 0).ToHashSet().SetEquals(request.SahaCekiSatiriIds);
            // Mevcut sandık seçildiyse taşımanın gittiği sandık da aynı olmalı (iz satırından okunur).
            var hedefSandikAyni = !request.HedefSandikId.HasValue ||
                                  izler.All(t => t.HedefSahaSandikId == request.HedefSandikId);
            if (baslik.KaynakSahaProjeId != request.KaynakSahaProjeId ||
                baslik.SahaProjeId != request.HedefSahaProjeId ||
                !satirlarAyni ||
                !hedefSandikAyni)
            {
                return Result<SahadanSahayaTasimaSonucDto>.Failure(
                    "Bu işlem anahtarı başka bir taşıma için kullanılmış; ekranı yenileyip tekrar deneyin.",
                    409);
            }

            var hedefSandikId = izler.Select(t => t.HedefSahaSandikId).FirstOrDefault(id => id.HasValue);
            var hedefProje = await unitOfWork.GetRepository<Proje>().GetByIdAsync(baslik.SahaProjeId);
            return Result<SahadanSahayaTasimaSonucDto>.Success(new SahadanSahayaTasimaSonucDto
            {
                SahaAktarimId = baslik.Id,
                HedefSahaProjeId = baslik.SahaProjeId,
                HedefSahaProjeNo = hedefProje?.ProjeNo ?? string.Empty,
                HedefSandikId = hedefSandikId ?? 0,
                HedefSandikNo = izler.Select(t => t.HedefSahaSandikNo).FirstOrDefault() ?? string.Empty,
                TasinanSatirSayisi = izler.Count,
                ToplamMiktar = izler.Sum(t => t.Miktar)
            });
        }
    }
}
