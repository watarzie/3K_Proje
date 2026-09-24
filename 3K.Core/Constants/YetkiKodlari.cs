using _3K.Core.Enums;

namespace _3K.Core.Constants;

/// <summary>Her kod tek bir ekran, işlem veya alanı temsil eder; kök W alt izin vermez.</summary>
public static class YetkiKodlari
{
    public static class Ambalaj
    {
        public const string Listele = "ambalaj-uretim-listesi";
        public const string KayitDuzenle = "ambalaj-kayit-duzenle";
        public const string RaporGoruntule = "ambalaj-rapor-goruntule";
        public const string M3Goruntule = "ambalaj-m3-goruntule";
        public const string SarfGoruntule = "ambalaj-sarf-goruntule";
        public const string KaynakGoruntule = "ambalaj-kaynak-goruntule";
        public const string M3Duzenle = "ambalaj-m3-duzenle";
        public const string SarfDuzenle = "ambalaj-sarf-duzenle";
        public const string KaynakSenkronizeEt = "ambalaj-kaynak-senkronize";
        public const string UretimeAl = "ambalaj-uretime-al";
        public const string UretimdenCikar = "ambalaj-uretimden-cikar";
        public const string DahilEt = "ambalaj-dahil-et";
        public const string HaricTut = "ambalaj-haric-tut";
        public const string IlaveOlustur = "ambalaj-ilave-olustur";
        public const string SahaOlustur = "ambalaj-saha-olustur";
        public const string YedekOlustur = "ambalaj-yedek-olustur";
        public const string IcOlustur = "ambalaj-ic-olustur";
        public const string DigerOlustur = "ambalaj-diger-olustur";
        public const string ManuelProje = "ambalaj-manuel-proje";
        public const string TurDuzenle = "ambalaj-tur-duzenle";
        public const string CinsDuzenle = "ambalaj-cins-duzenle";
        public const string ProjeDuzenle = "ambalaj-proje-duzenle";
        public const string OlcuDuzenle = "ambalaj-olcu-duzenle";
        public const string TalepBilgileriDuzenle = "ambalaj-talep-duzenle";
        public const string DurumDuzenle = "ambalaj-durum-duzenle";
        public const string Iptal = "ambalaj-iptal";
        public const string GeriYukle = "ambalaj-geri-yukle";
        public const string KaynakMudahalesi = "ambalaj-kaynak-mudahalesi";
        public const string FormGoruntule = "ambalaj-form-goruntule";
        public const string FormIndir = "ambalaj-form-indir";
        public const string ExcelIndir = "ambalaj-excel-indir";
        public const string PdfIndir = "ambalaj-pdf-indir";
        public const string FormOlustur = "ambalaj-form-olustur";
        public const string FormYenidenOlustur = "ambalaj-form-yeniden-olustur";
        public const string UretimiTamamla = "ambalaj-uretimi-tamamla";
        public const string UretimiYenidenAc = "ambalaj-uretimi-yeniden-ac";
        public const string DurumuGeriAl = "ambalaj-durumu-geri-al";
        public const string KritikVeriDuzenle = "ambalaj-kritik-veri-duzenle";
        public const string FormSonrasiSecimDegistir = "ambalaj-form-sonrasi-secim-degistir";
        public const string TamamlananProjeyeEkle = "ambalaj-tamamlanan-projeye-ekle";
        public const string OlcuGoruntule = "ambalaj-olcu-goruntule";
        public const string PlanGoruntule = "ambalaj-plan-goruntule";
        public const string PlanOlustur = "ambalaj-plan-olustur";
        public const string BekleyenGoruntule = "ambalaj-bekleyen-goruntule";
        public const string UretimdeGoruntule = "ambalaj-uretimde-goruntule";
        public const string TamamlananGoruntule = "ambalaj-tamamlanan-goruntule";
        public const string GerceklesmeDuzelt = "ambalaj-gerceklesme-duzelt";
        public const string GecmisGoruntule = "ambalaj-gecmis-goruntule";
        public const string ProjeDetayGoruntule = "ambalaj-proje-detay-goruntule";
        public const string SandikListesiGoruntule = "ambalaj-sandik-listesi-goruntule";
        public const string M3RaporGoruntule = "ambalaj-m3-rapor-goruntule";
        public const string KayitEkle = "ambalaj-kayit-ekle";
        public const string KayitSil = "ambalaj-kayit-sil";
    }
    public static class Finans
    {
        public const string Modul = "finans-yonetimi";
        public const string GelirGoruntule = "finans-gelir-goruntule";
        public const string GiderGoruntule = "finans-gider-goruntule";
        public const string GiderYonet = "finans-gider-yonet";
        public const string SiparisOperasyonGoruntule = "finans-siparis-goruntule";
        public const string FaturaYonet = "finans-fatura-yonet";
        public const string TarifeYonet = "finans-tarife-yonet";
        public const string RaporGoruntule = "finans-rapor-goruntule";
        public const string ManuelIsEkle = "finans-is-ekle";
        public const string ManuelIsDuzenle = "finans-is-duzenle";
        public const string IsIptal = "finans-is-iptal";
        public const string TarihDegistir = "finans-tarih-degistir";
        public const string PoGir = "finans-po-gir";
        public const string PoDegistir = "finans-po-degistir";
        public const string BirimFiyatGoruntule = "finans-birim-fiyat-goruntule";
        public const string BirimFiyatDegistir = "finans-birim-fiyat-degistir";
        public const string KarlilikGoruntule = "finans-karlilik-goruntule";
        public const string ExcelAktar = "finans-excel-aktar";
        public const string PdfAktar = "finans-pdf-aktar";
        public const string GiderEkle = "finans-gider-ekle";
        public const string GiderDuzenle = "finans-gider-duzenle";
        public const string GiderKutuphanesiYonet = "finans-gider-kutuphanesi-yonet";
        public const string IsKutuphanesiYonet = "finans-is-kutuphanesi-yonet";
        public const string DuzenliIsYonet = "finans-duzenli-is-yonet";
        public const string ParasalVeriGoruntule = "finans-parasal-veri-goruntule";
        public const string TutarGoruntule = "finans-tutar-goruntule";
        public const string KayitGoruntule = "finans-kayit-goruntule";
        public const string FaturaGir = "finans-fatura-gir";
        public const string FaturaDegistir = "finans-fatura-degistir";
        public const string FaturaIptal = "finans-fatura-iptal";
        public const string PoIptal = "finans-po-iptal";
        public const string GiderIptal = "finans-gider-iptal";
        public const string KaliciSil = "finans-kalici-sil";
        public const string BelgeYukle = "finans-belge-yukle";
        public const string BelgeIndir = "finans-belge-indir";
        public const string SablonYonet = "finans-sablon-yonet";
        public const string DenetimGor = "finans-denetim-gor";
        public const string FiyatlandirmaDegistir = "finans-fiyatlandirma-degistir";
    }
}

