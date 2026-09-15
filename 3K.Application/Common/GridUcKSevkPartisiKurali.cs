using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Common
{
    /// <summary>
    /// GridSevkMiktari yalnızca o anda yolda olan partiyi, GelenMiktar ise bütün
    /// partilerden 3K'nın teslim aldığı kümülatif miktarı temsil eder. Bu kural iki
    /// sayacı birbirinden ayırır ve tekil/toplu akışların aynı üst sınırları kullanmasını sağlar.
    /// </summary>
    public static class GridUcKSevkPartisiKurali
    {
        public static GridUcKDevamSevkKarari DevamSevkiniDegerlendir(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            if (LegacyAktifPartiBelirsizMi(satir))
                return GridUcKDevamSevkKarari.UygunDegil;

            var legacyGelmediPartisiSonuclanmis =
                !satir.AktifGridSevkKarsilananMiktari.HasValue &&
                !satir.AktifGridSevkPartisiErkenSonuclandirildiMi.HasValue &&
                satir.UcKDurumuId == (int)UcKDurum.Gelmedi &&
                satir.GridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli;
            if (satir.YenidenSevkGerekliAdet > 0 && satir.KalanMiktar > 0 &&
                ((satir.GridSevkMiktari ?? 0) <= 0 ||
                 satir.AktifGridSevkPartisiErkenSonuclandirildiMi == true ||
                 legacyGelmediPartisiSonuclanmis ||
                 AktifPartiTamamlandiMi(satir)))
            {
                return GridUcKDevamSevkKarari.Uygun(
                    GridUcKDevamSevkTipi.YenidenSevk,
                    Math.Min(satir.YenidenSevkGerekliAdet, satir.KalanMiktar));
            }

            if (satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi &&
                (satir.GridSevkMiktari ?? 0) > 0 &&
                satir.ProjeGonderilen > 0 &&
                satir.KalanMiktar > 0 &&
                AktifPartiTamamlandiMi(satir))
            {
                return GridUcKDevamSevkKarari.Uygun(
                    GridUcKDevamSevkTipi.ProjeTransferTelafi,
                    Math.Min(satir.ProjeGonderilen, satir.KalanMiktar));
            }

            // Grid'de eksik kalmış satırın önceki aktif partisi 3K'da tamamlandıysa,
            // eski akış kalan proje ihtiyacını yeni bir tamamlama partisi olarak
            // sevk ediyordu. GridGelen-Gelen farkı bu senaryoda sıfır olabildiği için
            // TamGeldi parçalı devam hesabından ayrı tutulmalıdır.
            if (satir.GridDurumuId == (int)GridDurum.EksikGeldi &&
                satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi &&
                (satir.GridSevkMiktari ?? 0) > 0 &&
                satir.KalanMiktar > 0 &&
                AktifPartiTamamlandiMi(satir))
            {
                return GridUcKDevamSevkKarari.Uygun(
                    GridUcKDevamSevkTipi.EksikGridTamamlama,
                    satir.KalanMiktar);
            }

            var griddeSevkEdilmemisMiktar = Math.Max(satir.GridGelenAdet - satir.GelenMiktar, 0);
            var parcaliDevamUstSiniri = Math.Min(satir.KalanMiktar, griddeSevkEdilmemisMiktar);

            if (satir.GridDurumuId == (int)GridDurum.TamGeldi &&
                satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi &&
                (satir.GridSevkMiktari ?? 0) > 0 &&
                AktifPartiTamamlandiMi(satir) &&
                parcaliDevamUstSiniri > 0)
            {
                return GridUcKDevamSevkKarari.Uygun(
                    GridUcKDevamSevkTipi.ParcaliSevkDevami,
                    parcaliDevamUstSiniri);
            }

            return GridUcKDevamSevkKarari.UygunDegil;
        }

        public static bool UcKTarafindaIslemVar(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            return satir.UcKDurumuId != (int)UcKDurum.Bekliyor ||
                   satir.GelenMiktar > 0 ||
                   satir.KarsilananMiktar > 0;
        }

        public static decimal AktifPartiKarsilananMiktariniHesapla(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            var aktifParti = Math.Max(satir.GridSevkMiktari ?? 0, 0);
            if (aktifParti <= 0)
                return 0;

            if (satir.AktifGridSevkKarsilananMiktari.HasValue)
                return Sinirla(satir.AktifGridSevkKarsilananMiktari.Value, 0, aktifParti);

            // Legacy kayıtlar yalnız açıkça yorumlanabilen durumlarda türetilir.
            // Bekliyor/Gelmedi kaydını teslim edilmiş varsaymak veri çoğaltabilir.
            var tamamlanmisLegacyDurum =
                satir.UcKDurumuId == (int)UcKDurum.TamGeldi ||
                satir.UcKDurumuId == (int)UcKDurum.FazlaGeldi;

            // Kümülatif gelen aktif partiye tam eşitse ilk/tek parti güvenle
            // yorumlanabilir. Daha büyük veya küçük değer önceki partiyle mevcut
            // partiyi ayırmaya yetmez; bu durumda otomatik varsayım yapılmaz.
            return tamamlanmisLegacyDurum && satir.GelenMiktar == aktifParti
                ? aktifParti
                : 0;
        }

        public static bool LegacyAktifPartiBelirsizMi(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            if ((satir.GridSevkMiktari ?? 0) <= 0)
                return false;

            var sayacVar = satir.AktifGridSevkKarsilananMiktari.HasValue;
            var sonucVar = satir.AktifGridSevkPartisiErkenSonuclandirildiMi.HasValue;
            if (sayacVar != sonucVar)
                return true;

            if (sayacVar)
                return false;

            if (satir.UcKDurumuId == (int)UcKDurum.EksikGeldi)
                return true;

            var tamamlanmisLegacyDurum =
                satir.UcKDurumuId == (int)UcKDurum.TamGeldi ||
                satir.UcKDurumuId == (int)UcKDurum.FazlaGeldi;

            if (tamamlanmisLegacyDurum)
            {
                return satir.GelenMiktar != Math.Max(satir.GridSevkMiktari ?? 0, 0);
            }

            // Kolon eklenmeden önce başlatılmış fakat henüz karşılanmamış parti,
            // yalnız Bekliyor/Gelmedi ve fiziksel geleni 0 ise güvenle yorumlanır.
            return satir.GelenMiktar > 0 ||
                   (satir.UcKDurumuId != (int)UcKDurum.Bekliyor &&
                    satir.UcKDurumuId != (int)UcKDurum.Gelmedi);
        }

        public static decimal AktifPartiKalanMiktariniHesapla(CekiSatiri satir)
        {
            var aktifParti = Math.Max(satir.GridSevkMiktari ?? 0, 0);
            return Math.Max(aktifParti - AktifPartiKarsilananMiktariniHesapla(satir), 0);
        }

        public static bool AktifPartiTamamlandiMi(CekiSatiri satir)
        {
            return (satir.GridSevkMiktari ?? 0) > 0 &&
                   AktifPartiKalanMiktariniHesapla(satir) == 0;
        }

        public static bool AktifPartiTeslimeAcikMi(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            if ((satir.GridSevkMiktari ?? 0) <= 0)
                return false;

            // Eksik/Gelmedi/Geri Gönderildi ile açıkça sonuçlandırılan parti, eksik
            // borcu daha sonra stok/proje/tedarikçiyle sıfırlansa bile yeniden
            // "yolda" sayılamaz. Karar kümülatif geçmişten tahmin edilmez.
            if (AktifPartiSonuclandirildiMi(satir))
                return false;

            if (satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi)
                return true;

            if (satir.GridSevkDurumuId != (int)GridSevkDurum.YenidenSevkGerekli)
                return false;

            // Kısmi yeniden sevkin açık partisi false ile açıkça işaretlidir. Legacy
            // kayıtta ise yalnız hiç işlem görmemiş Bekliyor durumu güvenli kabul edilir.
            return satir.AktifGridSevkPartisiErkenSonuclandirildiMi == false ||
                   (!satir.AktifGridSevkPartisiErkenSonuclandirildiMi.HasValue &&
                    AktifPartiIslemBekliyorMu(satir));
        }

        public static bool AktifPartiSonuclandirildiMi(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            return (satir.GridSevkMiktari ?? 0) > 0 &&
                   satir.AktifGridSevkPartisiErkenSonuclandirildiMi == true;
        }

        public static void AktifPartiyiErkenSonuclandir(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            if ((satir.GridSevkMiktari ?? 0) > 0 &&
                satir.AktifGridSevkKarsilananMiktari.HasValue)
            {
                satir.AktifGridSevkPartisiErkenSonuclandirildiMi = true;
            }
        }

        public static void AktifPartiyiYenidenAc(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            satir.AktifGridSevkPartisiErkenSonuclandirildiMi =
                (satir.GridSevkMiktari ?? 0) > 0 &&
                satir.AktifGridSevkKarsilananMiktari.HasValue
                    ? false
                    : null;
        }

        /// <summary>
        /// Liste/UI kararlarında kullanılır. Yalnız teslimata açık, miktarı kalan
        /// ve legacy geçmişi tek anlamlı olan aktif partiyi aksiyona açar.
        /// </summary>
        public static bool AktifPartiTeslimEdilebilirMi(CekiSatiri satir)
        {
            return !LegacyAktifPartiBelirsizMi(satir) &&
                   AktifPartiTeslimeAcikMi(satir) &&
                   AktifPartiKalanMiktariniHesapla(satir) > 0;
        }

        /// <summary>
        /// Aynı aktif sevkiyatta beklenenden fazla ürün kaydı açılabileceğini gösterir.
        /// Fazla kayıt, normal miktar tamamen teslim alınmış olsa da sonradan fark
        /// edilebilir. Bu karar mevcut Fazla Geldi iş akışının tekrar davranışını
        /// değiştirmez; yalnız legacy belirsizliğini güvenli biçimde kapatır.
        /// </summary>
        public static bool AktifPartiFazlaTeslimeAcikMi(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            return !LegacyAktifPartiBelirsizMi(satir) &&
                   AktifPartiTeslimeAcikMi(satir);
        }

        /// <summary>
        /// Yeni Grid partisinin proje ihtiyacına karşılık gelen bölümünün mevcut
        /// sandık tahsislerine sığıp sığmadığını doğrular. Partinin kalan ihtiyacı
        /// aşan bölümü 3K tarafında "Fazla Geldi" akışına girebildiği için kapasite
        /// hesabına yalnız sandığa yerleşecek bölüm katılır. Mevcut fiziksel kaynaklar
        /// ve başka projeye çıkan miktar parent satırdaki kümülatif toplam üzerinden
        /// hesaba dahil edilir; tahsis dağılımı burada tahmin edilmez veya değiştirilmez.
        /// </summary>
        public static Result YeniSevkTahsisKapasitesiniDogrula(
            CekiSatiri satir,
            IReadOnlyCollection<SandikIcerik> icerikler,
            decimal sevkMiktari,
            decimal? trafoSevkAdedi = null)
        {
            ArgumentNullException.ThrowIfNull(satir);
            ArgumentNullException.ThrowIfNull(icerikler);

            var pozitifSevkMiktari = Math.Max(sevkMiktari, 0);
            if (pozitifSevkMiktari <= 0)
                return Result.Success();

            var tahsisSayisi = icerikler.Count;
            var toplamTahsisKapasitesi = icerikler.Sum(icerik =>
                TahsisMiktariniHesapla(satir, icerik, tahsisSayisi));
            var mevcutSandikMiktari = Math.Max(satir.KumulatifToplam, 0);
            // Durum/trafo değişikliği toplu işlemde henüz entity'ye uygulanmamıştır.
            // Kapasite fiziksel ihtiyaçtır; terminal durumun Kalan=0 gösterimi veya
            // hatalı ürünün zorunlu Kalan=1 gösterimi bu hesabı değiştirmemelidir.
            var sandigaGirecekKalanMiktar = Math.Max(
                satir.IstenenAdet - satir.KumulatifToplam - (trafoSevkAdedi ?? satir.TrafoSevkAdet), 0);
            var yeniPartidenSandigaGirecekMiktar = Math.Min(
                pozitifSevkMiktari,
                sandigaGirecekKalanMiktar);
            var sevkSonrasiGerekliTahsis = mevcutSandikMiktari + yeniPartidenSandigaGirecekMiktar;

            if (sevkSonrasiGerekliTahsis <= toplamTahsisKapasitesi)
                return Result.Success();

            return Result.Failure(
                $"Yeni Grid sevki sonrasında sandıklarda bulunması gereken miktar ({sevkSonrasiGerekliTahsis:0.####}), " +
                $"toplam sandık tahsis kapasitesini ({toplamTahsisKapasitesi:0.####}) aşıyor. " +
                "Çeki/revizyon miktarı değiştiyse Grid sevkinden önce sandık tahsislerini güncelleyin.",
                409);
        }

        /// <summary>
        /// Seçili sandıkta fiziksel olarak bulunan kümülatif Grid payını döndürür.
        /// Stok, proje ve tedarikçi karşılamaları Grid'e geri gönderilemez.
        /// </summary>
        public static decimal SandiktakiGridKarsilananMiktariniHesapla(SandikIcerik icerik)
        {
            ArgumentNullException.ThrowIfNull(icerik);

            var gridDisiKarsilanan =
                Math.Max(icerik.StokKarsilanan, 0) +
                Math.Max(icerik.ProjeKarsilanan, 0) +
                Math.Max(icerik.TedarikciKarsilanan, 0);

            return Math.Max(icerik.KonulanAdet - gridDisiKarsilanan, 0);
        }

        public static decimal GeriGonderilebilirMiktariHesapla(
            CekiSatiri satir,
            SandikIcerik? seciliIcerik)
        {
            ArgumentNullException.ThrowIfNull(satir);

            // Başka projeye verilen miktar artık bu satırın fiziksel Grid stoğunda
            // değildir. İade üst sınırı brüt GelenMiktar üzerinden kurulursa aynı
            // ürün hem hedef projede kullanılabilir hem de Grid'e iade edilebilir.
            var toplamGridKarsilanan = Math.Max(
                satir.GelenMiktar - satir.ProjeGonderilen,
                0);
            return seciliIcerik == null
                ? toplamGridKarsilanan
                : Math.Min(
                    toplamGridKarsilanan,
                    SandiktakiGridKarsilananMiktariniHesapla(seciliIcerik));
        }

        /// <summary>
        /// Geri gönderim yeni bir inbound teslim değildir. Bununla birlikte halen
        /// eksik teslim alınmış aktif parti sürerken iade açılırsa yeni sevk partisi
        /// o partinin yoldaki kalanını ezebilir. Bu nedenle fiziksel Grid miktarı
        /// bulunmalı ve açık aktif parti ya tamamlanmış ya da finalize edilmiş olmalıdır.
        /// </summary>
        public static bool GeriGonderimeAcikMi(CekiSatiri satir, SandikIcerik? seciliIcerik)
        {
            ArgumentNullException.ThrowIfNull(satir);

            if (LegacyAktifPartiBelirsizMi(satir) ||
                GeriGonderilebilirMiktariHesapla(satir, seciliIcerik) <= 0)
            {
                return false;
            }

            var tamamlanmamisAktifPartiTeslimde =
                AktifPartiTeslimeAcikMi(satir) &&
                !AktifPartiTamamlandiMi(satir);

            return !tamamlanmamisAktifPartiTeslimde;
        }

        /// <summary>
        /// Parent aktif-parti sayacı ile bütün sandık tahsislerinin nullable durumu ve
        /// toplamını doğrular. Taşıma gibi child dağılımını değiştiren akışlar, belirsiz
        /// bir legacy kaydı tahmin ederek dönüştürmek yerine bu kontrolle güvenli durur.
        /// </summary>
        public static Result AktifPartiSandikSayaclariniDogrula(
            CekiSatiri satir,
            IReadOnlyCollection<SandikIcerik> icerikler)
        {
            ArgumentNullException.ThrowIfNull(satir);
            ArgumentNullException.ThrowIfNull(icerikler);

            if (!AktifPartiSandikSayaclariBelirsizMi(satir, icerikler))
                return Result.Success();

            return Result.Failure(
                "Aktif Grid sevk partisinin sandık sayaçları tutarlı değil. Veri mutabakatı yapılmadan sandık dağılımı değiştirilemez.",
                409);
        }

        /// <summary>
        /// Aktif partinin seçili sandığa düşen ve henüz karşılanmamış payını hesaplar.
        /// Pay, parti başlamadan hemen önceki fiziksel sandık açıkları üzerinden
        /// dağıtılır; böylece dolu sandığa pay ayrılmaz ve ilk sandık parent partinin
        /// tamamını tek başına tüketemez.
        /// </summary>
        public static Result<decimal> AktifPartiSandikKalanMiktariniHesapla(
            CekiSatiri satir,
            SandikIcerik seciliIcerik,
            IReadOnlyCollection<SandikIcerik> icerikler)
        {
            ArgumentNullException.ThrowIfNull(satir);
            ArgumentNullException.ThrowIfNull(seciliIcerik);
            ArgumentNullException.ThrowIfNull(icerikler);

            var siraliIcerikler = icerikler.OrderBy(i => i.Id).ToList();
            var secili = siraliIcerikler.FirstOrDefault(i => i.Id == seciliIcerik.Id);
            if (secili == null)
                return Result<decimal>.Failure("Seçilen sandık içeriği bu ürüne ait değil.", 400);

            if (AktifPartiSandikSayaclariBelirsizMi(satir, siraliIcerikler))
            {
                return Result<decimal>.Failure(
                    "Aktif Grid sevk partisinin sandık sayaçları tutarlı değil. Veri mutabakatı yapılmadan 3K karşılaması uygulanamaz.",
                    409);
            }

            var hedefler = AktifPartiSandikHedefleriniHesapla(satir, siraliIcerikler);
            var hedef = hedefler.GetValueOrDefault(secili.Id);
            var karsilanan = Math.Max(secili.AktifGridSevkKarsilananMiktari ?? 0, 0);
            return Result<decimal>.Success(Math.Max(hedef - karsilanan, 0));
        }

        public static async Task YeniPartiBaslatAsync(
            IUnitOfWork unitOfWork,
            CekiSatiri satir,
            decimal sevkMiktari,
            GridUcKDevamSevkKarari devamKarari,
            IReadOnlyCollection<SandikIcerik>? oncedenYuklenenIcerikler = null)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(satir);

            satir.GridSevkMiktari = Math.Max(sevkMiktari, 0);
            satir.AktifGridSevkKarsilananMiktari = 0;
            satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;

            var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = oncedenYuklenenIcerikler ??
                (await icerikRepo.FindAsync(i => i.CekiSatiriId == satir.Id)).ToList();
            foreach (var icerik in icerikler)
            {
                if (icerik.AktifGridSevkKarsilananMiktari == 0)
                    continue;

                icerik.AktifGridSevkKarsilananMiktari = 0;
                icerikRepo.Update(icerik);
            }

            if (!devamKarari.YeniPartiMi)
                return;

            satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
            satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
            satir.TeslimTarihi = null;

            if (devamKarari.Tip == GridUcKDevamSevkTipi.EksikGridTamamlama)
            {
                satir.GridDurumuId = (int)GridDurum.TamGeldi;
                satir.GridGelenAdet = Math.Max(
                    satir.GridGelenAdet,
                    satir.IstenenAdet - satir.TrafoSevkAdet);
            }

            if (devamKarari.Tip == GridUcKDevamSevkTipi.YenidenSevk)
            {
                satir.YenidenSevkGerekliAdet = Math.Max(
                    satir.YenidenSevkGerekliAdet - sevkMiktari,
                    0);
                satir.GridSevkDurumuId = satir.YenidenSevkGerekliAdet > 0
                    ? (int)GridSevkDurum.YenidenSevkGerekli
                    : (int)GridSevkDurum.SevkEdildi;
            }
        }

        public static Result<decimal> TamKarsilamaMiktariniHesapla(
            IUnitOfWork unitOfWork,
            CekiSatiri satir,
            SandikIcerik? seciliIcerik,
            decimal sandikKalan,
            IReadOnlyCollection<SandikIcerik>? oncedenYuklenenIcerikler = null)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(satir);

            if (LegacyAktifPartiBelirsizMi(satir))
            {
                return Result<decimal>.Failure(
                    "Legacy kayıtta aktif Grid sevk partisinin karşılanan miktarı kesin olarak belirlenemiyor. Veri mutabakatı yapılmadan yeni 3K karşılaması uygulanamaz.",
                    409);
            }

            if (!AktifPartiTeslimeAcikMi(satir))
                return Result<decimal>.Failure("3K teslimine açık aktif bir Grid sevk partisi bulunmuyor.", 400);

            var icerikler = oncedenYuklenenIcerikler?
                    .OrderBy(i => i.Id)
                    .ToList() ??
                unitOfWork.GetRepository<SandikIcerik>()
                    .Queryable()
                    .Where(i => i.CekiSatiriId == satir.Id)
                    .OrderBy(i => i.Id)
                    .ToList();
            if (AktifPartiSandikSayaclariBelirsizMi(satir, icerikler))
            {
                return Result<decimal>.Failure(
                    "Aktif Grid sevk partisinin sandık sayaçları tutarlı değil. Veri mutabakatı yapılmadan 3K karşılaması uygulanamaz.",
                    409);
            }

            var aktifPartiKalan = AktifPartiKalanMiktariniHesapla(satir);
            if (aktifPartiKalan <= 0 || satir.KalanMiktar <= 0)
                return Result<decimal>.Success(0);

            var teslimUstSiniri = Math.Min(
                Math.Min(aktifPartiKalan, Math.Max(satir.KalanMiktar, 0)),
                Math.Max(sandikKalan, 0));

            if (seciliIcerik == null)
                return Result<decimal>.Success(teslimUstSiniri);

            var seciliPartiKalanResult = AktifPartiSandikKalanMiktariniHesapla(
                satir,
                seciliIcerik,
                icerikler);
            if (!seciliPartiKalanResult.IsSuccess)
                return seciliPartiKalanResult;

            return Result<decimal>.Success(Math.Min(teslimUstSiniri, seciliPartiKalanResult.Value));
        }

        public static Result AktifPartiKarsilamasiniKaydet(
            IUnitOfWork unitOfWork,
            CekiSatiri satir,
            SandikIcerik? seciliIcerik,
            decimal karsilananMiktar,
            bool sandikBosluguAranmasin = false,
            IReadOnlyCollection<SandikIcerik>? oncedenYuklenenIcerikler = null)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(satir);

            if (karsilananMiktar < 0)
                return Result.Failure("Aktif sevk partisinde karşılanan miktar negatif olamaz.");
            if (karsilananMiktar == 0)
                return Result.Success();

            var mevcutToplam = AktifPartiKarsilananMiktariniHesapla(satir);
            var aktifParti = Math.Max(satir.GridSevkMiktari ?? 0, 0);
            if (mevcutToplam + karsilananMiktar > aktifParti)
                return Result.Failure("Karşılanan miktar aktif Grid sevk partisini aşamaz.");

            var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = oncedenYuklenenIcerikler?
                    .OrderBy(i => i.Id)
                    .ToList() ??
                icerikRepo.Queryable()
                    .Where(i => i.CekiSatiriId == satir.Id)
                    .OrderBy(i => i.Id)
                    .ToList();
            if (AktifPartiSandikSayaclariBelirsizMi(satir, icerikler))
            {
                return Result.Failure(
                    "Aktif Grid sevk partisinin sandık sayaçları tutarlı değil. Veri mutabakatı yapılmadan 3K karşılaması uygulanamaz.",
                    409);
            }

            var hedefler = sandikBosluguAranmasin
                ? new Dictionary<int, decimal>()
                : AktifPartiSandikHedefleriniHesapla(satir, icerikler);

            if (seciliIcerik != null)
            {
                var secili = icerikler.FirstOrDefault(i => i.Id == seciliIcerik.Id);
                if (secili == null)
                    return Result.Failure("Seçilen sandık içeriği bu ürüne ait değil.", 400);

                var mevcut = Math.Max(secili.AktifGridSevkKarsilananMiktari ?? 0, 0);
                var tahsis = TahsisMiktariniHesapla(satir, secili, icerikler.Count);
                var sandikBosluk = Math.Max(tahsis - secili.KonulanAdet, 0);
                var aktifPartiPayKalan = sandikBosluguAranmasin
                    ? Math.Max(tahsis - mevcut, 0)
                    : Math.Max(hedefler.GetValueOrDefault(secili.Id) - mevcut, 0);
                var seciliUstSinir = sandikBosluguAranmasin
                    ? aktifPartiPayKalan
                    : Math.Min(aktifPartiPayKalan, sandikBosluk);
                if (karsilananMiktar > seciliUstSinir)
                    return Result.Failure("Karşılanan miktar seçilen sandığın aktif sevk payını aşamaz.");

                SayaclariBaslat(satir, icerikler, icerikRepo);

                secili.AktifGridSevkKarsilananMiktari = mevcut + karsilananMiktar;
                if (!sandikBosluguAranmasin)
                    secili.KonulanAdet += karsilananMiktar;
                icerikRepo.Update(secili);
            }
            else
            {
                var toplamSandikBoslugu = icerikler.Sum(icerik =>
                {
                    var mevcut = Math.Max(icerik.AktifGridSevkKarsilananMiktari ?? 0, 0);
                    var tahsis = TahsisMiktariniHesapla(satir, icerik, icerikler.Count);
                    return sandikBosluguAranmasin
                        ? Math.Max(tahsis - mevcut, 0)
                        : Math.Max(hedefler.GetValueOrDefault(icerik.Id) - mevcut, 0);
                });
                if (icerikler.Any() && karsilananMiktar > toplamSandikBoslugu)
                    return Result.Failure("Aktif sevk karşılaması sandık tahsislerine dağıtılamadı.");

                SayaclariBaslat(satir, icerikler, icerikRepo);

                var dagitilacak = karsilananMiktar;
                foreach (var icerik in icerikler)
                {
                    var mevcut = Math.Max(icerik.AktifGridSevkKarsilananMiktari ?? 0, 0);
                    var tahsis = TahsisMiktariniHesapla(satir, icerik, icerikler.Count);
                    var sandikBosluk = sandikBosluguAranmasin
                        ? Math.Max(tahsis - mevcut, 0)
                        : Math.Max(hedefler.GetValueOrDefault(icerik.Id) - mevcut, 0);
                    var eklenecek = Math.Min(sandikBosluk, dagitilacak);
                    if (eklenecek <= 0)
                        continue;

                    icerik.AktifGridSevkKarsilananMiktari = mevcut + eklenecek;
                    if (!sandikBosluguAranmasin)
                        icerik.KonulanAdet += eklenecek;
                    icerikRepo.Update(icerik);
                    dagitilacak -= eklenecek;
                    if (dagitilacak <= 0)
                        break;
                }

            }

            satir.AktifGridSevkKarsilananMiktari = mevcutToplam + karsilananMiktar;
            return Result.Success();
        }

        public static async Task AktifPartiKarsilamasiniSifirlaAsync(
            IUnitOfWork unitOfWork,
            CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(satir);

            var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = await icerikRepo.FindAsync(i => i.CekiSatiriId == satir.Id);

            // Aktif Grid sevki yoksa 0 bir sevk-partisi sayaci degildir. Tam reset
            // sonrasinda parent ve child alanlarini birlikte NULL'a dondurmek, sonraki
            // stok/proje/tedarikci karsilamasini sahte bir legacy uyumsuzluguyla kilitlemez.
            if ((satir.GridSevkMiktari ?? 0) <= 0)
            {
                satir.AktifGridSevkKarsilananMiktari = null;
                satir.AktifGridSevkPartisiErkenSonuclandirildiMi = null;
                foreach (var icerik in icerikler)
                {
                    if (!icerik.AktifGridSevkKarsilananMiktari.HasValue)
                        continue;

                    icerik.AktifGridSevkKarsilananMiktari = null;
                    icerikRepo.Update(icerik);
                }

                return;
            }

            satir.AktifGridSevkKarsilananMiktari = 0;
            foreach (var icerik in icerikler)
            {
                if (icerik.AktifGridSevkKarsilananMiktari == 0)
                    continue;

                icerik.AktifGridSevkKarsilananMiktari = 0;
                icerikRepo.Update(icerik);
            }
        }

        /// <summary>
        /// Yeni takip alanlari eklenmeden once baslamis, ancak mevcut verilerle tek
        /// anlamli olarak yorumlanabilen aktif partiyi nullable takip sozlesmesine
        /// tasir. Kaynak karsilamasi UcK durumunu degistirmeden once cagrilmalidir;
        /// aksi halde guvenli bir legacy Gelmedi/Bekliyor kaydi sonradan belirsiz hale
        /// gelebilir. Belirsiz gecmis tahmin edilmez ve islem 409 ile durdurulur.
        /// </summary>
        public static async Task<Result> GuvenliLegacyAktifPartiyiMaterializeEtAsync(
            IUnitOfWork unitOfWork,
            CekiSatiri satir,
            IReadOnlyCollection<SandikIcerik>? oncedenYuklenenIcerikler = null)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(satir);

            // Aktif parti yoksa nullable takip ciftinin gecmis bir resetten kalmis
            // olmasi kaynak karsilamasini ilgilendirmez. Asagidaki eslesme kontrolu
            // yalniz gercekten aktif bir sevk partisi varken anlamlidir.
            if ((satir.GridSevkMiktari ?? 0) <= 0)
                return Result.Success();

            var sayacVar = satir.AktifGridSevkKarsilananMiktari.HasValue;
            var sonucVar = satir.AktifGridSevkPartisiErkenSonuclandirildiMi.HasValue;
            if (sayacVar && sonucVar)
                return Result.Success();

            if (sayacVar != sonucVar)
            {
                return Result.Failure(
                    "Aktif Grid sevk partisinin takip alanlari tutarli degil. Veri mutabakati yapilmadan kaynak karsilamasi uygulanamaz.",
                    409);
            }

            if (LegacyAktifPartiBelirsizMi(satir))
            {
                return Result.Failure(
                    "Legacy kayitta aktif Grid sevk partisinin karsilanan miktari kesin olarak belirlenemiyor. Veri mutabakati yapilmadan kaynak karsilamasi uygulanamaz.",
                    409);
            }

            var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = oncedenYuklenenIcerikler?
                    .OrderBy(i => i.Id)
                    .ToList() ??
                (await icerikRepo.FindAsync(i => i.CekiSatiriId == satir.Id))
                    .OrderBy(i => i.Id)
                    .ToList();

            if (AktifPartiSandikSayaclariBelirsizMi(satir, icerikler))
            {
                return Result.Failure(
                    "Aktif Grid sevk partisinin sandik takip alanlari tutarli degil. Veri mutabakati yapilmadan kaynak karsilamasi uygulanamaz.",
                    409);
            }

            if (satir.UcKDurumuId == (int)UcKDurum.Bekliyor ||
                satir.UcKDurumuId == (int)UcKDurum.Gelmedi)
            {
                SayaclariBaslat(satir, icerikler, icerikRepo);
                satir.AktifGridSevkKarsilananMiktari = 0;
                satir.AktifGridSevkPartisiErkenSonuclandirildiMi =
                    satir.UcKDurumuId == (int)UcKDurum.Gelmedi;
                return Result.Success();
            }

            var hazirlamaResult = AktifPartiSayaclariniGeriAlmayaHazirla(
                satir,
                icerikler,
                icerikRepo);
            if (!hazirlamaResult.IsSuccess)
                return hazirlamaResult;

            satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
            return Result.Success();
        }

        public static async Task AktifPartiTakibiniTemizleAsync(
            IUnitOfWork unitOfWork,
            CekiSatiri satir,
            IReadOnlyCollection<SandikIcerik>? oncedenYuklenenIcerikler = null)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(satir);

            satir.AktifGridSevkKarsilananMiktari = null;
            satir.AktifGridSevkPartisiErkenSonuclandirildiMi = null;
            var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = oncedenYuklenenIcerikler ??
                (await icerikRepo.FindAsync(i => i.CekiSatiriId == satir.Id)).ToList();
            foreach (var icerik in icerikler)
            {
                if (!icerik.AktifGridSevkKarsilananMiktari.HasValue)
                    continue;

                icerik.AktifGridSevkKarsilananMiktari = null;
                icerikRepo.Update(icerik);
            }
        }

        public static Result<decimal> AktifPartiKarsilamasiniGeriAl(
            IUnitOfWork unitOfWork,
            CekiSatiri satir,
            SandikIcerik? seciliIcerik,
            decimal geriAlinanMiktar)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(satir);

            var geriAlinacak = Math.Max(geriAlinanMiktar, 0);
            var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = icerikRepo.Queryable()
                .Where(i => i.CekiSatiriId == satir.Id)
                .OrderBy(i => i.Id)
                .ToList();
            var hazirlamaResult = AktifPartiSayaclariniGeriAlmayaHazirla(
                satir,
                icerikler,
                icerikRepo);
            if (!hazirlamaResult.IsSuccess)
                return Result<decimal>.Failure(
                    hazirlamaResult.Error!.Message,
                    hazirlamaResult.StatusCode);

            // Grid payi 0 olan bir sandigin resetinde fiziksel geri alma yoktur;
            // yine de yukaridaki guvenli legacy materializasyonu tamamlanmalidir.
            if (geriAlinacak <= 0)
                return Result<decimal>.Success(0);

            var mevcutToplam = AktifPartiKarsilananMiktariniHesapla(satir);

            if (seciliIcerik != null)
            {
                var secili = icerikler.FirstOrDefault(i => i.Id == seciliIcerik.Id);
                if (secili == null)
                    return Result<decimal>.Failure("Seçilen sandık içeriği bu ürüne ait değil.", 400);

                var seciliMevcut = Math.Max(secili.AktifGridSevkKarsilananMiktari ?? 0, 0);
                var secilidenGeriAlinan = Math.Min(seciliMevcut, geriAlinacak);
                if (secilidenGeriAlinan <= 0)
                    return Result<decimal>.Success(0);

                secili.AktifGridSevkKarsilananMiktari = seciliMevcut - secilidenGeriAlinan;
                satir.AktifGridSevkKarsilananMiktari = Math.Max(mevcutToplam - secilidenGeriAlinan, 0);
                icerikRepo.Update(secili);
                return Result<decimal>.Success(secilidenGeriAlinan);
            }

            if (!satir.AktifGridSevkKarsilananMiktari.HasValue)
                return Result<decimal>.Success(0);

            var kalan = Math.Min(mevcutToplam, geriAlinacak);
            var gerceklesenGeriAlma = 0m;
            foreach (var icerik in icerikler.OrderByDescending(i => i.Id))
            {
                var mevcut = Math.Max(icerik.AktifGridSevkKarsilananMiktari ?? 0, 0);
                var dusulecek = Math.Min(mevcut, kalan);
                if (dusulecek <= 0)
                    continue;

                icerik.AktifGridSevkKarsilananMiktari = mevcut - dusulecek;
                icerikRepo.Update(icerik);
                kalan -= dusulecek;
                gerceklesenGeriAlma += dusulecek;
                if (kalan <= 0)
                    break;
            }

            if (!icerikler.Any())
                gerceklesenGeriAlma = Math.Min(mevcutToplam, geriAlinacak);

            satir.AktifGridSevkKarsilananMiktari = Math.Max(mevcutToplam - gerceklesenGeriAlma, 0);
            return Result<decimal>.Success(gerceklesenGeriAlma);
        }

        /// <summary>
        /// Sandık bazlı geri alma, daha önce Eksik Geldi ile kesinleştirilmiş aktif
        /// parti eksiğini hükümsüz bırakır. Yalnız gerçekten geri alınan aktif miktar
        /// varsa çalışır; böylece aynı sıfırlama tekrarlandığında borç ikinci kez düşmez.
        /// </summary>
        public static void SandikBazliSifirlamaSonrasiPartiyiYenidenAc(
            CekiSatiri satir,
            decimal geriAlinanAktifMiktar,
            bool eksikPartiFinalizasyonuVardi,
            decimal finalizasyonAnindakiAktifPartiEksigi)
        {
            ArgumentNullException.ThrowIfNull(satir);

            if (geriAlinanAktifMiktar > 0)
            {
                if (eksikPartiFinalizasyonuVardi &&
                    finalizasyonAnindakiAktifPartiEksigi > 0)
                {
                    satir.YenidenSevkGerekliAdet = Math.Max(
                        satir.YenidenSevkGerekliAdet - finalizasyonAnindakiAktifPartiEksigi,
                        0);
                }

                // Kümülatif GelenMiktar önceki partileri taşımaya devam eder. Bu üç
                // alan ise yalnız yeniden açılan mevcut partinin işlem durumudur.
                satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
                satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
                satir.TeslimTarihi = null;
                satir.UcKAciklama = null;
                AktifPartiyiYenidenAc(satir);
            }

            SifirlamaSonrasiGridSevkDurumunuSenkronizeEt(satir);
        }

        public static void SifirlamaSonrasiGridSevkDurumunuSenkronizeEt(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            if (satir.YenidenSevkGerekliAdet > 0)
            {
                satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
                return;
            }

            if (satir.GridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli)
            {
                satir.GridSevkDurumuId = (satir.GridSevkMiktari ?? 0) > 0
                    ? (int)GridSevkDurum.SevkEdildi
                    : (int)GridSevkDurum.Bekliyor;
            }
        }

        private static decimal TahsisMiktariniHesapla(CekiSatiri satir, SandikIcerik icerik, int tahsisSayisi)
        {
            if (icerik.TahsisMiktari > 0)
                return icerik.TahsisMiktari;

            return tahsisSayisi <= 1
                ? Math.Max(satir.IstenenAdet, 0)
                : Math.Max(icerik.KonulanAdet, 0);
        }

        private static Dictionary<int, decimal> AktifPartiSandikHedefleriniHesapla(
            CekiSatiri satir,
            IReadOnlyList<SandikIcerik> icerikler)
        {
            if (icerikler.Count == 0)
                return new Dictionary<int, decimal>();

            var partiOncesiBosluklar = icerikler
                .Select(icerik =>
                {
                    var aktifKarsilanan = Math.Max(icerik.AktifGridSevkKarsilananMiktari ?? 0, 0);
                    var partiOncesiKonulan = Math.Max(icerik.KonulanAdet - aktifKarsilanan, 0);
                    var tahsis = TahsisMiktariniHesapla(satir, icerik, icerikler.Count);
                    return Math.Max(tahsis - partiOncesiKonulan, 0);
                })
                .ToList();
            var toplamBosluk = partiOncesiBosluklar.Sum();
            if (toplamBosluk <= 0)
                return icerikler.ToDictionary(i => i.Id, _ => 0m);

            var dagitilacakParti = Math.Min(
                Math.Max(satir.GridSevkMiktari ?? 0, 0),
                toplamBosluk);
            var hedefler = SandikTahsisHelper.HesaplaKalanPaylari(
                dagitilacakParti,
                partiOncesiBosluklar,
                new decimal[icerikler.Count]);

            return icerikler
                .Select((icerik, index) => new { icerik.Id, Hedef = hedefler[index] })
                .ToDictionary(x => x.Id, x => x.Hedef);
        }

        private static bool AktifPartiSandikSayaclariBelirsizMi(
            CekiSatiri satir,
            IReadOnlyCollection<SandikIcerik> icerikler)
        {
            if (icerikler.Any(icerik =>
                icerik.AktifGridSevkKarsilananMiktari.HasValue !=
                satir.AktifGridSevkKarsilananMiktari.HasValue))
            {
                return true;
            }

            return satir.AktifGridSevkKarsilananMiktari.HasValue &&
                   icerikler.Count > 0 &&
                   icerikler.Sum(i => Math.Max(i.AktifGridSevkKarsilananMiktari ?? 0, 0)) !=
                   Math.Max(satir.AktifGridSevkKarsilananMiktari.Value, 0);
        }

        private static void SayaclariBaslat(
            CekiSatiri satir,
            IEnumerable<SandikIcerik> icerikler,
            IGenericRepository<SandikIcerik> icerikRepo)
        {
            if (satir.AktifGridSevkKarsilananMiktari.HasValue)
                return;

            // Legacy acik parti ilk kez materialize edilirken parent takip alanlari
            // atomik bir cift olarak baslatilmalidir. Yalniz sayaci yazmak, sonraki
            // istekte kaydi hakli olarak "belirsiz" durumuna dusurur.
            satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;

            foreach (var icerik in icerikler)
            {
                if (icerik.AktifGridSevkKarsilananMiktari.HasValue)
                    continue;

                icerik.AktifGridSevkKarsilananMiktari = 0;
                // Toplu handler'lar içerikleri AsNoTracking ön yükleyebilir. Aktif
                // parti başlatılırken miktar almayan child kayıtlar da kalıcı olarak
                // 0'a çekilmelidir; aksi halde parent/child nullable paritesi bozulur.
                icerikRepo.Update(icerik);
            }
        }

        private static Result AktifPartiSayaclariniGeriAlmayaHazirla(
            CekiSatiri satir,
            IReadOnlyCollection<SandikIcerik> icerikler,
            IGenericRepository<SandikIcerik> icerikRepo)
        {
            if (satir.AktifGridSevkKarsilananMiktari.HasValue)
            {
                return AktifPartiSandikSayaclariBelirsizMi(satir, icerikler)
                    ? Result.Failure(
                        "Aktif Grid sevk partisinin sandık sayaçları tutarlı değil. Veri mutabakatı yapılmadan geri alma uygulanamaz.",
                        409)
                    : Result.Success();
            }

            // Aktif parti yoksa fiziksel iade önceki tamamlanmış partilerden gelir;
            // aktif sayaç üretilecek bir dağılım bulunmaz ve kümülatif GelenMiktar
            // handler tarafından ayrıca azaltılır.
            if ((satir.GridSevkMiktari ?? 0) <= 0)
                return Result.Success();

            if (LegacyAktifPartiBelirsizMi(satir))
            {
                return Result.Failure(
                    "Legacy kayıtta aktif Grid sevk partisinin karşılanan miktarı kesin olarak belirlenemiyor. Veri mutabakatı yapılmadan geri alma uygulanamaz.",
                    409);
            }

            var legacyAktifKarsilanan = AktifPartiKarsilananMiktariniHesapla(satir);
            var sandikGridKarsilananlari = icerikler
                .Select(icerik => new
                {
                    Icerik = icerik,
                    Miktar = Math.Max(
                        icerik.KonulanAdet - icerik.StokKarsilanan -
                        icerik.ProjeKarsilanan - icerik.TedarikciKarsilanan,
                        0)
                })
                .ToList();

            if (icerikler.Count > 0 &&
                sandikGridKarsilananlari.Sum(x => x.Miktar) != legacyAktifKarsilanan)
            {
                return Result.Failure(
                    "Legacy aktif Grid sevk miktarının sandık dağılımı kesin olarak belirlenemiyor. Veri mutabakatı yapılmadan geri alma uygulanamaz.",
                    409);
            }

            satir.AktifGridSevkKarsilananMiktari = legacyAktifKarsilanan;
            // Legacy parent/child sayaclari materialize edildiginde durum bayragi da
            // ayni anda yazilmalidir. Secilen sandigin Grid payi 0 ise sonraki yeniden
            // acma adimi calismayabilir; bayragi bos birakmak kaydi belirsizlestirir.
            satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
            foreach (var sandikKarsilanan in sandikGridKarsilananlari)
            {
                sandikKarsilanan.Icerik.AktifGridSevkKarsilananMiktari = sandikKarsilanan.Miktar;
                icerikRepo.Update(sandikKarsilanan.Icerik);
            }

            return Result.Success();
        }

        private static decimal Sinirla(decimal deger, decimal altSinir, decimal ustSinir)
        {
            return Math.Min(Math.Max(deger, altSinir), ustSinir);
        }

        private static bool AktifPartiIslemBekliyorMu(CekiSatiri satir)
        {
            return satir.AktifGridSevkKarsilananMiktari.HasValue &&
                   satir.UcKDurumuId == (int)UcKDurum.Bekliyor &&
                   satir.UcKKarsilamaTipiId == (int)UcKDurum.Bekliyor &&
                   satir.TeslimTarihi == null;
        }
    }

    public enum GridUcKDevamSevkTipi
    {
        Yok = 0,
        YenidenSevk = 1,
        ProjeTransferTelafi = 2,
        ParcaliSevkDevami = 3,
        EksikGridTamamlama = 4
    }

    public readonly record struct GridUcKDevamSevkKarari(
        bool YeniPartiMi,
        GridUcKDevamSevkTipi Tip,
        decimal UstSinir)
    {
        public static GridUcKDevamSevkKarari UygunDegil => new(false, GridUcKDevamSevkTipi.Yok, 0);

        public static GridUcKDevamSevkKarari Uygun(GridUcKDevamSevkTipi tip, decimal ustSinir)
            => new(ustSinir > 0, tip, Math.Max(ustSinir, 0));
    }
}
