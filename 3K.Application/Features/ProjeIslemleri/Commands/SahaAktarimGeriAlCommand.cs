using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class SahaAktarimGeriAlCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => "saha-aktarim-geri-al";
        public int SahaCekiSatiriId { get; set; }
        public string? Aciklama { get; set; }
    }

    public class SahaAktarimGeriAlCommandHandler : IRequestHandler<SahaAktarimGeriAlCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public SahaAktarimGeriAlCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IHareketService hareketService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _hareketService = hareketService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(SahaAktarimGeriAlCommand request, CancellationToken cancellationToken)
        {
            if (request.SahaCekiSatiriId <= 0)
                return Result.Failure("Geri alınacak saha aktarım satırı seçilmelidir.", 400);

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();

            var sahaSatir = await cekiSatiriRepo.GetByIdAsync(request.SahaCekiSatiriId);
            if (sahaSatir == null)
                return Result.Failure("Saha aktarım satırı bulunamadı.", 404);

            var sahaCeki = await cekiRepo.GetByIdAsync(sahaSatir.CekiId);
            if (sahaCeki == null)
                return Result.Failure("Saha aktarım çeki bulunamadı.", 404);

            var sahaProje = await projeRepo.GetByIdAsync(sahaCeki.ProjeId);
            if (sahaProje == null)
                return Result.Failure("Saha projesi bulunamadı.", 404);

            if (!sahaSatir.KaynakCekiSatiriId.HasValue || sahaProje.ProjeTipiId != (int)ProjeTipi.Saha)
                return Result.Failure("Seçilen satır bir saha aktarım satırı değil.", 400);

            var kaynakSatir = await cekiSatiriRepo.GetByIdAsync(sahaSatir.KaynakCekiSatiriId.Value);
            if (kaynakSatir == null)
                return Result.Failure("Aktarımın bağlı olduğu kaynak ürün bulunamadı.", 404);

            var kaynakCeki = await cekiRepo.GetByIdAsync(kaynakSatir.CekiId);
            if (kaynakCeki == null)
                return Result.Failure("Kaynak çeki bulunamadı.", 404);

            var kaynakProje = await projeRepo.GetByIdAsync(kaynakCeki.ProjeId);
            if (kaynakProje == null)
                return Result.Failure("Kaynak proje bulunamadı.", 404);

            var sahaIcerikleri = (await sandikIcerikRepo.FindAsync(si => si.CekiSatiriId == sahaSatir.Id)).ToList();
            var sahaSandikIds = sahaIcerikleri
                .Select(si => si.SandikId)
                .Distinct()
                .ToList();
            var sandiklar = (await sandikRepo.FindAsync(s => sahaSandikIds.Contains(s.Id))).ToList();

            if (sandiklar.Any(s => s.DurumId == (int)SandikDurum.Sevkedildi))
                return Result.Failure("Bu aktarım sevk edilmiş bir saha sandığında olduğu için geri alınamaz.", 400);

            if (SahaSatiriIslemGormusMu(sahaSatir, sahaIcerikleri))
            {
                return Result.Failure(
                    "Bu saha aktarımında işlem başladığı için geri alınamaz. Önce saha projesindeki Grid/3K işlemlerini geri alın.",
                    400);
            }

            var silinenIcerikIds = sahaIcerikleri.Select(si => si.Id).ToHashSet();
            var silinenAdet = sahaSatir.IstenenAdet;
            var kaynakSatirId = kaynakSatir.Id;
            var kaynakUrunMetni = string.IsNullOrWhiteSpace(kaynakSatir.Aciklama)
                ? kaynakSatir.BarkodNo
                : kaynakSatir.Aciklama;

            foreach (var icerik in sahaIcerikleri)
                sandikIcerikRepo.Remove(icerik);

            cekiSatiriRepo.Remove(sahaSatir);

            foreach (var sandik in sandiklar)
            {
                var kalanIcerikVar = (await sandikIcerikRepo.FindAsync(si =>
                    si.SandikId == sandik.Id &&
                    !silinenIcerikIds.Contains(si.Id))).Any();

                if (!kalanIcerikVar && sandik.DurumId != (int)SandikDurum.Sevkedildi)
                {
                    sandik.DurumId = (int)SandikDurum.Bos;
                    sandikRepo.Update(sandik);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(new[] { kaynakSatirId }, cancellationToken);

            var aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                ? "Kullanıcı tarafından saha aktarımı geri alındı."
                : request.Aciklama.Trim();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = sahaProje.Id,
                ReferansTipi = "CekiSatiri",
                ReferansId = request.SahaCekiSatiriId.ToString(),
                Islem = "Saha Aktarımı Geri Alındı",
                IslemTipiId = null,
                KullaniciId = _currentUserService.UserId ?? 0,
                EskiDeger = $"SahaProje:{sahaProje.ProjeNo}, Kaynak:{kaynakProje.ProjeNo}, Miktar:{FormatAdet(silinenAdet)}",
                YeniDeger = "Aktarım kaldırıldı",
                Aciklama = $"{kaynakProje.ProjeNo} kaynak projesindeki #{kaynakSatir.SiraNo} - {kaynakUrunMetni} için {FormatAdet(silinenAdet)} adetlik saha aktarımı geri alındı. {aciklama}".Trim()
            });

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = kaynakProje.Id,
                ReferansTipi = "CekiSatiri",
                ReferansId = kaynakSatirId.ToString(),
                Islem = "Saha Aktarımı Geri Alındı",
                IslemTipiId = null,
                KullaniciId = _currentUserService.UserId ?? 0,
                EskiDeger = $"SahaProje:{sahaProje.ProjeNo}, Miktar:{FormatAdet(silinenAdet)}",
                YeniDeger = "Kaynak eksik takibine geri döndü",
                Aciklama = $"{sahaProje.ProjeNo} saha projesindeki aktarım geri alındı. Kaynak ürün tekrar normal proje eksik takibine dahil edildi. {aciklama}".Trim()
            });

            return Result.Success();
        }

        private static bool SahaSatiriIslemGormusMu(CekiSatiri satir, IEnumerable<SandikIcerik> sahaIcerikleri)
        {
            return satir.GridDurumuId != (int)GridDurum.Gelmedi ||
                satir.GridGelenAdet > 0 ||
                satir.TrafoSevkAdet > 0 ||
                satir.GridSevkDurumuId != (int)GridSevkDurum.SevkEdilmedi ||
                (satir.GridSevkMiktari ?? 0) > 0 ||
                satir.YenidenSevkGerekliAdet > 0 ||
                satir.GelenMiktar > 0 ||
                satir.KarsilananMiktar > 0 ||
                satir.StokKarsilanan > 0 ||
                satir.ProjeKarsilanan > 0 ||
                satir.ProjeGonderilen > 0 ||
                satir.TedarikciKarsilanan > 0 ||
                satir.HataliMiktar > 0 ||
                satir.GeriGonderilenMiktar > 0 ||
                satir.UcKDurumuId != (int)UcKDurum.Bekliyor ||
                satir.UcKKarsilamaTipiId != (int)UcKDurum.Bekliyor ||
                sahaIcerikleri.Any(si =>
                    si.KonulanAdet > 0 ||
                    si.EksikAdet > 0 ||
                    si.StokKarsilanan > 0 ||
                    si.ProjeKarsilanan > 0 ||
                    si.TedarikciKarsilanan > 0);
        }

        private static string FormatAdet(decimal value)
        {
            return decimal.Truncate(value) == value
                ? decimal.Truncate(value).ToString("0")
                : value.ToString("0.####");
        }
    }
}
