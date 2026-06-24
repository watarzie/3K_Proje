using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using System.Globalization;
using _3K.Core.Helpers;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    public class UcKDurumGuncelleCommandHandler : IRequestHandler<UcKDurumGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;
        private readonly IStokService _stokService;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        private static readonly int[] GecerliTipler =
            { (int)UcKDurum.TamGeldi, (int)UcKDurum.EksikGeldi, (int)UcKDurum.Gelmedi, (int)UcKDurum.ProjedenKarsilandi, (int)UcKDurum.StoktanKarsilandi,
              (int)UcKDurum.TedarikcidenGeldi, (int)UcKDurum.GeriGonderildi, (int)UcKDurum.FazlaGeldi };

        public UcKDurumGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService,
            IStokService stokService,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
            _stokService = stokService;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(UcKDurumGuncelleCommand request, CancellationToken cancellationToken)
        {
            if (!GecerliTipler.Contains(request.KarsilamaTipiId))
                return Result.Failure($"Geçersiz karşılama tipi: {request.KarsilamaTipiId}");

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);
            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            if (!satir.KaynakCekiSatiriId.HasValue &&
                await _sahaTamamlamaService.AktifTamamlamaVarMiAsync(satir.Id, cancellationToken))
                return Result.Failure("Bu ürün sahaya aktarıldığı için normal proje üzerinden 3K işlemi yapılamaz. İşlem saha projesinde yürütülmelidir.");

            if (await SandikSevkKilidiHelper.CekiSatiriSevkEdilmisSandiktaMiAsync(_unitOfWork, satir))
                return Result.Failure(SandikSevkKilidiHelper.UrunKilitliMesaji);

            var eskiDurum = satir.UcKKarsilamaTipiId;

            // ===== Grid İptal / Kapandı Blokajı =====
            if (satir.GridDurumuId == (int)GridDurum.Iptal)
                return Result.Failure("Bu ürün Grid tarafından iptal edildiği için üzerinde hiçbir işlem yapılamaz.");
            if (satir.GridDurumuId == (int)GridDurum.GridKapandi)
                return Result.Failure("Bu ürün Grid tarafından kapatıldığı için üzerinde hiçbir işlem yapılamaz.");

            // ===== Grid Trafo Sevk Blokajı =====
            var fizikselSevkGerektirenTipler = new[]
            {
                (int)UcKDurum.TamGeldi,
                (int)UcKDurum.EksikGeldi,
                (int)UcKDurum.Gelmedi,
                (int)UcKDurum.GeriGonderildi,
                (int)UcKDurum.FazlaGeldi
            };

            var gridKismiTrafoSevkEdildi =
                satir.GridDurumuId == (int)GridDurum.TrafoSevk &&
                satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi &&
                (satir.GridSevkMiktari ?? 0) > 0;

            if (satir.GridDurumuId == (int)GridDurum.TrafoSevk
                && fizikselSevkGerektirenTipler.Contains(request.KarsilamaTipiId)
                && !gridKismiTrafoSevkEdildi)
            {
                return Result.Failure("Bu ürün Trafo Sevk durumunda. 3K fiziksel işlem yapılabilmesi için Grid'e gelen miktar önce 3K'ya sevk edilmelidir.");
            }

            // ===== Kalite Tadilatta Blokajı =====
            if (satir.KaliteDurumId.HasValue)
            {
                var kaliteMetni = _lookupCache.GetDeger<LookupKaliteDurum>(satir.KaliteDurumId.Value);
                if (kaliteMetni == "Tadilatta")
                    return Result.Failure("Bu ürün Kalite tarafından 'Tadilatta' olarak işaretlenmiş. 3K işlemi yapılamaz.");
            }

            // ===== Grid Gelmedi - Sadece Projeden/Stoktan/Tedarikçiden =====
            if (satir.GridDurumuId == (int)GridDurum.Gelmedi)
            {
                var izinliTipler = new[] { (int)UcKDurum.ProjedenKarsilandi, (int)UcKDurum.StoktanKarsilandi, (int)UcKDurum.TedarikcidenGeldi };
                if (!izinliTipler.Contains(request.KarsilamaTipiId))
                    return Result.Failure("Grid 'Gelmedi' durumunda yalnızca Projeden, Stoktan veya Tedarikçiden karşılama yapılabilir.");
            }

            var kaynakKarsilamaTipleri = new[] { (int)UcKDurum.ProjedenKarsilandi, (int)UcKDurum.StoktanKarsilandi, (int)UcKDurum.TedarikcidenGeldi };
            var gridKaynakKarsilamaAcik =
                satir.GridDurumuId == (int)GridDurum.EksikGeldi ||
                satir.GridDurumuId == (int)GridDurum.Gelmedi ||
                satir.GridDurumuId == (int)GridDurum.TrafoSevk;

            var geriGonderimSonrasiKaynakAcik =
                satir.KalanMiktar > 0 &&
                (satir.UcKKarsilamaTipiId == (int)UcKDurum.GeriGonderildi ||
                 satir.GeriGonderilenMiktar > 0 ||
                 satir.YenidenSevkGerekliAdet > 0);

            var projeTransferTelafiProjedenAcik =
                request.KarsilamaTipiId == (int)UcKDurum.ProjedenKarsilandi &&
                satir.KalanMiktar > 0 &&
                satir.ProjeGonderilen > 0;

            if (kaynakKarsilamaTipleri.Contains(request.KarsilamaTipiId)
                && !gridKaynakKarsilamaAcik
                && !geriGonderimSonrasiKaynakAcik
                && !projeTransferTelafiProjedenAcik)
            {
                return Result.Failure("Projeden, stoktan veya tedarikciden karsilama yalnizca eksik/gelmedi, kismi trafo sevk veya 3K geri gonderim sonrasi kalan acikken yapilabilir. Proje transfer telafisi yalnizca projeden karsilama icin gecerlidir.");
            }

            // ===== Eksiksiz (Tam) Geldi Validasyonu =====
            if (request.KarsilamaTipiId == (int)UcKDurum.TamGeldi && satir.GridSevkDurumuId != (int)GridSevkDurum.SevkEdildi)
                return Result.Failure("Grid tarafından eksiksiz sevk edilmeden ürün 'Tam Geldi' olarak işaretlenemez.");



            var gridSevkiVar =
                satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi &&
                (satir.GridSevkMiktari ?? 0) > 0;

            if (request.KarsilamaTipiId == (int)UcKDurum.Gelmedi &&
                (satir.GelenMiktar > 0 ||
                 satir.UcKKarsilamaTipiId == (int)UcKDurum.TamGeldi ||
                 satir.UcKDurumuId == (int)UcKDurum.TamGeldi))
            {
                return Result.Failure("3K gelen miktarı bulunan veya 'Sevk Adeti Tam Geldi' olarak karşılanmış ürün 'Gelmedi' durumuna alınamaz. Önce 3K işlemini geri alın.");
            }

            if ((request.KarsilamaTipiId == (int)UcKDurum.Gelmedi ||
                 request.KarsilamaTipiId == (int)UcKDurum.GeriGonderildi ||
                 request.KarsilamaTipiId == (int)UcKDurum.FazlaGeldi)
                && !gridSevkiVar)
            {
                return Result.Failure("Gelmedi, Geri Gönderildi veya Fazla Geldi işlemi için Grid tarafından sevk edilmiş aktif miktar bulunmalıdır.");
            }

            // ===== Validasyon =====
            switch (request.KarsilamaTipiId)
            {
                case (int)UcKDurum.EksikGeldi:
                    if (request.GelenAdet == null || request.GelenAdet <= 0)
                        return Result.Failure("Eksik geldi durumunda gelen adet girilmelidir.");
                    if (request.GelenAdet >= satir.IstenenAdet)
                        return Result.Failure("Gelen adet miktardan küçük olmalıdır.");
                    break;

                case (int)UcKDurum.ProjedenKarsilandi:
                    if (request.GelenAdet == null || request.GelenAdet <= 0)
                        return Result.Failure("Projeden karşılama adeti girilmelidir.");
                    if (string.IsNullOrWhiteSpace(request.KaynakHedefProjeNo))
                        return Result.Failure("Kaynak proje girilmelidir.");
                    if (request.KaynakCekiSatiriId == null || request.KaynakCekiSatiriId <= 0)
                        return Result.Failure("Kaynak urun secilmelidir.");
                    // Kendi projesinden karşılama yapılamaz
                    var cekiRepo = _unitOfWork.GetRepository<Ceki>();
                    var ceki = await cekiRepo.GetByIdAsync(satir.CekiId);
                    if (ceki != null)
                    {
                        var projeRepo = _unitOfWork.GetRepository<Proje>();
                        var mevcutProje = await projeRepo.GetByIdAsync(ceki.ProjeId);
                        if (mevcutProje != null && mevcutProje.ProjeNo == request.KaynakHedefProjeNo)
                            return Result.Failure("Bir proje kendi ürünlerinden karşılama yapamaz.");
                    }
                    break;

                case (int)UcKDurum.StoktanKarsilandi:
                    if (request.GelenAdet == null || request.GelenAdet <= 0)
                        return Result.Failure("Gelen adet girilmelidir.");
                    if (request.StokKaydiId == null || request.StokKaydiId <= 0)
                        return Result.Failure("Stoktan karşılanan ürünler için stok kaydı (barkod/ürün) seçilmelidir.");
                    
                    var stokRepo = _unitOfWork.GetRepository<StokKaydi>();
                    var secilenStok = await stokRepo.GetByIdAsync(request.StokKaydiId.Value);
                    if (secilenStok != null)
                    {
                        var tStokAd = System.Text.RegularExpressions.Regex.Replace(secilenStok.MalzemeAdi ?? "", @"[^\p{L}0-9\s]", "");
                        tStokAd = System.Text.RegularExpressions.Regex.Replace(tStokAd, @"\s+", " ").Trim().ToLower(new System.Globalization.CultureInfo("tr-TR"));

                        var tUrunAd = System.Text.RegularExpressions.Regex.Replace(satir.Aciklama ?? "", @"[^\p{L}0-9\s]", "");
                        tUrunAd = System.Text.RegularExpressions.Regex.Replace(tUrunAd, @"\s+", " ").Trim().ToLower(new System.Globalization.CultureInfo("tr-TR"));
                        
                        if (tStokAd != tUrunAd)
                        {
                            return Result.Failure($"Seçilen stok adı ({secilenStok.MalzemeAdi}) ile proje ürün adı ({satir.Aciklama}) eşleşmelidir!");
                        }
                    }

                    var yeterli = await _stokService.StokYeterliMi(request.StokKaydiId.Value, request.GelenAdet.Value);
                    if (!yeterli)
                        return Result.Failure("Seçilen stokta yeterli miktar bulunmuyor.");
                    break;

                case (int)UcKDurum.TedarikcidenGeldi:
                    if (request.GelenAdet == null || request.GelenAdet <= 0)
                        return Result.Failure("Gelen adet girilmelidir.");
                    break;
                case (int)UcKDurum.FazlaGeldi:
                    if (request.GelenAdet == null || request.GelenAdet <= 0)
                        return Result.Failure("Fazla gelen adet girilmelidir.");
                    if (!request.StogaAktar)
                        return Result.Failure("Fazla gelen adet stoka aktarılmalıdır.");
                    break;
                case (int)UcKDurum.GeriGonderildi:
                    if (!request.GeriGonderilmeSebebiId.HasValue || request.GeriGonderilmeSebebiId.Value <= 0)
                        return Result.Failure("Geri gönderilme sebebi seçilmelidir.");
                    if (request.GelenAdet == null || request.GelenAdet <= 0)
                        return Result.Failure("Geri gönderilen adet girilmelidir.");
                    if (request.GelenAdet > satir.GelenMiktar)
                        return Result.Failure($"Geri gönderilen adet ({request.GelenAdet}), 3K gelen miktardan ({satir.GelenMiktar}) büyük olamaz.");
                    break;
            }

            // ===== Alanları göncelle =====
            satir.UcKKarsilamaTipiId = request.KarsilamaTipiId;
            satir.UcKAciklama = request.Aciklama;
            satir.KaynakHedefProjeNo = request.KaynakHedefProjeNo;
            decimal? fazlaTeslimStokAdedi = null;

            switch (request.KarsilamaTipiId)
            {
                case (int)UcKDurum.TamGeldi:
                    // KURAL 1: Tam Geldi - Grid'in sevk ettiği miktar kadar teslim al (sandık bütünlüğü)
                    // çeki hedefinin tamamını DEĞİL, o sevkiyattaki fiziksel miktarı alır
                    var sevkMiktari = satir.GridSevkMiktari ?? (satir.IstenenAdet - satir.GelenMiktar - satir.StokKarsilanan - satir.ProjeKarsilanan - satir.TedarikciKarsilanan);
                    satir.GelenMiktar += Math.Max(sevkMiktari, 0);
                    satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
                    satir.TeslimTarihi = TurkeyTime.Now;
                    break;

                case (int)UcKDurum.EksikGeldi:
                    satir.GelenMiktar += request.GelenAdet!.Value;
                    satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
                    satir.TeslimTarihi = TurkeyTime.Now;
                    break;

                case (int)UcKDurum.Gelmedi:
                    satir.UcKDurumuId = (int)UcKDurum.Gelmedi;
                    satir.TeslimTarihi = TurkeyTime.Now;
                    var gelmeyenAdet = satir.GridSevkMiktari ?? 0;
                    satir.YenidenSevkGerekliAdet = Math.Max(satir.YenidenSevkGerekliAdet, gelmeyenAdet);
                    satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
                    break;

                case (int)UcKDurum.ProjedenKarsilandi:
                    satir.KarsilananMiktar += request.GelenAdet!.Value;
                    satir.ProjeKarsilanan += request.GelenAdet!.Value; // Madde 2: Parçalı tracking
                    satir.UcKDurumuId = (int)UcKDurum.ProjedenKarsilandi;
                    satir.TeslimTarihi = TurkeyTime.Now;
                    // Cross-project transfer
                    KapatYenidenSevkIhtiyaci(satir, request.GelenAdet.Value);
                    var transferResult = await HandleProjedenKarsilandi(satir, request);
                    if (!transferResult.IsSuccess)
                        return transferResult;
                    break;

                case (int)UcKDurum.StoktanKarsilandi:
                    satir.KarsilananMiktar += request.GelenAdet!.Value;
                    satir.StokKarsilanan += request.GelenAdet!.Value; // Madde 2: Parçalı tracking
                    satir.UcKDurumuId = (int)UcKDurum.StoktanKarsilandi;
                    satir.TeslimTarihi = TurkeyTime.Now;

                    // Stoktan Düşme ve StokHareketi Loglama İşlemi
                    KapatYenidenSevkIhtiyaci(satir, request.GelenAdet.Value);
                    await _stokService.StokDusAsync(request.StokKaydiId!.Value, request.GelenAdet.Value);

                    var stokHareketRepo = _unitOfWork.GetRepository<StokHareketi>();
                    await stokHareketRepo.AddAsync(new StokHareketi
                    {
                        StokKaydiId = request.StokKaydiId.Value,
                        CekiSatiriId = request.CekiSatiriId,
                        ProjeId = request.ProjeId,
                        KullaniciId = _currentUserService.UserId ?? 0,
                        Miktar = request.GelenAdet.Value,
                        IslemTipiId = (int)IslemTipi.StoktanKarsilandi,
                        Aciklama = $"Proje {request.ProjeId} için 3K aşamasında stoktan {FormatAdet(request.GelenAdet.Value)} adet karşılandı.",
                        Tarih = TurkeyTime.Now
                    });
                    break;

                case (int)UcKDurum.TedarikcidenGeldi:
                    satir.KarsilananMiktar += request.GelenAdet!.Value;
                    satir.TedarikciKarsilanan += request.GelenAdet!.Value; // Madde 2: Parçalı tracking
                    satir.UcKDurumuId = (int)UcKDurum.TedarikcidenGeldi;
                    satir.TeslimTarihi = TurkeyTime.Now;
                    KapatYenidenSevkIhtiyaci(satir, request.GelenAdet.Value);
                    break;

                case (int)UcKDurum.FazlaGeldi:
                    var mevcutTamamlanan = satir.GelenMiktar + satir.KarsilananMiktar - satir.ProjeGonderilen + satir.TrafoSevkAdet;
                    var normalKalan = Math.Max(satir.IstenenAdet - mevcutTamamlanan, 0);
                    var normalTeslimAdedi = Math.Min(satir.GridSevkMiktari ?? normalKalan, normalKalan);
                    if (normalTeslimAdedi > 0)
                        satir.GelenMiktar += normalTeslimAdedi;

                    satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
                    satir.TeslimTarihi = TurkeyTime.Now;
                    fazlaTeslimStokAdedi = request.GelenAdet!.Value;
                    break;

                case (int)UcKDurum.GeriGonderildi:
                    // Geri gönderilen miktarı 3K gelen'den ve Grid gelen'den düş
                    var geriAdet = request.GelenAdet!.Value;
                    satir.GelenMiktar = Math.Max(satir.GelenMiktar - geriAdet, 0);
                    satir.GeriGonderilenMiktar += geriAdet;
                    satir.YenidenSevkGerekliAdet += geriAdet;
                    satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
                    satir.UcKDurumuId = (int)UcKDurum.GeriGonderildi;
                    satir.GeriGonderilmeSebebiId = request.GeriGonderilmeSebebiId;
                    break;
            }

            // Toplam kontrol
            var toplam = satir.GelenMiktar + satir.KarsilananMiktar - satir.ProjeGonderilen;
            if (toplam + satir.TrafoSevkAdet > satir.IstenenAdet)
                return Result.Failure($"Toplam tamamlanan adet ({FormatAdet(toplam)}) ve trafo sevk ({FormatAdet(satir.TrafoSevkAdet)}), çeki miktarını ({FormatAdet(satir.IstenenAdet)}) aşamaz.");

            // Genel durumu hesapla
            satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
            // KURAL 2: Merkezi kalan hesaplaması ve durum override
            _durumHesaplaService.HesaplaKalanVeDurum(satir);

            repo.Update(satir);

            // ===== SANDIK İÇERİK SENKRONİZASYONU =====
            // 3K'da "Tam Geldi" veya karşılandı olarak işaretlenen ürünler doğrudan sandığa konmuş sayılır.
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var ilgiliIcerikler = await sandikIcerikRepo.FindAsync(x => x.CekiSatiriId == satir.Id);
            var ilgiliIcerikListesi = ilgiliIcerikler.ToList();
            
            if (ilgiliIcerikListesi.Any())
            {
                var anaIcerik = ilgiliIcerikListesi.First();
                anaIcerik.KonulanAdet = Math.Max(toplam, 0); // Kumulatif net toplami konulan adete esitle
                // Madde 2: Parçalı karşılama SandikIcerik senkronizasyonu
                anaIcerik.StokKarsilanan = satir.StokKarsilanan;
                anaIcerik.ProjeKarsilanan = satir.ProjeKarsilanan;
                anaIcerik.TedarikciKarsilanan = satir.TedarikciKarsilanan;
                sandikIcerikRepo.Update(anaIcerik);
            }

            await VarsayilanUcKDepoLokasyonuAtaAsync(ilgiliIcerikListesi, request.KarsilamaTipiId);

            if (fazlaTeslimStokAdedi.HasValue)
                await FazlaTeslimStogaAktarAsync(satir, request, fazlaTeslimStokAdedi.Value);

            await _unitOfWork.SaveChangesAsync();

            // Hareket kaydı
            var hareketAciklama = request.Aciklama ?? "";
            // Geri gönderildi durumunda açıklamaya sebep ve adet bilgisi ekle
            if (request.KarsilamaTipiId == (int)UcKDurum.GeriGonderildi)
            {
                var sebebiMetni = request.GeriGonderilmeSebebiId.HasValue
                    ? _lookupCache.GetDeger<LookupGeriGonderilmeSebebi>(request.GeriGonderilmeSebebiId.Value)
                    : "Bilinmiyor";
                hareketAciklama = $"Geri gönderildi - {FormatAdet(request.GelenAdet ?? 0)} adet - Sebep: {sebebiMetni}. {hareketAciklama}".Trim();
            }
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                Islem = "3K Durum Göncellendi",
                IslemTipiId = (int)IslemTipi.UcKDurumGuncellendi,
                EskiDeger = eskiDurum.ToString(),
                YeniDeger = request.KarsilamaTipiId.ToString(),
                Aciklama = hareketAciklama
            });

            return Result.Success();
        }

        private async Task FazlaTeslimStogaAktarAsync(CekiSatiri satir, UcKDurumGuncelleCommand request, decimal fazlaAdet)
        {
            var projeNo = request.MevcutProjeNo;
            if (string.IsNullOrWhiteSpace(projeNo))
            {
                var projeRepo = _unitOfWork.GetRepository<Proje>();
                var proje = await projeRepo.GetByIdAsync(request.ProjeId);
                projeNo = proje?.ProjeNo;
            }

            var stokRepo = _unitOfWork.GetRepository<StokKaydi>();
            var stokKaydi = new StokKaydi
            {
                MalzemeKodu = satir.BarkodNo ?? string.Empty,
                MalzemeAdi = satir.Aciklama ?? string.Empty,
                Miktar = fazlaAdet,
                BirimId = satir.BirimId,
                KaynakProje = projeNo,
                StokGirisNedeni = "Fazla teslim",
                DurumId = (int)StokDurum.Aktif
            };

            await stokRepo.AddAsync(stokKaydi);
            await _unitOfWork.SaveChangesAsync();

            var stokHareketRepo = _unitOfWork.GetRepository<StokHareketi>();
            await stokHareketRepo.AddAsync(new StokHareketi
            {
                StokKaydiId = stokKaydi.Id,
                CekiSatiriId = satir.Id,
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                Miktar = fazlaAdet,
                IslemTipiId = (int)IslemTipi.FazlaTeslimStogaAktarildi,
                Aciklama = $"Fazla teslim nedeniyle {FormatAdet(fazlaAdet)} adet stoka aktarıldı.",
                Tarih = TurkeyTime.Now
            });
        }

        private async Task VarsayilanUcKDepoLokasyonuAtaAsync(IReadOnlyCollection<SandikIcerik> ilgiliIcerikler, int karsilamaTipiId)
        {
            if (!UcKDepoLokasyonuTetikler(karsilamaTipiId) || !ilgiliIcerikler.Any())
                return;

            var sandikIdleri = ilgiliIcerikler
                .Select(i => i.SandikId)
                .Distinct()
                .ToList();

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandiklar = await sandikRepo.FindAsync(s =>
                sandikIdleri.Contains(s.Id) &&
                s.DepoLokasyonId == (int)DepoLokasyon.Belirsiz);

            foreach (var sandik in sandiklar)
            {
                sandik.DepoLokasyonId = (int)DepoLokasyon.UcK;
                sandikRepo.Update(sandik);
            }
        }

        private static bool UcKDepoLokasyonuTetikler(int karsilamaTipiId)
        {
            return karsilamaTipiId == (int)UcKDurum.TamGeldi ||
                   karsilamaTipiId == (int)UcKDurum.EksikGeldi ||
                   karsilamaTipiId == (int)UcKDurum.ProjedenKarsilandi ||
                   karsilamaTipiId == (int)UcKDurum.StoktanKarsilandi ||
                   karsilamaTipiId == (int)UcKDurum.TedarikcidenGeldi ||
                   karsilamaTipiId == (int)UcKDurum.FazlaGeldi;
        }

        /// <summary>
        /// Geri bildirim sonrasındaki yeniden sevk ihtiyacını 3K kaynak karşılaması kadar kapatır.
        /// </summary>
        private static void KapatYenidenSevkIhtiyaci(CekiSatiri satir, decimal karsilananAdet)
        {
            if (satir.YenidenSevkGerekliAdet <= 0 || karsilananAdet <= 0)
                return;

            satir.YenidenSevkGerekliAdet = Math.Max(satir.YenidenSevkGerekliAdet - karsilananAdet, 0);

            if (satir.YenidenSevkGerekliAdet <= 0 &&
                satir.GridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli)
            {
                satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
            }
        }

        /// <summary>
        /// PROJEDEN KARŞILANDI: Kaynak projede eşleşen ürünü bul, adet düş, transfer kaydı oluştur.
        /// </summary>
        private async Task<Result> HandleProjedenKarsilandi(CekiSatiri hedefSatir, UcKDurumGuncelleCommand request)
        {
            if (request.KaynakCekiSatiriId == null)
                return Result.Failure("Kaynak urun secilmelidir.");

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var kaynakSatir = await cekiSatiriRepo.GetByIdAsync(request.KaynakCekiSatiriId.Value);

            if (kaynakSatir == null)
                return Result.Failure("Kaynak urun bulunamadi.", 404);

            // Kaynak projedeki ürünün fiziksel gelen stoğundan eksilt
            var adet = request.GelenAdet ?? 0;
            var kullanilabilir = HesaplaNetKullanilabilir(kaynakSatir);
            if (adet > kullanilabilir)
                return Result.Failure($"Kaynak urunde kullanilabilir miktar yetersiz. Kullanilabilir: {FormatAdet(kullanilabilir)}, istenen: {FormatAdet(adet)}.");

            // Kaynak üründe "Başka projeye verildi" bilgisini kaydet
            // Birden fazla projeye verilebileceği için mevcut değere ekleme yapılır
            var hedefProjeNo = request.MevcutProjeNo ?? request.ProjeId.ToString();
            if (string.IsNullOrWhiteSpace(kaynakSatir.KaynakHedefProjeNo))
                kaynakSatir.KaynakHedefProjeNo = hedefProjeNo;
            else if (!kaynakSatir.KaynakHedefProjeNo.Contains(hedefProjeNo))
                kaynakSatir.KaynakHedefProjeNo += $", {hedefProjeNo}";

            // Gönderilen miktar takibi
            kaynakSatir.ProjeGonderilen += adet;
            kaynakSatir.DurumId = _durumHesaplaService.HesaplaGenelDurum(kaynakSatir.GridDurumuId, kaynakSatir.UcKDurumuId);
            _durumHesaplaService.HesaplaKalanVeDurum(kaynakSatir);
            AcYenidenSevkIhtiyaci(kaynakSatir);

            cekiSatiriRepo.Update(kaynakSatir);

            // Kaynak projeyi bul
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var kaynakProjeler = await projeRepo.FindAsync(p => p.Cekiler.Any(c => c.Id == kaynakSatir.CekiId));
            var kaynakProje = kaynakProjeler.FirstOrDefault();

            var hedefProjeler = await projeRepo.FindAsync(p => p.Cekiler.Any(c => c.Id == hedefSatir.CekiId));
            var hedefProje = hedefProjeler.FirstOrDefault();

            if (kaynakProje == null || hedefProje == null)
                return Result.Failure("Kaynak veya hedef proje bulunamadi.", 404);

            var transferRepo = _unitOfWork.GetRepository<ProjeTransfer>();
            var aktifTransferler = await transferRepo.FindAsync(t =>
                t.DurumId == (int)ProjeTransferDurum.Aktif &&
                (t.KaynakCekiSatiriId == hedefSatir.Id || t.HedefCekiSatiriId == hedefSatir.Id));

            var parentTransfer = aktifTransferler
                .Where(t => t.KaynakCekiSatiriId == hedefSatir.Id)
                .OrderBy(t => t.ZincirSeviyesi)
                .ThenBy(t => t.Tarih)
                .FirstOrDefault();

            var transfer = new ProjeTransfer
            {
                KaynakProjeId = kaynakProje.Id,
                HedefProjeId = hedefProje.Id,
                KaynakCekiSatiriId = kaynakSatir.Id,
                HedefCekiSatiriId = hedefSatir.Id,
                BarkodNo = hedefSatir.BarkodNo,
                UrunAdi = hedefSatir.Aciklama,
                Miktar = adet,
                TransferTipiId = parentTransfer != null ? (int)ProjeTransferTipi.Telafi : (int)ProjeTransferTipi.Karsilama,
                DurumId = (int)ProjeTransferDurum.Aktif,
                ParentTransferId = parentTransfer?.Id,
                RootTransferId = parentTransfer?.RootTransferId ?? parentTransfer?.Id,
                ZincirSeviyesi = parentTransfer != null ? parentTransfer.ZincirSeviyesi + 1 : 0,
                KullaniciId = _currentUserService.UserId ?? 0,
                Aciklama = request.Aciklama,
                Tarih = TurkeyTime.Now
            };
            await transferRepo.AddAsync(transfer);

            return Result.Success();
        }

        private static decimal HesaplaNetKullanilabilir(CekiSatiri satir)
        {
            var net = satir.GelenMiktar + satir.ProjeKarsilanan - satir.ProjeGonderilen;

            return Math.Max(net, 0);
        }

        private static void AcYenidenSevkIhtiyaci(CekiSatiri satir)
        {
            if (satir.KalanMiktar <= 0)
                return;

            if (satir.GridSevkDurumuId != (int)GridSevkDurum.SevkEdildi || (satir.GridSevkMiktari ?? 0) <= 0)
                return;

            satir.YenidenSevkGerekliAdet = Math.Max(satir.YenidenSevkGerekliAdet, satir.KalanMiktar);
            satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        }

        /// <summary>
        /// Adet değerini ondalıksız (2,000 -> 2) formatta döndürür.
        /// </summary>
        private static string FormatAdet(decimal value)
        {
            if (decimal.Truncate(value) == value)
                return decimal.Truncate(value).ToString(CultureInfo.InvariantCulture);
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
