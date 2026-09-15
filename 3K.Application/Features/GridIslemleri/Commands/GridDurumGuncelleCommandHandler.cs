using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    public class GridDurumGuncelleCommandHandler : IRequestHandler<GridDurumGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        private static readonly int[] GecerliDurumlar =
            { (int)GridDurum.TamGeldi, (int)GridDurum.EksikGeldi, (int)GridDurum.Gelmedi, (int)GridDurum.TrafoSevk, (int)GridDurum.Iptal, (int)GridDurum.Sipariste, (int)GridDurum.Bekliyor, (int)GridDurum.GridKapandi };

        public GridDurumGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(GridDurumGuncelleCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionCancellationToken => HandleInTransactionAsync(request, transactionCancellationToken),
                cancellationToken);
        }

        private async Task<Result> HandleInTransactionAsync(
            GridDurumGuncelleCommand request,
            CancellationToken cancellationToken)
        {
            // Validate durum
            if (!GecerliDurumlar.Contains(request.YeniDurumId))
                return Result.Failure($"Geçersiz durum: {request.YeniDurumId}");

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            if (!satir.KaynakCekiSatiriId.HasValue &&
                await _sahaTamamlamaService.AktifTamamlamaVarMiAsync(satir.Id, cancellationToken))
                return Result.Failure("Bu ürün sahaya aktarıldığı için normal proje üzerinden Grid işlemi yapılamaz. İşlem saha projesinde yürütülmelidir.");

            if (await SandikSevkKilidiHelper.CekiSatiriSevkEdilmisSandiktaMiAsync(_unitOfWork, satir))
                return Result.Failure(SandikSevkKilidiHelper.UrunKilitliMesaji);

            // ===== 3K İşlem Blokajı =====
            // 3K tarafında işlem yapılmışsa Grid artık durum değiştiremez.
            // Önce 3K durumunun sıfırlanması gerekir.
            var devamSevkKarari = GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(satir);
            var requestDurumuSevkAkisiylaUyumlu =
                request.YeniDurumId == (int)GridDurum.TamGeldi ||
                request.YeniDurumId == (int)GridDurum.EksikGeldi ||
                request.YeniDurumId == (int)GridDurum.TrafoSevk;
            var devamSevkAkisi =
                devamSevkKarari.YeniPartiMi &&
                requestDurumuSevkAkisiylaUyumlu &&
                request.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi;

            // 3K işlemi başlamadıysa sevk miktarı mevcut toplamın üzerine yazılır.
            // Yalnız teslim sonrası devam sevkleri yeni parti olarak değerlendirilir.
            if (!devamSevkAkisi && GridUcKSevkPartisiKurali.UcKTarafindaIslemVar(satir))
            {
                return Result.Failure("Bu ürün için 3K tarafında işlem yapılmış. Grid durumu değiştirilemez. Önce 3K durumunu sıfırlayın.");
            }

            // ===== Kalite Tadilatta Blokajı =====
            if (satir.KaliteDurumId.HasValue)
            {
                var kaliteMetni = _lookupCache.GetDeger<LookupKaliteDurum>(satir.KaliteDurumId.Value);
                if (kaliteMetni == "Tadilatta")
                    return Result.Failure("Bu ürün Kalite tarafından 'Tadilatta' olarak işaretlenmiş. Grid durumu değiştirilemez.");
            }

            if (request.SevkMiktari.HasValue &&
                request.SevkMiktari.Value > 0 &&
                request.GridSevkDurumuId != (int)GridSevkDurum.SevkEdildi)
            {
                return Result.Failure("Sevk adeti girildiyse Grid sevk durumu 'Sevk Edildi' seçilmelidir.");
            }

            var eskiDurum = satir.GridDurumuId;

            // Devam sevkinde istemci sandık bazlı bir satırdan gelebilir. Bu satırdaki
            // GridGelen/Trafo değerleri parent CekiSatiri toplamı değildir; mevcut
            // kabul durumunu ve toplamlarını yeniden yazmak parent veriyi daraltır.
            // Devam kararı backend'de parent kayıt üzerinden verildiği için burada
            // yalnız yeni aktif sevk partisi açılır.
            satir.GridPersonelId = _currentUserService.UserId;
            satir.GridAciklama = request.Aciklama;

            if (!devamSevkAkisi)
            {
                satir.GridDurumuId = request.YeniDurumId;

                // ===== Durum bazlı alan yönetimi =====
                switch (request.YeniDurumId)
                {
                    case (int)GridDurum.TamGeldi:
                        satir.GridGelenAdet = satir.IstenenAdet; // otomatik
                        satir.TrafoSevkAdet = 0;
                        break;

                    case (int)GridDurum.EksikGeldi:
                        if (request.GridGelenAdet == null || request.GridGelenAdet <= 0)
                            return Result.Failure("Eksik geldi durumunda gelen adet girilmelidir.");
                        if (request.GridGelenAdet >= satir.IstenenAdet)
                            return Result.Failure("Eksik geldi durumunda gelen adet miktardan küçük olmalıdır.");
                        satir.GridGelenAdet = request.GridGelenAdet.Value;
                        satir.TrafoSevkAdet = 0;
                        break;

                    case (int)GridDurum.Gelmedi:
                        satir.GridGelenAdet = 0;
                        satir.TrafoSevkAdet = 0;
                        satir.GridSevkDurumuId = 3; // SevkEdilmedi
                        satir.GridSevkMiktari = null;
                        await GridUcKSevkPartisiKurali.AktifPartiTakibiniTemizleAsync(_unitOfWork, satir);
                        break;

                    case (int)GridDurum.TrafoSevk:
                        if (request.TrafoSevkAdet == null || request.TrafoSevkAdet <= 0)
                            return Result.Failure("Trafo sevk durumunda trafo sevk adeti girilmelidir.");
                        if (request.TrafoSevkAdet > satir.IstenenAdet)
                            return Result.Failure("Trafo sevk adeti miktardan büyük olamaz.");
                        satir.TrafoSevkAdet = request.TrafoSevkAdet.Value;
                        satir.GridGelenAdet = request.GridGelenAdet ?? 0;
                        // Toplam kontrol
                        if (satir.GridGelenAdet + satir.TrafoSevkAdet > satir.IstenenAdet)
                            return Result.Failure("Toplam adet, çeki miktarını aşamaz.");
                        break;

                    case (int)GridDurum.Iptal:
                        satir.GridGelenAdet = 0;
                        satir.TrafoSevkAdet = 0;
                        satir.GridSevkDurumuId = 3; // SevkEdilmedi
                        satir.GridSevkMiktari = null;
                        await GridUcKSevkPartisiKurali.AktifPartiTakibiniTemizleAsync(_unitOfWork, satir);
                        break;

                    case (int)GridDurum.Sipariste:
                        satir.GridGelenAdet = 0;
                        satir.TrafoSevkAdet = 0;
                        satir.GridSevkDurumuId = 3; // SevkEdilmedi
                        satir.GridSevkMiktari = null;
                        await GridUcKSevkPartisiKurali.AktifPartiTakibiniTemizleAsync(_unitOfWork, satir);
                        break;

                    case (int)GridDurum.GridKapandi:
                        // Grid kapandı — mevcut adetler korunur, sadece durum güncellenir
                        break;
                }

                if (request.YeniDurumId == (int)GridDurum.GridKapandi)
                {
                    await SandigiGridLokasyonunaAlAsync(request.ProjeId, satir);
                }
            }

            // ===== Grid Sevk Durumu =====
            IReadOnlyCollection<SandikIcerik>? sevkIcerikleri = null;
            if (request.GridSevkDurumuId.HasValue)
            {
                // Sevk edilebilmesi kontrolü 
                if (request.GridSevkDurumuId.Value == (int)GridSevkDurum.SevkEdildi)
                {
                    var sevkeUygunDurum = devamSevkAkisi ||
                        request.YeniDurumId == (int)GridDurum.TamGeldi ||
                        request.YeniDurumId == (int)GridDurum.EksikGeldi ||
                        (request.YeniDurumId == (int)GridDurum.TrafoSevk && satir.GridGelenAdet > 0);
                    if (!sevkeUygunDurum)
                        return Result.Failure("Sevk edilmesi için durum TamGeldi veya EksikGeldi olmalıdır.");
                    var sevkUstSinir = devamSevkAkisi
                        ? devamSevkKarari.UstSinir
                        : satir.GridGelenAdet;
                    if (request.SevkMiktari > sevkUstSinir)
                        return Result.Failure("Sevk miktari Grid'e gelen adetten buyuk olamaz.");
                    if (request.SevkMiktari == null || request.SevkMiktari <= 0)
                        return Result.Failure("Sevk miktarı girilmelidir.");

                    sevkIcerikleri = (await _unitOfWork.GetRepository<SandikIcerik>()
                            .FindAsync(i => i.CekiSatiriId == satir.Id))
                        .ToList();
                    var tahsisKapasitesiResult = GridUcKSevkPartisiKurali.YeniSevkTahsisKapasitesiniDogrula(
                        satir,
                        sevkIcerikleri,
                        request.SevkMiktari.Value);
                    if (!tahsisKapasitesiResult.IsSuccess)
                    {
                        return Result.Failure(
                            tahsisKapasitesiResult.Error!.Message,
                            tahsisKapasitesiResult.StatusCode);
                    }
                }

                satir.GridSevkDurumuId = request.GridSevkDurumuId.Value;
                if (request.SevkMiktari.HasValue)
                {
                    await GridUcKSevkPartisiKurali.YeniPartiBaslatAsync(
                        _unitOfWork,
                        satir,
                        request.SevkMiktari.Value,
                        devamSevkAkisi ? devamSevkKarari : GridUcKDevamSevkKarari.UygunDegil,
                        sevkIcerikleri);
                    satir.GridSevkTarihi = TurkeyTime.Now;
                }
            }

            // Genel durumu otomatik hesapla
            satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
            GridSurecDurumHelper.SyncSurecTamamlandi(satir);

            repo.Update(satir);
            await _unitOfWork.SaveChangesAsync();

            if (satir.KaynakCekiSatiriId.HasValue)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(new[] { satir.KaynakCekiSatiriId.Value }, cancellationToken);

            // ===== Sandık Tamamlanma Kontrolü (TrafoSevk ise) =====
            if (!devamSevkAkisi && request.YeniDurumId == (int)GridDurum.TrafoSevk)
            {
                await SandikTamamlanmaKontrol(satir);
            }

            var sevkMetni = "";
            var sevkDurumAdi = Enum.GetName(typeof(GridSevkDurum), satir.GridSevkDurumuId) ?? "Belirsiz";
            sevkMetni = $" | Sevk Durumu: {sevkDurumAdi}";
            if (satir.GridSevkMiktari.HasValue)
            {
                sevkMetni += $" ({satir.GridSevkMiktari.Value} Adet)";
            }

            var uygulananDurumId = satir.GridDurumuId;
            var aciklamaMetni = $"Durum: {Enum.GetName(typeof(GridDurum), uygulananDurumId) ?? uygulananDurumId.ToString()}{sevkMetni}";
            if (!string.IsNullOrWhiteSpace(request.Aciklama))
            {
                aciklamaMetni += $" | Not: {request.Aciklama}";
            }

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                Islem = "Grid Durum Güncellendi",
                IslemTipiId = (int)IslemTipi.GridDurumGuncellendi,
                EskiDeger = eskiDurum.ToString(),
                YeniDeger = uygulananDurumId.ToString(),
                Aciklama = aciklamaMetni
            });

            return Result.Success();
        }

        /// <summary>
        /// Sandıktaki tüm ürünler tamamlanmış mı kontrol et.
        /// Tamamlanan: 3K tipleri (TamGeldi, ProjedenKarsilandi, StoktanKarsilandi, TedarikcidenGeldi) VEYA GridDurumu=TrafoSevk
        /// </summary>
        private async Task SandikTamamlanmaKontrol(CekiSatiri guncellenenSatir)
        {
            var sandikNo = guncellenenSatir.FiiliSandikNo ?? guncellenenSatir.CekideGecenSandikNo;
            if (string.IsNullOrWhiteSpace(sandikNo)) return;

            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var ceki = await cekiRepo.GetByIdAsync(guncellenenSatir.CekiId);
            if (ceki == null) return;

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var tumCekiler = await cekiRepo.FindAsync(c => c.ProjeId == ceki.ProjeId);
            var tumCekiIdler = tumCekiler.Select(c => c.Id).ToList();

            var sandiktakiUrunler = await cekiSatiriRepo.FindAsync(cs =>
                tumCekiIdler.Contains(cs.CekiId) &&
                (cs.FiiliSandikNo == sandikNo || cs.CekideGecenSandikNo == sandikNo));

            if (!sandiktakiUrunler.Any()) return;

            var tamamlananTipler = new[] { (int)UcKDurum.TamGeldi, (int)UcKDurum.ProjedenKarsilandi, (int)UcKDurum.StoktanKarsilandi, (int)UcKDurum.TedarikcidenGeldi };
            var hepsiTamam = sandiktakiUrunler.All(u =>
                tamamlananTipler.Contains(u.UcKKarsilamaTipiId) || u.GridDurumuId == (int)GridDurum.TrafoSevk);

            if (hepsiTamam)
            {
                var sandikRepo = _unitOfWork.GetRepository<Sandik>();
                var sandiklar = await sandikRepo.FindAsync(s =>
                    s.ProjeId == ceki.ProjeId && s.SandikNo == sandikNo);
                var sandik = sandiklar.FirstOrDefault();

                if (sandik != null && sandik.DurumId != (int)SandikDurum.Kapandi)
                {
                    sandik.DurumId = (int)SandikDurum.Kapandi;
                    sandikRepo.Update(sandik);
                    await _unitOfWork.SaveChangesAsync();

                    await _hareketService.HareketKaydetAsync(new HareketGecmisi
                    {
                        ProjeId = ceki.ProjeId,
                        KullaniciId = _currentUserService.UserId ?? 0,
                        ReferansTipi = "Sandik",
                        ReferansId = sandik.Id.ToString(),
                        Islem = "Sandık Otomatik Hazır",
                        IslemTipiId = (int)IslemTipi.SandikOtomatikHazirlandi,
                        EskiDeger = ((int)SandikDurum.Hazirlaniyor).ToString(),
                        YeniDeger = ((int)SandikDurum.Kapandi).ToString(),
                        Aciklama = $"Sandık {sandikNo} içindeki tüm ürünler tamamlandı."
                    });
                }
            }
        }

        private async Task SandigiGridLokasyonunaAlAsync(int projeId, CekiSatiri satir)
        {
            var sandikNo = satir.FiiliSandikNo ?? satir.CekideGecenSandikNo;
            if (string.IsNullOrWhiteSpace(sandikNo))
                return;

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandiklar = await sandikRepo.FindAsync(s => s.ProjeId == projeId && s.SandikNo == sandikNo);
            var sandik = sandiklar.FirstOrDefault();
            if (sandik == null || sandik.DepoLokasyonId == (int)DepoLokasyon.Grid)
                return;

            sandik.DepoLokasyonId = (int)DepoLokasyon.Grid;
            sandikRepo.Update(sandik);
        }
    }
}
