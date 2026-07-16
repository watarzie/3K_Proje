using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class UrunGuncelleCommandHandler : IRequestHandler<UrunGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public UrunGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(UrunGuncelleCommand request, CancellationToken cancellationToken)
        {
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            CekiSatiri? urun = null;
            SandikIcerik? icerik = null;

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var hedefSandik = await sandikRepo.GetByIdAsync(request.SandikId);
            if (hedefSandik == null || hedefSandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);

            if (SandikSevkKilidiHelper.SandikKilitliMi(hedefSandik))
                return Result.Failure(SandikSevkKilidiHelper.SandikKilitliMesaji);

            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var hedefProje = await projeRepo.GetByIdAsync(hedefSandik.ProjeId);
            var isSahaYedek = hedefProje != null &&
                (hedefProje.ProjeTipiId == (int)ProjeTipi.Saha || hedefProje.ProjeTipiId == (int)ProjeTipi.Yedek);

            if (isSahaYedek)
            {
                return await SahaYedekIcerikGuncelle(request, sandikIcerikRepo, hedefSandik, cancellationToken);
            }

            // ===== Saha/Yedek (CekiSatiriId yok) → SandikIcerikId ile bul =====
            if (!request.CekiSatiriId.HasValue || request.CekiSatiriId.Value == 0)
            {
                if (request.SandikIcerikId.HasValue && request.SandikIcerikId.Value > 0)
                {
                    icerik = await sandikIcerikRepo.GetByIdAsync(request.SandikIcerikId.Value);
                }
                if (icerik == null)
                    return Result.Failure("Sandık içeriği bulunamadı.", 404);

                var manuelTahsis = icerik.TahsisMiktari > 0
                    ? icerik.TahsisMiktari
                    : Math.Max(icerik.Miktar, icerik.KonulanAdet);
                if (request.KonulanAdet is < 0)
                    return Result.Failure("Konulan adet 0'dan küçük olamaz.");
                if (request.KonulanAdet > manuelTahsis)
                    return Result.Failure($"Konulan adet bu sandığa tahsis edilen miktarı aşamaz. Maksimum: {manuelTahsis}");

                icerik.TahsisMiktari = manuelTahsis;

                // KonulanAdet güncelle
                if (request.KonulanAdet.HasValue) icerik.KonulanAdet = request.KonulanAdet.Value;
                if (request.EksikAdet.HasValue) icerik.EksikAdet = request.EksikAdet.Value;
                sandikIcerikRepo.Update(icerik);

                // Sandık durumu kontrolü (CekiSatiri olmadan basit kontrol)
                var sandik = hedefSandik;
                if (sandik != null)
                {
                    var tumIcerikler = await sandikIcerikRepo.FindAsync(si => si.SandikId == request.SandikId);
                    if (tumIcerikler.Any() && sandik.DurumId == (int)SandikDurum.Bos)
                    {
                        sandik.DurumId = (int)SandikDurum.Hazirlaniyor;
                        sandikRepo.Update(sandik);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    ReferansTipi = "SandikIcerik",
                    ReferansId = icerik.Id.ToString(),
                    Islem = "Ürün Adet Güncellendi",
                    IslemTipiId = (int)IslemTipi.UrunGuncellendi,
                    KullaniciId = request.KullaniciId,
                    Aciklama = $"Konulan: {icerik.KonulanAdet}, Eksik: {icerik.EksikAdet}"
                });

                return Result.Success();
            }

            // ===== Normal proje akışı (CekiSatiriId var) =====
            urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId.Value);
            if (urun == null) return Result.Failure("Ürün bulunamadı.", 404);

            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var ceki = await cekiRepo.GetByIdAsync(urun.CekiId);
            if (ceki == null || ceki.ProjeId != request.ProjeId)
                return Result.Failure("Ürün projeye ait değil.", 404);

            if (await SahaAktarimBlokajHelper.KaynakSatirAktarildiMiAsync(_sahaTamamlamaService, urun, cancellationToken))
                return Result.Failure(SahaAktarimBlokajHelper.SandikMesaji);

            var icerikler = await sandikIcerikRepo.FindAsync(si =>
                si.CekiSatiriId == request.CekiSatiriId.Value && si.SandikId == request.SandikId);
            icerik = icerikler.FirstOrDefault();

            if (icerik == null)
            {
                icerik = new SandikIcerik
                {
                    SandikId = request.SandikId,
                    CekiSatiriId = request.CekiSatiriId.Value,
                    TahsisMiktari = urun.IstenenAdet,
                    KonulanAdet = 0,
                    EksikAdet = 0
                };
                await sandikIcerikRepo.AddAsync(icerik);
            }

            if (icerik.TahsisMiktari <= 0)
                icerik.TahsisMiktari = Math.Max(urun.IstenenAdet, icerik.KonulanAdet);

            if (request.KonulanAdet is < 0)
                return Result.Failure("Konulan adet 0'dan küçük olamaz.");
            if (request.KonulanAdet > icerik.TahsisMiktari)
            {
                return Result.Failure(
                    $"Konulan adet bu sandığa tahsis edilen miktarı aşamaz. Maksimum: {icerik.TahsisMiktari}");
            }

            if (request.KonulanAdet.HasValue) icerik.KonulanAdet = request.KonulanAdet.Value;
            if (request.EksikAdet.HasValue) icerik.EksikAdet = request.EksikAdet.Value;
            sandikIcerikRepo.Update(icerik);

            if (request.PaketleyenId.HasValue) urun.PaketleyenId = request.PaketleyenId.Value;
            if (request.KontrolEdenId.HasValue) urun.KontrolEdenId = request.KontrolEdenId.Value;

            if (request.GridDurumuId.HasValue) urun.GridDurumuId = request.GridDurumuId.Value;
            if (request.UcKDurumuId.HasValue) urun.UcKDurumuId = request.UcKDurumuId.Value;

            if (!string.IsNullOrEmpty(request.Aciklama)) urun.Remarks = request.Aciklama;

            // Durum hesaplama (State Diagram) — int karşılaştırma
            if (urun.UcKDurumuId == (int)UcKDurum.TamGeldi || icerik.KonulanAdet >= urun.IstenenAdet)
                urun.DurumId = (int)UrunDurum.Tamamlandi;
            else if (urun.UcKDurumuId == (int)UcKDurum.EksikGeldi || icerik.EksikAdet > 0)
                urun.DurumId = (int)UrunDurum.Eksik;
            else if (urun.GridDurumuId == (int)GridDurum.TamGeldi || (icerik.KonulanAdet > 0 && icerik.KonulanAdet < urun.IstenenAdet))
                urun.DurumId = (int)UrunDurum.KismiGeldi;

            cekiSatiriRepo.Update(urun);

            // Sandık durumu ve lokasyon otomasyonu
            var sandikRepo2 = sandikRepo;
            var sandik2 = hedefSandik;
            if (sandik2 != null)
            {
                if (SandikDepoKurali.BelirsizLokasyonMu(sandik2.DepoLokasyonId) &&
                    request.UcKDurumuId.HasValue && request.UcKDurumuId.Value != (int)UcKDurum.Bekliyor)
                    sandik2.DepoLokasyonId = (int)DepoLokasyon.UcK;
                else if (SandikDepoKurali.BelirsizLokasyonMu(sandik2.DepoLokasyonId) &&
                         request.GridDurumuId.HasValue && request.GridDurumuId.Value != (int)GridDurum.Bekliyor)
                    sandik2.DepoLokasyonId = (int)DepoLokasyon.Grid;

                var tumIcerikler2 = await sandikIcerikRepo.FindAsync(si => si.SandikId == request.SandikId);
                var tumUrunIds = tumIcerikler2.Where(si => si.CekiSatiriId.HasValue).Select(si => si.CekiSatiriId!.Value).ToList();

                bool hepsiTamamlandi = true;
                foreach (var urunId in tumUrunIds)
                {
                    var u = await cekiSatiriRepo.GetByIdAsync(urunId);
                    if (u != null)
                    {
                        if (u.DurumId != (int)UrunDurum.Tamamlandi) hepsiTamamlandi = false;
                    }
                }

                var eskiDurumId = sandik2.DurumId;

                if (!tumIcerikler2.Any()) sandik2.DurumId = (int)SandikDurum.Bos;
                else if (hepsiTamamlandi && tumUrunIds.Count > 0) sandik2.DurumId = (int)SandikDurum.Kapandi;
                else sandik2.DurumId = (int)SandikDurum.Hazirlaniyor;

                sandikRepo2.Update(sandik2);

                if (eskiDurumId == (int)SandikDurum.Kapandi && sandik2.DurumId != (int)SandikDurum.Kapandi)
                {
                    var eskiDurumMetni = Enum.GetName(typeof(SandikDurum), eskiDurumId) ?? "Hazir";
                    var yeniDurumMetni = Enum.GetName(typeof(SandikDurum), sandik2.DurumId) ?? "Hazirlaniyor";

                    await _hareketService.HareketKaydetAsync(new HareketGecmisi
                    {
                        ProjeId = request.ProjeId,
                        ReferansTipi = "Sandik",
                        ReferansId = sandik2.Id.ToString(),
                        Islem = "Sandık Geri Açıldı",
                        IslemTipiId = null,
                        EskiDeger = eskiDurumMetni,
                        YeniDeger = yeniDurumMetni,
                        Aciklama = $"Ürün durumundaki değişiklik nedeniyle sandık tekrar '{yeniDurumMetni}' konumuna getirildi.",
                        KullaniciId = request.KullaniciId
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var durumMetni = Enum.GetName(typeof(UrunDurum), urun.DurumId) ?? urun.DurumId.ToString();
            var gridMetni = Enum.GetName(typeof(GridDurum), urun.GridDurumuId) ?? urun.GridDurumuId.ToString();
            var uckMetni = Enum.GetName(typeof(UcKDurum), urun.UcKDurumuId) ?? urun.UcKDurumuId.ToString();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "Ürün Güncellendi",
                IslemTipiId = (int)IslemTipi.UrunGuncellendi,
                KullaniciId = request.KullaniciId,
                Aciklama = $"Durum: {durumMetni}, Grid: {gridMetni}, 3K: {uckMetni}, Konulan: {icerik.KonulanAdet}, Eksik: {icerik.EksikAdet}"
            });

            return Result.Success();
        }

        private async Task<Result> SahaYedekIcerikGuncelle(
            UrunGuncelleCommand request,
            IGenericRepository<SandikIcerik> sandikIcerikRepo,
            Sandik sandik,
            CancellationToken cancellationToken)
        {
            if (SandikSevkKilidiHelper.SandikKilitliMi(sandik))
                return Result.Failure("Sevk edilmiş sandıkta ürün adedi güncellenemez.");

            if (!request.SandikIcerikId.HasValue || request.SandikIcerikId.Value <= 0)
                return Result.Failure("Saha/Yedek ürün güncellemesi için SandikIcerikId zorunludur.");

            var icerik = await sandikIcerikRepo.GetByIdAsync(request.SandikIcerikId.Value);
            if (icerik == null || icerik.SandikId != request.SandikId)
                return Result.Failure("Sandık içeriği bulunamadı.", 404);

            CekiSatiri? satir = null;
            var bagliIcerikler = new List<SandikIcerik>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();

            if (icerik.CekiSatiriId.HasValue)
            {
                satir = await cekiSatiriRepo.GetByIdAsync(icerik.CekiSatiriId.Value);
                if (satir == null)
                {
                    return Result.Failure(
                        "Ürünün bağlı çeki satırı bulunamadı. Veri bütünlüğü kontrol edilmeden güncelleme yapılamaz.",
                        409);
                }

                var ceki = await _unitOfWork.GetRepository<Ceki>().GetByIdAsync(satir.CekiId);
                if (ceki == null || ceki.ProjeId != request.ProjeId)
                    return Result.Failure("Ürünün bağlı çeki satırı bu projeye ait değil.", 409);

                bagliIcerikler = (await sandikIcerikRepo.FindAsync(i => i.CekiSatiriId == satir.Id)).ToList();
            }

            var tahsisKayitSayisi = bagliIcerikler.Any(i => i.Id == icerik.Id)
                ? bagliIcerikler.Count
                : bagliIcerikler.Count + 1;
            var tahsisMiktari = satir != null
                ? SandikTahsisHelper.HesaplaSandikMiktari(satir, icerik, tahsisKayitSayisi)
                : icerik.TahsisMiktari > 0
                    ? icerik.TahsisMiktari
                    : Math.Max(icerik.Miktar, icerik.KonulanAdet);

            if (tahsisMiktari <= 0)
                return Result.Failure("Ürünün güncellenebilir tahsis miktarı bulunamadı.", 409);

            icerik.TahsisMiktari = tahsisMiktari;
            // Saha/Yedek raporlarının gölge miktarı tahsisle aynı kalır. Bu değer fiziksel
            // KonulanAdet değildir ve parçalı sandık taşımasında ana istenen miktara dönmez.
            icerik.Miktar = tahsisMiktari;

            var gridKapandiIstendi = request.GridDurumuId == (int)GridDurum.GridKapandi;

            if (gridKapandiIstendi)
            {
                icerik.KonulanAdet = tahsisMiktari;
                icerik.EksikAdet = 0;
            }
            else if (request.KonulanAdet.HasValue)
            {
                if (request.KonulanAdet.Value < 0)
                    return Result.Failure("Konulan adet 0'dan küçük olamaz.");

                if (request.KonulanAdet.Value > tahsisMiktari)
                    return Result.Failure($"Konulan adet tahsis miktarından büyük olamaz. Maksimum: {tahsisMiktari}");

                icerik.KonulanAdet = request.KonulanAdet.Value;
            }

            if (!gridKapandiIstendi && request.EksikAdet.HasValue)
            {
                if (request.EksikAdet.Value < 0)
                    return Result.Failure("Eksik adet 0'dan küçük olamaz.");
                if (request.EksikAdet.Value > tahsisMiktari)
                    return Result.Failure($"Eksik adet tahsis miktarından büyük olamaz. Maksimum: {tahsisMiktari}");

                icerik.EksikAdet = request.EksikAdet.Value;
            }

            sandikIcerikRepo.Update(icerik);

            var kaynakCekiSatiriId = satir?.KaynakCekiSatiriId;
            if (satir != null)
            {
                // FindAsync AsNoTracking döndürdüğünden listedeki mevcut kayıt request'ten
                // önceki değeri taşıyabilir. Güncellenen Id'yi dışlayıp tracked icerik değerini
                // ekleyerek CekiSatiri toplamının bir işlem geriden gelmesini önleriz.
                var toplamKonulan = bagliIcerikler
                    .Where(i => i.Id != icerik.Id)
                    .Sum(i => Math.Max(i.KonulanAdet, 0))
                    + Math.Max(icerik.KonulanAdet, 0);

                if (gridKapandiIstendi)
                {
                    satir.DurumId = (int)UrunDurum.Tamamlandi;
                    satir.GridDurumuId = (int)GridDurum.GridKapandi;
                    satir.GridGelenAdet = 0;
                    satir.TrafoSevkAdet = 0;
                    satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi;
                    satir.GridSevkMiktari = null;
                    satir.GelenMiktar = 0;
                    satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
                    satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
                }
                else
                {
                    satir.GelenMiktar = toplamKonulan;
                    satir.GridGelenAdet = toplamKonulan;
                    satir.GridSevkMiktari = toplamKonulan;

                    if (toplamKonulan >= satir.IstenenAdet)
                    {
                        satir.DurumId = (int)UrunDurum.Tamamlandi;
                        satir.GridDurumuId = (int)GridDurum.TamGeldi;
                        satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
                        satir.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
                    }
                    else if (toplamKonulan > 0)
                    {
                        satir.DurumId = (int)UrunDurum.KismiGeldi;
                        satir.GridDurumuId = (int)GridDurum.EksikGeldi;
                        satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
                        satir.UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi;
                    }
                    else
                    {
                        satir.DurumId = (int)UrunDurum.Bekliyor;
                        satir.GridDurumuId = (int)GridDurum.Gelmedi;
                        satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
                        satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
                    }
                }

                cekiSatiriRepo.Update(satir);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (kaynakCekiSatiriId.HasValue)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(new[] { kaynakCekiSatiriId.Value }, cancellationToken);

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "SandikIcerik",
                ReferansId = icerik.Id.ToString(),
                Islem = "Saha/Yedek Ürün Adet Güncellendi",
                IslemTipiId = (int)IslemTipi.UrunGuncellendi,
                KullaniciId = request.KullaniciId,
                Aciklama = $"Konulan: {icerik.KonulanAdet}, Eksik: {icerik.EksikAdet}, Kaynak: {icerik.KaynakProjeNo ?? "-"}"
            });

            return Result.Success();
        }
    }
}
