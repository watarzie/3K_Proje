namespace _3K.Application.DTOs
{
    public class ProjeDto
    {
        public int Id { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public string Durum { get; set; } = string.Empty;
        public DateTime? PlanlananSevkTarihi { get; set; }
        public string SorumluKisi { get; set; } = string.Empty;
        public int SandikSayisi { get; set; }
        public int ToplamUrunSayisi { get; set; }
        public int TamamlananUrunSayisi { get; set; }
    }

    public class ProjeOlusturDto
    {
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public DateTime? PlanlananSevkTarihi { get; set; }
        public string SorumluKisi { get; set; } = string.Empty;
    }

    public class CekiYuklemeResultDto
    {
        public int CekiId { get; set; }
        public int SatirSayisi { get; set; }
        public int SandikSayisi { get; set; }
        public string Mesaj { get; set; } = string.Empty;
    }

    public class CekiSatiriDto
    {
        public int Id { get; set; }
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public int IstenenAdet { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string CekideGecenSandikNo { get; set; } = string.Empty;
        public string? FiiliSandikNo { get; set; }
        public string? Remarks { get; set; }
        public string Durum { get; set; } = string.Empty;
        public string? PaketleyenBasHarf { get; set; }
        public string? KontrolEdenBasHarf { get; set; }
        public int KonulanAdet { get; set; }
        public int EksikAdet { get; set; }
    }

    public class SandikDto
    {
        public int Id { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string Durum { get; set; } = string.Empty;
        public string? DepoLokasyonu { get; set; }
        public int UrunSayisi { get; set; }
    }

    public class SandikDetayDto
    {
        public int Id { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string Durum { get; set; } = string.Empty;
        public string? DepoLokasyonu { get; set; }
        public List<SandikIcerikDto> Icerikler { get; set; } = new();
    }

    public class SandikIcerikDto
    {
        public int Id { get; set; }
        public int CekiSatiriId { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public int IstenenAdet { get; set; }
        public int KonulanAdet { get; set; }
        public int EksikAdet { get; set; }
        public string Durum { get; set; } = string.Empty;
        public string? PaketleyenBasHarf { get; set; }
        public string? KontrolEdenBasHarf { get; set; }
        public string? Remarks { get; set; }
    }

    public class UrunGuncelleDto
    {
        public int CekiSatiriId { get; set; }
        public int? KonulanAdet { get; set; }
        public int? EksikAdet { get; set; }
        public int? PaketleyenId { get; set; }
        public int? KontrolEdenId { get; set; }
        public string? Aciklama { get; set; }
        public string? YeniFiiliSandikNo { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
    }

    public class FBTransferDto
    {
        public int CekiSatiriId { get; set; }
        public string AsilFB { get; set; } = string.Empty;
        public string AlinanFB { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string? Neden { get; set; }
        public string? IadeDurumu { get; set; }
        public string? Aciklama { get; set; }
        public int KullaniciId { get; set; }
    }

    public class FBTransferResultDto
    {
        public int Id { get; set; }
        public string AsilFB { get; set; } = string.Empty;
        public string AlinanFB { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string? Neden { get; set; }
        public string? IadeDurumu { get; set; }
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; }
    }

    public class StokKarsilamaDto
    {
        public int CekiSatiriId { get; set; }
        public int StokKaydiId { get; set; }
        public int Miktar { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
    }

    public class StokKaydiDto
    {
        public int Id { get; set; }
        public string MalzemeKodu { get; set; } = string.Empty;
        public string MalzemeAdi { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string? Lokasyon { get; set; }
        public string? KaynakProje { get; set; }
        public string Durum { get; set; } = string.Empty;
    }

    public class StokKaydiOlusturDto
    {
        public string MalzemeKodu { get; set; } = string.Empty;
        public string MalzemeAdi { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string? Lokasyon { get; set; }
        public string? KaynakProje { get; set; }
    }

    public class HareketGecmisiDto
    {
        public int Id { get; set; }
        public string Islem { get; set; } = string.Empty;
        public string ReferansTipi { get; set; } = string.Empty;
        public string? ReferansId { get; set; }
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
        public string? Aciklama { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public DateTime Tarih { get; set; }
    }

    public class KullaniciDto
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public string BasHarf { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class KullaniciOlusturDto
    {
        public string AdSoyad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
    }

    public class LoginResultDto
    {
        public string Token { get; set; } = string.Empty;
        public KullaniciDto Kullanici { get; set; } = null!;
    }

    public class SandikDegistirDto
    {
        public int CekiSatiriId { get; set; }
        public string YeniFiiliSandikNo { get; set; } = string.Empty;
        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
    }
}
