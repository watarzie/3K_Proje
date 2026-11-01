using System.Globalization;
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
    /// Önizlemesi onaylanan otomatik sahaya aktarımı tek transaction'da uygular. Plan sunucuda yeniden kurulur;
    /// <see cref="PlanOzeti"/> önizlemedekiyle aynı değilse hiçbir kayıt yazılmadan 409 döner.
    /// </summary>
    public sealed class OtomatikSahayaAktarCommand :
        IRequest<Result<OtomatikSahaAktarimSonucDto>>,
        ISecuredRequest,
        IRequiresMenuPermission
    {
        public string RequiredMenuKod => YetkiKodlari.Saha.SahayaAktar;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;

        public int ProjeId { get; set; }
        public int? HedefSahaProjeId { get; set; }
        public bool YeniProje { get; set; }
        public string? YeniProjeNo { get; set; }
        public string? Aciklama { get; set; }
        public string PlanOzeti { get; set; } = string.Empty;
    }

    public sealed class OtomatikSahayaAktarCommandHandler(
        IUnitOfWork unitOfWork,
        ISandikService sandikService,
        ISahaTamamlamaService sahaTamamlamaService,
        IHareketService hareketService,
        ILookupCacheService lookupCache,
        ICurrentUserService currentUserService) :
        IRequestHandler<OtomatikSahayaAktarCommand, Result<OtomatikSahaAktarimSonucDto>>
    {
        /// <summary>Ön yüz bu kodu görünce önizlemeyi yeniler.</summary>
        public const string PlanDegistiKodu = "PLAN_DEGISTI";

        public Task<Result<OtomatikSahaAktarimSonucDto>> Handle(
            OtomatikSahayaAktarCommand request,
            CancellationToken cancellationToken) =>
            unitOfWork.ExecuteAtomicAsync(token => UygulaAsync(request, token), cancellationToken);

        private async Task<Result<OtomatikSahaAktarimSonucDto>> UygulaAsync(
            OtomatikSahayaAktarCommand request,
            CancellationToken cancellationToken)
        {
            var hazirlik = await new OtomatikSahaAktarimHazirlayici(unitOfWork, sandikService, sahaTamamlamaService)
                .HazirlaAsync(request.ProjeId, request.HedefSahaProjeId, request.YeniProje, cancellationToken);
            if (!hazirlik.IsSuccess)
                return SahaAktarimSonucEslemesi.Basarisiz<OtomatikSahaAktarimSonucDto>(hazirlik);

            var h = hazirlik.Value!;
            var plan = h.Plan;

            // Aynı isteğin tekrarı (ağ tekrarı, çift tıklama) burada durur: ilk uygulama kalanları planladığı için
            // yeniden kurulan plan boştur ya da özeti farklıdır; ikinci kez kayıt yazılmaz.
            if (plan.BosMu)
            {
                return Result<OtomatikSahaAktarimSonucDto>.Failure(
                    "Aktarılacak kalem kalmadı. Önizlemeyi yenileyin.",
                    409,
                    new { kod = PlanDegistiKodu });
            }

            if (!string.Equals(h.OnizlemeOzeti, request.PlanOzeti?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Result<OtomatikSahaAktarimSonucDto>.Failure(
                    "Proje verisi önizlemeden sonra değişti. Önizlemeyi yenileyip tekrar deneyin.",
                    409,
                    new { kod = PlanDegistiKodu });
            }

            if (h.HedefSecimiGerekli)
            {
                return Result<OtomatikSahaAktarimSonucDto>.Failure(
                    "Projeye bağlı birden çok saha projesi var; hedef saha projesini seçin.");
            }

            var toplamEksikAdet = plan.EksikKalemleri.Sum(e => e.Miktar);
            var ozet = $"{SahaAktarimConstants.OtomatikAktarimAciklamaPrefix} {plan.SandikAktarimlari.Count} sandık, " +
                       $"{plan.EksikKalemleri.Count} eksik satır ({FormatAdet(toplamEksikAdet)} adet)";
            var aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                ? $"{ozet}. Kaynak proje: {h.Proje.ProjeNo}"
                : $"{ozet}. {request.Aciklama.Trim()}";

            var aktarim = await new SahaAktarimIslemi(
                    unitOfWork,
                    hareketService,
                    lookupCache,
                    currentUserService,
                    sahaTamamlamaService)
                .UygulaAsync(new EksiklerdenSahaProjesiOlusturCommand
                {
                    KaynakProjeId = h.Proje.Id,
                    HedefSahaProjeId = h.HedefSahaProje?.Id,
                    ProjeNo = h.YeniSahaProjesi && !string.IsNullOrWhiteSpace(request.YeniProjeNo)
                        ? request.YeniProjeNo.Trim()
                        : null,
                    Aciklama = aciklama,
                    Sandiklar = SandikTaslaklari(h)
                }, cancellationToken);

            if (!aktarim.IsSuccess)
                return SahaAktarimSonucEslemesi.Basarisiz<OtomatikSahaAktarimSonucDto>(aktarim);

            var sonuc = aktarim.Value!;
            await hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = h.Proje.Id,
                ReferansTipi = "Proje",
                ReferansId = h.Proje.Id.ToString(CultureInfo.InvariantCulture),
                Islem = "Otomatik Sahaya Aktarım",
                IslemTipiId = null,
                KullaniciId = currentUserService.UserId ?? 0,
                YeniDeger = $"SahaProje:{sonuc.Proje.ProjeNo}, Aktarım:{sonuc.SahaAktarimId}",
                Aciklama = $"{plan.SandikAktarimlari.Count} sevk edilmemiş sandık ve {plan.EksikKalemleri.Count} eksik satır " +
                           $"({FormatAdet(toplamEksikAdet)} adet) {sonuc.Proje.ProjeNo} saha projesine aktarıldı; " +
                           $"{plan.Atlananlar.Count} kalem ana projede kaldı."
            });

            return Result<OtomatikSahaAktarimSonucDto>.Success(new OtomatikSahaAktarimSonucDto
            {
                SahaProjeId = sonuc.Proje.Id,
                SahaProjeNo = sonuc.Proje.ProjeNo,
                SahaAktarimId = sonuc.SahaAktarimId,
                AktarilanSandikSayisi = plan.SandikAktarimlari.Count,
                EksikSatirSayisi = plan.EksikKalemleri.Count,
                ToplamSatirSayisi = sonuc.SatirSayisi,
                ToplamAdet = sonuc.ToplamAdet
            });
        }

        /// <summary>
        /// Önizlemedeki sırayla taslaklar: önce eksikler sandığı, sonra sevk edilmemiş sandıklar. Sandık ürünleri elle
        /// sandık aktarımının açıklama ön ekini taşır; saha tamamlama servisi sandık bazlı aktarımı bu tiple tanır.
        /// </summary>
        private static List<EksikSahaSandikDto> SandikTaslaklari(OtomatikSahaAktarimHazirligi h)
        {
            var taslaklar = new List<EksikSahaSandikDto>();

            if (h.EksiklerHedefi != null && h.Plan.EksikKalemleri.Count > 0)
            {
                taslaklar.Add(new EksikSahaSandikDto
                {
                    HedefSandikId = h.EksiklerHedefi.HedefSandikId,
                    SandikNo = h.EksiklerHedefi.SandikNo,
                    SandikIsmi = h.EksiklerHedefi.HedefSandikId.HasValue ? null : SahaAktarimConstants.EksiklerSandikAdi,
                    Urunler = h.Plan.EksikKalemleri
                        .Select(e => new EksikSahaUrunDto
                        {
                            CekiSatiriId = e.Satir.Id,
                            KaynakProjeId = h.Proje.Id,
                            KaynakSandikId = e.KaynakSandikId,
                            Miktar = e.Miktar,
                            Aciklama = string.IsNullOrWhiteSpace(e.KaynakSandikNo)
                                ? SahaAktarimConstants.SevkSonrasiEksikAciklamaPrefix.TrimEnd(':')
                                : $"{SahaAktarimConstants.SevkSonrasiEksikAciklamaPrefix} sandık {e.KaynakSandikNo}"
                        })
                        .ToList()
                });
            }

            foreach (var sandikAktarimi in h.Plan.SandikAktarimlari)
            {
                var kaynak = sandikAktarimi.KaynakSandik;
                var hedef = h.SandikHedefleri[kaynak.Id];
                taslaklar.Add(new EksikSahaSandikDto
                {
                    HedefSandikId = hedef.HedefSandikId,
                    SandikNo = hedef.SandikNo,
                    SandikIsmi = kaynak.Ad,
                    En = kaynak.En,
                    Boy = kaynak.Boy,
                    Yukseklik = kaynak.Yukseklik,
                    NetKg = kaynak.NetKg,
                    GrossKg = kaynak.GrossKg,
                    Urunler = sandikAktarimi.Urunler
                        .Select(u => new EksikSahaUrunDto
                        {
                            CekiSatiriId = u.Satir.Id,
                            KaynakProjeId = h.Proje.Id,
                            KaynakSandikId = kaynak.Id,
                            Miktar = u.Miktar,
                            Aciklama = $"{SahaAktarimConstants.SandikBazliAktarimAciklamaPrefix} {kaynak.SandikNo}"
                        })
                        .ToList()
                });
            }

            return taslaklar;
        }

        private static string FormatAdet(decimal value)
        {
            return decimal.Truncate(value) == value
                ? decimal.Truncate(value).ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.####", CultureInfo.GetCultureInfo("tr-TR"));
        }
    }
}