public sealed record YetkiTanimi(int Id, string Kod, string Ad, int ParentId, YetkiTipi GerekenYetki, bool Kritik);

public static class YetkiKatalogu
{
    public static readonly IReadOnlyList<YetkiTanimi> Tum =
    [
        new(5100, YetkiKodlari.Ambalaj.KayitDuzenle, "Kayıt düzenleme", 46, YetkiTipi.W, false),
        new(5101, YetkiKodlari.Ambalaj.RaporGoruntule, "Rapor görüntüleme", 46, YetkiTipi.R, false),
        new(5102, YetkiKodlari.Ambalaj.M3Goruntule, "Üretim m³ görüntüleme", 46, YetkiTipi.R, false),
        new(5103, YetkiKodlari.Ambalaj.SarfGoruntule, "Sarf görüntüleme", 46, YetkiTipi.R, false),
        new(5104, YetkiKodlari.Ambalaj.KaynakGoruntule, "Kaynak görüntüleme", 46, YetkiTipi.R, false),
        new(5105, YetkiKodlari.Ambalaj.M3Duzenle, "m³ değiştirme", 46, YetkiTipi.W, true),
        new(5106, YetkiKodlari.Ambalaj.SarfDuzenle, "Sarf değiştirme", 46, YetkiTipi.W, true),
        new(5107, YetkiKodlari.Ambalaj.KaynakSenkronizeEt, "Kaynak eşitleme", 46, YetkiTipi.W, false),
        new(5108, YetkiKodlari.Ambalaj.UretimeAl, "Üretime alma", 46, YetkiTipi.W, false),
        new(5109, YetkiKodlari.Ambalaj.UretimdenCikar, "Üretimden çıkarma", 46, YetkiTipi.W, true),
        new(5110, YetkiKodlari.Ambalaj.DahilEt, "Yapılır seçme", 46, YetkiTipi.W, false),
        new(5111, YetkiKodlari.Ambalaj.HaricTut, "Yapılmaz seçme", 46, YetkiTipi.W, false),
        new(5112, YetkiKodlari.Ambalaj.IlaveOlustur, "İlave kayıt ekleme", 46, YetkiTipi.W, false),
        new(5113, YetkiKodlari.Ambalaj.SahaOlustur, "Saha kaydı ekleme", 46, YetkiTipi.W, false),
        new(5114, YetkiKodlari.Ambalaj.YedekOlustur, "Yedek kayıt ekleme", 46, YetkiTipi.W, false),
        new(5115, YetkiKodlari.Ambalaj.IcOlustur, "İç sandık ekleme", 46, YetkiTipi.W, false),
        new(5116, YetkiKodlari.Ambalaj.DigerOlustur, "Diğer kayıt ekleme", 46, YetkiTipi.W, false),
        new(5117, YetkiKodlari.Ambalaj.ManuelProje, "Manuel proje ekleme", 46, YetkiTipi.W, false),
        new(5118, YetkiKodlari.Ambalaj.TurDuzenle, "Kullanım türü değiştirme", 46, YetkiTipi.W, true),
        new(5119, YetkiKodlari.Ambalaj.CinsDuzenle, "Fiziksel cins değiştirme", 46, YetkiTipi.W, true),
        new(5120, YetkiKodlari.Ambalaj.ProjeDuzenle, "Proje bağlantısı değiştirme", 46, YetkiTipi.W, true),
        new(5121, YetkiKodlari.Ambalaj.OlcuDuzenle, "Ölçü değiştirme", 46, YetkiTipi.W, false),
        new(5122, YetkiKodlari.Ambalaj.TalepBilgileriDuzenle, "Talep bilgisi değiştirme", 46, YetkiTipi.W, false),
        new(5123, YetkiKodlari.Ambalaj.DurumDuzenle, "Üretim durumu değiştirme", 46, YetkiTipi.W, false),
        new(5124, YetkiKodlari.Ambalaj.Iptal, "Kayıt iptal etme", 46, YetkiTipi.W, true),
        new(5125, YetkiKodlari.Ambalaj.GeriYukle, "İptal edilmiş kaydı geri yükleme", 46, YetkiTipi.W, true),
        new(5126, YetkiKodlari.Ambalaj.KaynakMudahalesi, "Kaynak kaydına müdahale", 46, YetkiTipi.W, true),
        new(5127, YetkiKodlari.Ambalaj.FormGoruntule, "Üretim formu görüntüleme", 46, YetkiTipi.R, false),
        new(5128, YetkiKodlari.Ambalaj.FormIndir, "Üretim formu PDF alma", 46, YetkiTipi.R, false),
        new(5129, YetkiKodlari.Ambalaj.ExcelIndir, "Excel dışarı aktarma", 46, YetkiTipi.R, false),
        new(5130, YetkiKodlari.Ambalaj.PdfIndir, "PDF dışarı aktarma", 46, YetkiTipi.R, false),
        new(5131, YetkiKodlari.Ambalaj.FormOlustur, "İlk üretim formu oluşturma", 46, YetkiTipi.W, false),
        new(5132, YetkiKodlari.Ambalaj.FormYenidenOlustur, "Yeni form sürümü oluşturma", 46, YetkiTipi.W, true),
        new(5133, YetkiKodlari.Ambalaj.UretimiTamamla, "Üretimi tamamlama", 46, YetkiTipi.W, false),
        new(5134, YetkiKodlari.Ambalaj.UretimiYenidenAc, "Tamamlanan üretimi açma", 46, YetkiTipi.W, true),
        new(5135, YetkiKodlari.Ambalaj.DurumuGeriAl, "Üretim durumunu geri alma", 46, YetkiTipi.W, true),
        new(5136, YetkiKodlari.Ambalaj.KritikVeriDuzenle, "Kritik üretim verisi değiştirme", 46, YetkiTipi.W, true),
        new(5137, YetkiKodlari.Ambalaj.FormSonrasiSecimDegistir, "İlk form sonrası Yapılır/Yapılmaz", 46, YetkiTipi.W, true),
        new(5138, YetkiKodlari.Ambalaj.TamamlananProjeyeEkle, "Tamamlanan projeye üretim ekleme", 46, YetkiTipi.W, true),
        new(5139, YetkiKodlari.Ambalaj.OlcuGoruntule, "Ölçü görüntüleme", 46, YetkiTipi.R, false),
        new(5140, YetkiKodlari.Ambalaj.PlanGoruntule, "Ambalaj planı görüntüleme", 46, YetkiTipi.R, false),
        new(5141, YetkiKodlari.Ambalaj.PlanOlustur, "Ambalaj planı oluşturma", 46, YetkiTipi.W, false),
        new(5142, YetkiKodlari.Ambalaj.BekleyenGoruntule, "Bekleyen üretimleri görüntüleme", 46, YetkiTipi.R, false),
        new(5143, YetkiKodlari.Ambalaj.UretimdeGoruntule, "Üretimde listesini görüntüleme", 46, YetkiTipi.R, false),
        new(5144, YetkiKodlari.Ambalaj.TamamlananGoruntule, "Tamamlanan üretimleri görüntüleme", 46, YetkiTipi.R, false),
        new(5145, YetkiKodlari.Ambalaj.GerceklesmeDuzelt, "Üretim gerçekleşmesini düzeltme", 46, YetkiTipi.W, true),
        new(5146, YetkiKodlari.Ambalaj.GecmisGoruntule, "Üretim geçmişi görüntüleme", 46, YetkiTipi.R, false),
        new(5147, YetkiKodlari.Ambalaj.ProjeDetayGoruntule, "Üretim proje detayı", 46, YetkiTipi.R, false),
        new(5148, YetkiKodlari.Ambalaj.SandikListesiGoruntule, "Üretim sandık listesi", 46, YetkiTipi.R, false),
        new(5149, YetkiKodlari.Ambalaj.M3RaporGoruntule, "m³ raporu görüntüleme", 46, YetkiTipi.R, false),
        new(5150, YetkiKodlari.Ambalaj.KayitEkle, "Üretim kaydı ekleme", 46, YetkiTipi.W, false),
        new(5151, YetkiKodlari.Ambalaj.KayitSil, "Üretim kaydı silme", 46, YetkiTipi.W, true),
        new(5200, YetkiKodlari.Finans.GelirGoruntule, "Gelir görüntüleme", 47, YetkiTipi.R, false),
        new(5201, YetkiKodlari.Finans.GiderGoruntule, "Gider görüntüleme", 47, YetkiTipi.R, false),
        new(5202, YetkiKodlari.Finans.GiderYonet, "Gider yönetimi ekranı", 47, YetkiTipi.R, false),
        new(5203, YetkiKodlari.Finans.SiparisOperasyonGoruntule, "Sipariş görüntüleme", 47, YetkiTipi.R, false),
        new(5204, YetkiKodlari.Finans.FaturaYonet, "Fatura yönetimi", 47, YetkiTipi.W, false),
        new(5205, YetkiKodlari.Finans.TarifeYonet, "Tarife yönetimi", 47, YetkiTipi.W, false),
        new(5206, YetkiKodlari.Finans.RaporGoruntule, "Finans raporu görüntüleme", 47, YetkiTipi.R, false),
        new(5207, YetkiKodlari.Finans.ManuelIsEkle, "Finans işi ekleme", 47, YetkiTipi.W, false),
        new(5208, YetkiKodlari.Finans.ManuelIsDuzenle, "Finans işi düzenleme", 47, YetkiTipi.W, false),
        new(5209, YetkiKodlari.Finans.IsIptal, "Finans işi iptali", 47, YetkiTipi.W, true),
        new(5210, YetkiKodlari.Finans.TarihDegistir, "Finans tarihi değiştirme", 47, YetkiTipi.W, true),
        new(5211, YetkiKodlari.Finans.PoGir, "Sipariş açma", 47, YetkiTipi.W, false),
        new(5212, YetkiKodlari.Finans.PoDegistir, "Sipariş değiştirme", 47, YetkiTipi.W, true),
        new(5213, YetkiKodlari.Finans.BirimFiyatGoruntule, "Birim fiyat görüntüleme", 47, YetkiTipi.R, false),
        new(5214, YetkiKodlari.Finans.BirimFiyatDegistir, "Birim fiyat değiştirme", 47, YetkiTipi.W, true),
        new(5215, YetkiKodlari.Finans.KarlilikGoruntule, "Kârlılık ve oran görüntüleme", 47, YetkiTipi.R, false),
        new(5216, YetkiKodlari.Finans.ExcelAktar, "Finans Excel dışarı aktarma", 47, YetkiTipi.R, false),
        new(5217, YetkiKodlari.Finans.PdfAktar, "Finans PDF dışarı aktarma", 47, YetkiTipi.R, false),
        new(5218, YetkiKodlari.Finans.GiderEkle, "Gider ekleme", 47, YetkiTipi.W, false),
        new(5219, YetkiKodlari.Finans.GiderDuzenle, "Gider düzenleme", 47, YetkiTipi.W, false),
        new(5220, YetkiKodlari.Finans.GiderKutuphanesiYonet, "Gider kategori yönetimi", 47, YetkiTipi.W, false),
        new(5221, YetkiKodlari.Finans.IsKutuphanesiYonet, "İş kütüphanesi yönetimi", 47, YetkiTipi.W, false),
        new(5222, YetkiKodlari.Finans.DuzenliIsYonet, "Düzenli iş yönetimi", 47, YetkiTipi.W, false),
        new(5223, YetkiKodlari.Finans.ParasalVeriGoruntule, "Parasal veri ana izni", 47, YetkiTipi.R, false),
        new(5224, YetkiKodlari.Finans.TutarGoruntule, "Net/KDV/brüt tutar görüntüleme", 47, YetkiTipi.R, false),
        new(5225, YetkiKodlari.Finans.KayitGoruntule, "Finans kayıtlarını görüntüleme", 47, YetkiTipi.R, false),
        new(5226, YetkiKodlari.Finans.FaturaGir, "Fatura girişi", 47, YetkiTipi.W, false),
        new(5227, YetkiKodlari.Finans.FaturaDegistir, "Fatura değiştirme", 47, YetkiTipi.W, true),
        new(5228, YetkiKodlari.Finans.FaturaIptal, "Fatura iptali", 47, YetkiTipi.W, true),
        new(5229, YetkiKodlari.Finans.PoIptal, "Sipariş iptali", 47, YetkiTipi.W, true),
        new(5230, YetkiKodlari.Finans.GiderIptal, "Gider iptali", 47, YetkiTipi.W, true),
        new(5231, YetkiKodlari.Finans.KaliciSil, "Finans kaydını kalıcı silme", 47, YetkiTipi.W, true),
        new(5232, YetkiKodlari.Finans.BelgeYukle, "PDF belge yükleme", 47, YetkiTipi.W, false),
        new(5233, YetkiKodlari.Finans.BelgeIndir, "PDF belge indirme", 47, YetkiTipi.R, false),
        new(5234, YetkiKodlari.Finans.SablonYonet, "Özel iş şablonu yönetimi", 47, YetkiTipi.W, false),
        new(5235, YetkiKodlari.Finans.DenetimGor, "Finans değişiklik geçmişi", 47, YetkiTipi.R, false),
        new(5236, YetkiKodlari.Finans.FiyatlandirmaDegistir, "Fiyatlandırma değiştirme", 47, YetkiTipi.W, true),
    ];

    public static YetkiTanimi? Bul(string kod) => Tum.FirstOrDefault(x => x.Kod == kod);
}

