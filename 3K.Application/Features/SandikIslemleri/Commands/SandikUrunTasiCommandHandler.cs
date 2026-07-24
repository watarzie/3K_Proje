using System.Globalization;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikUrunTasiCommandHandler : IRequestHandler<SandikUrunTasiCommand, Result>
    {
        private const string TransferIslemAnahtariIndex = "IX_SandikUrunTransferleri_IslemAnahtari";
        private const decimal EnYuksekTasinabilirMiktar = 99999999999999.9999m;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public SandikUrunTasiCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(SandikUrunTasiCommand request, CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not int kullaniciId || kullaniciId <= 0)
                return Result.Failure("Taşıma işlemi için geçerli bir kullanıcı oturumu bulunamadı.", 401);

            if (request.TasinanAdet <= 0)
                return Result.Failure("Taşınan miktar 0'dan büyük olmalıdır.");

            if (!MiktarHassasiyetiGecerliMi(request.TasinanAdet))
            {
                return Result.Failure(
                    "Taşınan miktar en fazla 14 tam ve 4 ondalık basamak içerebilir.");
            }

            if (request.IslemAnahtari == Guid.Empty)
                return Result.Failure("İşlem anahtarı zorunludur.");

            var islemAnahtari = request.IslemAnahtari;
            var transferRepo = _unitOfWork.GetRepository<SandikUrunTransferi>();

            var oncekiTransfer = (await transferRepo.FindAsync(t => t.IslemAnahtari == islemAnahtari))
                .SingleOrDefault();

            if (oncekiTransfer != null)
            {
                return TransferTekrariAyniMi(oncekiTransfer, request)
                    ? Result.Success()
                    : Result.Failure("İşlem anahtarı daha önce farklı bir sandık taşımasında kullanılmış.", 409);
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
                {
                    var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
                    var sandikRepo = _unitOfWork.GetRepository<Sandik>();
                    var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
                    var cekiRepo = _unitOfWork.GetRepository<Ceki>();
                    var hareketRepo = _unitOfWork.GetRepository<HareketGecmisi>();
                    var sahaAktarimKalemiRepo = _unitOfWork.GetRepository<SahaAktarimKalemi>();

                    var kaynakIcerik = await icerikRepo.GetByIdAsync(request.KaynakSandikIcerikId);
                    if (kaynakIcerik == null)
                        return Result.Failure("Kaynak sandık içeriği bulunamadı.", 404);

                    var kaynakSandik = await sandikRepo.GetByIdAsync(kaynakIcerik.SandikId);
                    if (kaynakSandik == null)
                        return Result.Failure("Kaynak sandık bulunamadı.", 404);

                    var hedefSandik = await sandikRepo.GetByIdAsync(request.HedefSandikId);
                    if (hedefSandik == null)
                        return Result.Failure("Hedef sandık bulunamadı.", 404);

                    var projeKontrolu = ProjeVeSandiklariDogrula(request, kaynakSandik, hedefSandik);
                    if (projeKontrolu != null)
                        return projeKontrolu;

                    if (SandikSevkKilidiHelper.SandikKilitliMi(kaynakSandik))
                        return Result.Failure("Kaynak sandık sevk edildiği için içinden ürün taşınamaz.");

                    if (SandikSevkKilidiHelper.SandikKilitliMi(hedefSandik))
                        return Result.Failure("Hedef sandık sevk edildiği için içine ürün taşınamaz.");

                    CekiSatiri? cekiSatiri = null;
                    List<SandikIcerik> satirIcerikleri = new();

                    if (kaynakIcerik.CekiSatiriId.HasValue)
                    {
                        cekiSatiri = await cekiSatiriRepo.GetByIdAsync(kaynakIcerik.CekiSatiriId.Value);
                        if (cekiSatiri == null)
                            return Result.Failure("Kaynak ürünün çeki satırı bulunamadı.", 404);

                        var ceki = await cekiRepo.GetByIdAsync(cekiSatiri.CekiId);
                        if (ceki == null)
                            return Result.Failure("Kaynak ürünün çeki kaydı bulunamadı.", 404);

                        if (ceki.ProjeId != request.ProjeId ||
                            ceki.ProjeId != kaynakSandik.ProjeId ||
                            ceki.ProjeId != hedefSandik.ProjeId)
                        {
                            return Result.Failure(
                                "Kaynak ürünün çeki kaydı kaynak ve hedef sandıkların projesine ait değil.",
                                409);
                        }

                        if (await SahaAktarimBlokajHelper.KaynakSatirAktarildiMiAsync(
                                _sahaTamamlamaService,
                                cekiSatiri,
                                cancellationToken))
                        {
                            return Result.Failure(SahaAktarimBlokajHelper.SandikMesaji);
                        }

                        satirIcerikleri = (await icerikRepo.FindAsync(i => i.CekiSatiriId == cekiSatiri.Id)).ToList();
                    }

                    if (IcerikteNegatifMiktarVar(kaynakIcerik))
                    {
                        return Result.Failure(
                            "Kaynak sandık içeriğinde negatif miktar bulundu. Veri düzeltilmeden taşıma yapılamaz.",
                            409);
                    }

                    kaynakIcerik.TahsisMiktari = EtkinTahsisMiktariniBul(kaynakIcerik, cekiSatiri, satirIcerikleri);

                    if (request.TasinanAdet > kaynakIcerik.TahsisMiktari)
                    {
                        return Result.Failure(
                            $"Taşınan miktar ({FormatAdet(request.TasinanAdet)}), kaynak sandığa tahsis edilen miktardan " +
                            $"({FormatAdet(kaynakIcerik.TahsisMiktari)}) büyük olamaz.", 409);
                    }

                    var hedefSonucu = await HedefIcerigiBulVeyaOlusturAsync(
                        kaynakIcerik,
                        hedefSandik,
                        cekiSatiri,
                        satirIcerikleri,
                        icerikRepo);

                    if (hedefSonucu.Hata != null)
                        return hedefSonucu.Hata;

                    var hedefIcerik = hedefSonucu.Icerik!;
                    if (IcerikteNegatifMiktarVar(hedefIcerik))
                    {
                        return Result.Failure(
                            "Hedef sandık içeriğinde negatif miktar bulundu. Veri düzeltilmeden taşıma yapılamaz.",
                            409);
                    }

                    var kaynakTahsisOnce = kaynakIcerik.TahsisMiktari;
                    var kaynakKonulanOnce = kaynakIcerik.KonulanAdet;
                    var fizikselTasinanAdet = Math.Min(request.TasinanAdet, kaynakKonulanOnce);
                    var tasinanEksikAdet = EksikMiktariniBol(
                        kaynakIcerik.EksikAdet,
                        request.TasinanAdet,
                        kaynakTahsisOnce);
                    var kaynakKirilimlari = KaynakKirilimlariniBol(
                        kaynakIcerik,
                        fizikselTasinanAdet,
                        kaynakKonulanOnce);

                    kaynakIcerik.TahsisMiktari -= request.TasinanAdet;
                    kaynakIcerik.KonulanAdet -= fizikselTasinanAdet;
                    kaynakIcerik.StokKarsilanan -= kaynakKirilimlari.Stok;
                    kaynakIcerik.ProjeKarsilanan -= kaynakKirilimlari.Proje;
                    kaynakIcerik.TedarikciKarsilanan -= kaynakKirilimlari.Tedarikci;
                    kaynakIcerik.EksikAdet -= tasinanEksikAdet;

                    hedefIcerik.TahsisMiktari += request.TasinanAdet;
                    hedefIcerik.KonulanAdet += fizikselTasinanAdet;
                    hedefIcerik.StokKarsilanan += kaynakKirilimlari.Stok;
                    hedefIcerik.ProjeKarsilanan += kaynakKirilimlari.Proje;
                    hedefIcerik.TedarikciKarsilanan += kaynakKirilimlari.Tedarikci;
                    hedefIcerik.EksikAdet += tasinanEksikAdet;

                    // Miktar alanı Saha/Yedek raporlarında kullanılan tahsis gölgesidir. ÇEKİ bağlantısı
                    // olsa da olmasa da parçalı taşıma sonrasında iki sandığın gerçek tahsisini izler.
                    kaynakIcerik.Miktar = kaynakIcerik.TahsisMiktari;
                    hedefIcerik.Miktar = hedefIcerik.TahsisMiktari;

                    if (kaynakIcerik.TahsisMiktari <= 0 && kaynakIcerik.KonulanAdet <= 0)
                        icerikRepo.Remove(kaynakIcerik);
                    else
                        icerikRepo.Update(kaynakIcerik);

                    if (hedefSonucu.YeniKayit)
                        await icerikRepo.AddAsync(hedefIcerik);
                    else
                        icerikRepo.Update(hedefIcerik);

                    if (!IcerikAktifMi(kaynakIcerik))
                    {
                        var kaynaktaAktifDigerIcerikVar = (await icerikRepo.FindAsync(i =>
                                i.SandikId == kaynakSandik.Id &&
                                i.Id != kaynakIcerik.Id &&
                                (i.TahsisMiktari > 0 ||
                                 i.KonulanAdet > 0 ||
                                 (i.CekiSatiriId == null && i.Miktar > 0))))
                            .Any();

                        if (!kaynaktaAktifDigerIcerikVar)
                            kaynakSandik.DurumId = (int)SandikDurum.Bos;
                    }

                    if (cekiSatiri != null)
                    {
                        await TekilFiiliSandikNoVarsaGuncelleAsync(
                            cekiSatiri,
                            kaynakIcerik,
                            hedefIcerik,
                            satirIcerikleri,
                            kaynakSandik,
                            hedefSandik,
                            sandikRepo,
                            cekiSatiriRepo);

                        await SahaAktarimHedefSandiginiSenkronizeEtAsync(
                            cekiSatiri,
                            kaynakIcerik,
                            hedefIcerik,
                            satirIcerikleri,
                            sahaAktarimKalemiRepo);
                    }

                    var hedefEskiDurumId = hedefSandik.DurumId;
                    if (hedefSandik.DurumId is (int)SandikDurum.Bos or (int)SandikDurum.Kapandi)
                    {
                        hedefSandik.DurumId = (int)SandikDurum.Hazirlaniyor;
                    }

                    // Her iki sandığı da UPDATE ederek xmin eşzamanlılık kontrolünü devreye alırız.
                    // Böylece kontrol sonrasında başlayan bir sevkiyat, taşıma ile sessizce yarışamaz.
                    sandikRepo.Update(kaynakSandik);
                    sandikRepo.Update(hedefSandik);

                    var urunAdi = cekiSatiri?.Aciklama ?? kaynakIcerik.Isim ?? "malzeme";
                    var barkodNo = cekiSatiri?.BarkodNo ?? kaynakIcerik.BarkodNo;
                    var birimId = kaynakIcerik.BirimId ?? cekiSatiri?.BirimId;
                    var aciklama = fizikselTasinanAdet == request.TasinanAdet
                        ? $"{FormatAdet(request.TasinanAdet)} adet '{urunAdi}', " +
                          $"Sandık {kaynakSandik.SandikNo}'den Sandık {hedefSandik.SandikNo}'e taşındı."
                        : $"'{urunAdi}' için {FormatAdet(request.TasinanAdet)} adet tahsis " +
                          $"Sandık {kaynakSandik.SandikNo}'den Sandık {hedefSandik.SandikNo}'e aktarıldı; " +
                          $"fiziksel taşınan miktar {FormatAdet(fizikselTasinanAdet)} adettir.";

                    await transferRepo.AddAsync(new SandikUrunTransferi
                    {
                        ProjeId = request.ProjeId,
                        CekiSatiriId = kaynakIcerik.CekiSatiriId,
                        KaynakSandikId = kaynakSandik.Id,
                        HedefSandikId = hedefSandik.Id,
                        KaynakSandikIcerikId = kaynakIcerik.Id,
                        IslemAnahtari = islemAnahtari,
                        Miktar = request.TasinanAdet,
                        StokKarsilanan = kaynakKirilimlari.Stok,
                        ProjeKarsilanan = kaynakKirilimlari.Proje,
                        TedarikciKarsilanan = kaynakKirilimlari.Tedarikci,
                        KaynakSandikNo = kaynakSandik.SandikNo,
                        HedefSandikNo = hedefSandik.SandikNo,
                        BarkodNo = barkodNo,
                        UrunAdi = urunAdi,
                        BirimId = birimId,
                        KullaniciId = kullaniciId,
                        Aciklama = aciklama
                    });

                    if (hedefEskiDurumId == (int)SandikDurum.Kapandi)
                    {
                        await hareketRepo.AddAsync(new HareketGecmisi
                        {
                            ProjeId = request.ProjeId,
                            KullaniciId = kullaniciId,
                            ReferansTipi = "Sandik",
                            ReferansId = hedefSandik.Id.ToString(CultureInfo.InvariantCulture),
                            ReferansMetni = $"Sandık {hedefSandik.SandikNo}",
                            Islem = "Sandık Geri Açıldı",
                            EskiDeger = Enum.GetName(typeof(SandikDurum), hedefEskiDurumId) ?? "Kapandi",
                            YeniDeger = Enum.GetName(typeof(SandikDurum), hedefSandik.DurumId) ?? "Hazirlaniyor",
                            Aciklama = "İçine ürün taşındığı için sandık yeniden hazırlanıyor durumuna getirildi."
                        });
                    }

                    await hareketRepo.AddAsync(new HareketGecmisi
                    {
                        ProjeId = request.ProjeId,
                        KullaniciId = kullaniciId,
                        ReferansTipi = kaynakIcerik.CekiSatiriId.HasValue ? "CekiSatiri" : "SandikIcerik",
                        ReferansId = (kaynakIcerik.CekiSatiriId ?? kaynakIcerik.Id).ToString(CultureInfo.InvariantCulture),
                        ReferansMetni = urunAdi,
                        Islem = "Sandık Ürün Taşıma",
                        IslemTipiId = (int)IslemTipi.UrunTasindi,
                        EskiDeger = kaynakSandik.SandikNo,
                        YeniDeger = hedefSandik.SandikNo,
                        Aciklama = aciklama
                    });

                    // Tahsis, fiziksel miktar, transfer defteri ve hareket geçmişi tek atomik
                    // SaveChanges çağrısında kalıcılaştırılır.
                    await _unitOfWork.SaveChangesAsync(transactionCancellationToken);

                    if (cekiSatiri?.KaynakCekiSatiriId.HasValue == true)
                    {
                        await _sahaTamamlamaService.SenkronizeKaynakProjelerBySahaSandikIdsAsync(
                            new[] { kaynakSandik.Id, hedefSandik.Id },
                            transactionCancellationToken);
                    }

                    return Result.Success();
                }, cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                var tekrarSonucu = await KayitliTransferTekrariniDogrulaAsync(
                    transferRepo,
                    islemAnahtari,
                    request);
                if (tekrarSonucu != null)
                    return tekrarSonucu;

                return Result.Failure(
                    "Ürün miktarı başka bir kullanıcı tarafından değiştirildi. Güncel veriyi yükleyip tekrar deneyin.",
                    409);
            }
            catch (UniqueConstraintViolationException ex)
                when (string.Equals(ex.ConstraintName, TransferIslemAnahtariIndex, StringComparison.OrdinalIgnoreCase))
            {
                // Unique ihlali diğer transaction commit edildikten sonra oluşur. Veritabanındaki
                // payload doğrulanmadan başarılı saymak, aynı anahtar/farklı talebi gizleyebilir.
                return await KayitliTransferTekrariniDogrulaAsync(
                           transferRepo,
                           islemAnahtari,
                           request)
                       ?? Result.Failure("Taşıma işlemi eşzamanlı bir istekle çakıştı. Tekrar deneyin.", 409);
            }
            catch (UniqueConstraintViolationException)
            {
                return Result.Failure(
                    "Taşıma sırasında aynı ürün için çakışan bir sandık tahsisi oluştu. Güncel veriyi yükleyip tekrar deneyin.",
                    409);
            }
        }

        private static Result? ProjeVeSandiklariDogrula(
            SandikUrunTasiCommand request,
            Sandik kaynakSandik,
            Sandik hedefSandik)
        {
            if (kaynakSandik.Id == hedefSandik.Id)
                return Result.Failure("Kaynak ve hedef sandık aynı olamaz.");

            if (kaynakSandik.ProjeId != hedefSandik.ProjeId)
                return Result.Failure("Kaynak ve hedef sandık aynı projeye ait olmalıdır.");

            if (kaynakSandik.ProjeId != request.ProjeId || hedefSandik.ProjeId != request.ProjeId)
                return Result.Failure("Kaynak veya hedef sandık belirtilen projeye ait değil.", 403);

            return null;
        }

        private static bool MiktarHassasiyetiGecerliMi(decimal miktar)
        {
            var decimalBits = decimal.GetBits(miktar);
            var scale = (decimalBits[3] >> 16) & 0x7F;

            return scale <= 4 && miktar <= EnYuksekTasinabilirMiktar;
        }

        private static bool IcerikteNegatifMiktarVar(SandikIcerik icerik)
        {
            return icerik.TahsisMiktari < 0 ||
                   icerik.KonulanAdet < 0 ||
                   icerik.EksikAdet < 0 ||
                   icerik.Miktar < 0 ||
                   icerik.StokKarsilanan < 0 ||
                   icerik.ProjeKarsilanan < 0 ||
                   icerik.TedarikciKarsilanan < 0;
        }

        private static decimal EksikMiktariniBol(
            decimal kaynakEksikAdet,
            decimal tasinanTahsis,
            decimal kaynakTahsis)
        {
            if (kaynakEksikAdet <= 0 || tasinanTahsis <= 0 || kaynakTahsis <= 0)
                return 0;

            // Tahsisin tamamı taşınıyorsa yuvarlama artığını kaynakta bırakmayız.
            if (tasinanTahsis >= kaynakTahsis)
                return kaynakEksikAdet;

            var pay = DortHaneyeAsagiYuvarla(kaynakEksikAdet * tasinanTahsis / kaynakTahsis);
            return Math.Min(pay, kaynakEksikAdet);
        }

        private static bool IcerikAktifMi(SandikIcerik icerik)
        {
            return icerik.TahsisMiktari > 0 ||
                   icerik.KonulanAdet > 0 ||
                   (!icerik.CekiSatiriId.HasValue && icerik.Miktar > 0);
        }

        private static bool TransferTekrariAyniMi(SandikUrunTransferi transfer, SandikUrunTasiCommand request)
        {
            return transfer.ProjeId == request.ProjeId &&
                   transfer.KaynakSandikIcerikId == request.KaynakSandikIcerikId &&
                   transfer.HedefSandikId == request.HedefSandikId &&
                   transfer.Miktar == request.TasinanAdet;
        }

        private static async Task<Result?> KayitliTransferTekrariniDogrulaAsync(
            IGenericRepository<SandikUrunTransferi> transferRepo,
            Guid islemAnahtari,
            SandikUrunTasiCommand request)
        {
            var kayitliTransfer = (await transferRepo.FindAsync(t => t.IslemAnahtari == islemAnahtari))
                .SingleOrDefault();

            if (kayitliTransfer == null)
                return null;

            return TransferTekrariAyniMi(kayitliTransfer, request)
                ? Result.Success()
                : Result.Failure("İşlem anahtarı daha önce farklı bir sandık taşımasında kullanılmış.", 409);
        }

        private static decimal EtkinTahsisMiktariniBul(
            SandikIcerik icerik,
            CekiSatiri? cekiSatiri,
            IReadOnlyCollection<SandikIcerik> satirIcerikleri)
        {
            if (icerik.TahsisMiktari > 0)
                return icerik.TahsisMiktari;

            if (cekiSatiri == null)
                return Math.Max(icerik.Miktar, icerik.KonulanAdet);

            var aktifSandikSayisi = satirIcerikleri
                .Where(i => i.TahsisMiktari > 0 || i.KonulanAdet > 0)
                .Select(i => i.SandikId)
                .Distinct()
                .Count();

            // Legacy tek tahsisli kayıtta ana istenen miktar planı temsil eder. Daha önce
            // bölünmüş satırlarda ise her kaydın en güvenli tahsisi kendi fiziksel miktarıdır.
            return aktifSandikSayisi <= 1
                ? Math.Max(cekiSatiri.IstenenAdet, icerik.KonulanAdet)
                : icerik.KonulanAdet;
        }

        private static async Task<(SandikIcerik? Icerik, bool YeniKayit, Result? Hata)>
            HedefIcerigiBulVeyaOlusturAsync(
                SandikIcerik kaynakIcerik,
                Sandik hedefSandik,
                CekiSatiri? cekiSatiri,
                IReadOnlyCollection<SandikIcerik> satirIcerikleri,
                IGenericRepository<SandikIcerik> icerikRepo)
        {
            if (kaynakIcerik.CekiSatiriId.HasValue)
            {
                var hedefAdaylari = satirIcerikleri
                    .Where(i => i.SandikId == hedefSandik.Id)
                    .ToList();

                if (hedefAdaylari.Count > 1)
                {
                    return (null, false, Result.Failure(
                        "Hedef sandıkta aynı ürün için birden fazla içerik kaydı bulundu. Veri düzeltilmeden taşıma yapılamaz.",
                        409));
                }

                if (hedefAdaylari.Count == 1)
                {
                    var hedefIcerik = await icerikRepo.GetByIdAsync(hedefAdaylari[0].Id);
                    if (hedefIcerik == null)
                        return (null, false, Result.Failure("Hedef sandık içeriği bulunamadı.", 404));
                    if (IcerikteNegatifMiktarVar(hedefIcerik))
                    {
                        return (null, false, Result.Failure(
                            "Hedef sandık içeriğinde negatif miktar bulundu. Veri düzeltilmeden taşıma yapılamaz.",
                            409));
                    }

                    hedefIcerik.TahsisMiktari = hedefIcerik.TahsisMiktari > 0
                        ? hedefIcerik.TahsisMiktari
                        : hedefIcerik.KonulanAdet;

                    return (hedefIcerik, false, null);
                }
            }

            // CekiSatiriId null olan manuel satırlar birbirinden bağımsızdır. Barkod/isim
            // benzerliğine göre birleştirmek farklı manuel ürünleri yanlışlıkla kaynaştırır.
            return (new SandikIcerik
            {
                SandikId = hedefSandik.Id,
                CekiSatiriId = kaynakIcerik.CekiSatiriId,
                TahsisMiktari = 0,
                KonulanAdet = 0,
                EksikAdet = 0,
                BarkodNo = kaynakIcerik.BarkodNo,
                Isim = kaynakIcerik.Isim,
                Miktar = 0,
                Aciklama = kaynakIcerik.Aciklama,
                BirimId = kaynakIcerik.BirimId ?? cekiSatiri?.BirimId,
                KaynakProjeNo = kaynakIcerik.KaynakProjeNo
            }, true, null);
        }

        private static (decimal Stok, decimal Proje, decimal Tedarikci) KaynakKirilimlariniBol(
            SandikIcerik kaynak,
            decimal tasinanMiktar,
            decimal kaynakKonulanOnce)
        {
            if (kaynakKonulanOnce <= 0 || tasinanMiktar <= 0)
                return (0, 0, 0);

            var oran = Math.Min(tasinanMiktar / kaynakKonulanOnce, 1);
            var stokMevcut = Math.Max(kaynak.StokKarsilanan, 0);
            var projeMevcut = Math.Max(kaynak.ProjeKarsilanan, 0);
            var tedarikciMevcut = Math.Max(kaynak.TedarikciKarsilanan, 0);
            var stok = stokMevcut * oran;
            var proje = projeMevcut * oran;
            var tedarikci = tedarikciMevcut * oran;
            var toplam = stok + proje + tedarikci;

            // Bozuk legacy kayıtlarda kırılım toplamı fiziksel miktarı aşabiliyor. Taşınan
            // kaynak toplamını tasinanMiktar ile sınırlandırıp oranları koruyoruz.
            if (toplam > tasinanMiktar && toplam > 0)
            {
                var duzeltmeOrani = tasinanMiktar / toplam;
                stok *= duzeltmeOrani;
                proje *= duzeltmeOrani;
                tedarikci *= duzeltmeOrani;
            }

            // Veritabanı kolonları 4 ondalık basamaklıdır. Aşağı yuvarlama, üç kırılımın
            // ayrı ayrı yuvarlanıp toplamda taşınan miktarı aşmasını önler.
            return (
                Math.Min(DortHaneyeAsagiYuvarla(stok), stokMevcut),
                Math.Min(DortHaneyeAsagiYuvarla(proje), projeMevcut),
                Math.Min(DortHaneyeAsagiYuvarla(tedarikci), tedarikciMevcut));
        }

        private static decimal DortHaneyeAsagiYuvarla(decimal value)
        {
            const decimal carpan = 10_000m;
            return Math.Floor(Math.Max(value, 0) * carpan) / carpan;
        }

        private static async Task TekilFiiliSandikNoVarsaGuncelleAsync(
            CekiSatiri cekiSatiri,
            SandikIcerik kaynakIcerik,
            SandikIcerik hedefIcerik,
            IReadOnlyCollection<SandikIcerik> mevcutIcerikler,
            Sandik kaynakSandik,
            Sandik hedefSandik,
            IGenericRepository<Sandik> sandikRepo,
            IGenericRepository<CekiSatiri> cekiSatiriRepo)
        {
            var tahsisler = new Dictionary<int, (decimal Tahsis, decimal Konulan)>();

            foreach (var mevcut in mevcutIcerikler)
            {
                var deger = mevcut.Id == kaynakIcerik.Id
                    ? (kaynakIcerik.TahsisMiktari, kaynakIcerik.KonulanAdet)
                    : mevcut.Id == hedefIcerik.Id
                        ? (hedefIcerik.TahsisMiktari, hedefIcerik.KonulanAdet)
                        : (mevcut.TahsisMiktari, mevcut.KonulanAdet);

                if (tahsisler.TryGetValue(mevcut.SandikId, out var toplam))
                    tahsisler[mevcut.SandikId] = (toplam.Tahsis + deger.Item1, toplam.Konulan + deger.Item2);
                else
                    tahsisler[mevcut.SandikId] = deger;
            }

            if (!mevcutIcerikler.Any(i => i.Id == hedefIcerik.Id && hedefIcerik.Id > 0))
                tahsisler[hedefIcerik.SandikId] = (hedefIcerik.TahsisMiktari, hedefIcerik.KonulanAdet);

            var aktifSandikIdleri = tahsisler
                .Where(x => x.Value.Tahsis > 0 || x.Value.Konulan > 0)
                .Select(x => x.Key)
                .Distinct()
                .ToList();

            // Parçalı tahsiste tek bir FiiliSandikNo gerçeği temsil edemez; mevcut değer
            // değiştirilmez. Yalnızca tek aktif tahsis kaldığında güvenle türetilir.
            if (aktifSandikIdleri.Count != 1)
                return;

            var tekSandikId = aktifSandikIdleri[0];
            string? tekSandikNo;

            if (tekSandikId == kaynakSandik.Id)
                tekSandikNo = kaynakSandik.SandikNo;
            else if (tekSandikId == hedefSandik.Id)
                tekSandikNo = hedefSandik.SandikNo;
            else
                tekSandikNo = (await sandikRepo.GetByIdAsync(tekSandikId))?.SandikNo;

            if (string.IsNullOrWhiteSpace(tekSandikNo))
                return;

            cekiSatiri.FiiliSandikNo = tekSandikNo;
            cekiSatiriRepo.Update(cekiSatiri);
        }

        private static async Task SahaAktarimHedefSandiginiSenkronizeEtAsync(
            CekiSatiri cekiSatiri,
            SandikIcerik kaynakIcerik,
            SandikIcerik hedefIcerik,
            IReadOnlyCollection<SandikIcerik> mevcutIcerikler,
            IGenericRepository<SahaAktarimKalemi> sahaAktarimKalemiRepo)
        {
            var aktifAktarimKalemleri = (await sahaAktarimKalemiRepo.FindAsync(k =>
                    k.SahaCekiSatiriId == cekiSatiri.Id &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal))
                .ToList();

            if (aktifAktarimKalemleri.Count == 0)
                return;

            var aktifSandikIdleri = new HashSet<int>();
            foreach (var mevcutIcerik in mevcutIcerikler)
            {
                var etkinIcerik = mevcutIcerik.Id == kaynakIcerik.Id
                    ? kaynakIcerik
                    : hedefIcerik.Id > 0 && mevcutIcerik.Id == hedefIcerik.Id
                        ? hedefIcerik
                        : mevcutIcerik;

                if (IcerikAktifMi(etkinIcerik))
                    aktifSandikIdleri.Add(etkinIcerik.SandikId);
            }

            if (hedefIcerik.Id <= 0 && IcerikAktifMi(hedefIcerik))
                aktifSandikIdleri.Add(hedefIcerik.SandikId);

            // Tek sandıkta kalan saha aktarımında defter fiziksel hedefi izler.
            // Parçalı tahsiste tek bir sandık gerçeği olmadığı için null bırakılır;
            // geri alma akışı içerik kayıtlarından güvenli biçimde karar verir.
            int? etkinSahaSandikId = aktifSandikIdleri.Count == 1
                ? aktifSandikIdleri.Single()
                : null;

            foreach (var aktarimKalemi in aktifAktarimKalemleri)
            {
                if (aktarimKalemi.SahaSandikId == etkinSahaSandikId)
                    continue;

                aktarimKalemi.SahaSandikId = etkinSahaSandikId;
                sahaAktarimKalemiRepo.Update(aktarimKalemi);
            }
        }

        private static string FormatAdet(decimal value)
        {
            if (decimal.Truncate(value) == value)
                return decimal.Truncate(value).ToString(CultureInfo.InvariantCulture);

            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
