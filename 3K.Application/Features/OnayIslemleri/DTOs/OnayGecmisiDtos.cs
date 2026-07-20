using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.OnayIslemleri.DTOs
{
    public sealed class OnayGecmisiKayitDto
    {
        public int Id { get; set; }
        public string IslemKodu { get; set; } = string.Empty;
        public string IslemAdi { get; set; } = string.Empty;
        public string IslemAciklamasi { get; set; } = string.Empty;
        public int TalepEdenKullaniciId { get; set; }
        public string TalepEdenKisi { get; set; } = string.Empty;
        public int? KararVerenKullaniciId { get; set; }
        public string? KararVerenKisi { get; set; }
        public int DurumId { get; set; }
        public string Durum { get; set; } = string.Empty;
        public DateTime TalepTarihi { get; set; }
        public DateTime? KararTarihi { get; set; }
        public string? KararAciklamasi { get; set; }
        public int CalistirmaDurumuId { get; set; }
        public string CalistirmaDurumu { get; set; } = string.Empty;
        public DateTime? CalistirmaBaslamaTarihi { get; set; }
        public DateTime? CalistirmaBitisTarihi { get; set; }
        public string? CalistirmaHatasi { get; set; }
        public string? ReferansTipi { get; set; }
        public int? ReferansId { get; set; }
        public int? ProjeId { get; set; }
        public string? ProjeNo { get; set; }
        public string? HedefUrl { get; set; }
        public bool AksiyonAktifMi { get; set; }
        public CekiRevizyonOnayDetayiDto? RevizyonDetayi { get; set; }
    }

    /// <summary>
    /// Onay kaydında kullanıcıya gösterilebilecek güvenli revizyon snapshot'ı.
    /// Çalıştırılabilir command payload'ı, dosya içeriği ve doğrulama hash'leri
    /// bu DTO'ya bilinçli olarak dahil edilmez.
    /// </summary>
    public sealed class CekiRevizyonOnayDetayiDto
    {
        public int TalepId { get; set; }
        public CekiRevizyonOnizlemeSonuc Onizleme { get; set; } = new();
    }

    public sealed class OnayGecmisiListeDto
    {
        public List<OnayGecmisiKayitDto> Kayitlar { get; set; } = [];
        public int ToplamKayit { get; set; }
        public int Sayfa { get; set; }
        public int SayfaBoyutu { get; set; }
        public int ToplamSayfa { get; set; }
    }

    internal static class OnayGecmisiDtoMapper
    {
        public static OnayGecmisiKayitDto ToDto(this OnayGecmisiKaydi kayit)
        {
            return new OnayGecmisiKayitDto
            {
                Id = kayit.Id,
                IslemKodu = kayit.IslemKodu,
                IslemAdi = OnayIslemKodlari.DisplayName(kayit.IslemKodu),
                IslemAciklamasi = kayit.IslemAciklamasi,
                TalepEdenKullaniciId = kayit.TalepEdenKullaniciId,
                TalepEdenKisi = kayit.TalepEdenKisi,
                KararVerenKullaniciId = kayit.KararVerenKullaniciId,
                KararVerenKisi = kayit.KararVerenKisi,
                DurumId = (int)kayit.Durum,
                Durum = DurumMetni(kayit.Durum),
                TalepTarihi = kayit.TalepTarihi,
                KararTarihi = kayit.KararTarihi,
                KararAciklamasi = kayit.KararAciklamasi,
                CalistirmaDurumuId = (int)kayit.CalistirmaDurumu,
                CalistirmaDurumu = CalistirmaDurumuMetni(kayit.CalistirmaDurumu),
                CalistirmaBaslamaTarihi = kayit.CalistirmaBaslamaTarihi,
                CalistirmaBitisTarihi = kayit.CalistirmaBitisTarihi,
                CalistirmaHatasi = kayit.CalistirmaHatasi,
                ReferansTipi = kayit.ReferansTipi,
                ReferansId = kayit.ReferansId,
                ProjeId = kayit.ProjeId,
                ProjeNo = kayit.ProjeNo,
                HedefUrl = kayit.HedefUrl,
                AksiyonAktifMi = kayit.AksiyonAktifMi
            };
        }

        private static string DurumMetni(OnayDurumu durum)
        {
            return durum switch
            {
                OnayDurumu.Bekliyor => "Bekliyor",
                OnayDurumu.Onaylandi => "Onaylandı",
                OnayDurumu.Reddedildi => "Reddedildi",
                _ => "Bilinmiyor"
            };
        }

        private static string CalistirmaDurumuMetni(OnayCalistirmaDurumu durum)
        {
            return durum switch
            {
                OnayCalistirmaDurumu.Bekliyor => "Bekliyor",
                OnayCalistirmaDurumu.Calisiyor => "Çalışıyor",
                OnayCalistirmaDurumu.Basarili => "Başarılı",
                OnayCalistirmaDurumu.Basarisiz => "Başarısız",
                OnayCalistirmaDurumu.Atlandi => "Atlandı",
                _ => "Bilinmiyor"
            };
        }
    }
}
