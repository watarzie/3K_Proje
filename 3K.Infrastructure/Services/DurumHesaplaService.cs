using _3K.Core.Interfaces;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// GridDurumu + UcKDurumu kesişiminden genel UrunDurum hesaplar.
    /// Kural: 3K tarafı "Paketlendi" veya "IadeEdildi" ise Grid durumunu ezer.
    /// </summary>
    public class DurumHesaplaService : IDurumHesaplaService
    {
        public string HesaplaGenelDurum(string gridDurumu, string ucKDurumu)
        {
            // ===== 3K Override Kuralları (Grid'i ezer) =====
            // Paketlendi → iş bitti, ürün sandıkta
            if (ucKDurumu == "Paketlendi")
                return "Tamamlandi";

            // İade edildi → Grid'e geri gönderildi
            if (ucKDurumu == "IadeEdildi")
                return "GeriGonderildi";

            // ===== Grid Tarafı Kuralları =====

            // Grid iptal ettiyse
            if (gridDurumu == "IptalEdildi")
                return "IptalVeyaPasif";

            // Grid bekletiyorsa
            if (gridDurumu == "Bekletiliyor")
                return "SonraGidecek";

            // ===== Grid SevkEdildi + 3K Durumları =====
            if (gridDurumu == "SevkEdildi")
            {
                return ucKDurumu switch
                {
                    "TamGeldi" => "Tamamlandi",
                    "KontrolEdildi" => "Tamamlandi",
                    "EksikGeldi" => "Eksik",
                    "Gelmedi" => "Kayip",
                    "Bekliyor" => "KismiGeldi",   // Sevk edildi ama henüz teslim alınmadı
                    _ => "KismiGeldi"
                };
            }

            // ===== Grid KismiSevkEdildi + 3K Durumları =====
            if (gridDurumu == "KismiSevkEdildi")
            {
                return ucKDurumu switch
                {
                    "TamGeldi" => "KismiTamamlandi",
                    "KontrolEdildi" => "KismiTamamlandi",
                    "EksikGeldi" => "Eksik",
                    "Gelmedi" => "Kayip",
                    "Bekliyor" => "KismiGeldi",
                    _ => "KismiGeldi"
                };
            }

            // ===== 3K teslim aldıysa ama Grid henüz sevk etmemiş (veri tutarsızlığı) =====
            if (ucKDurumu == "TamGeldi" || ucKDurumu == "KontrolEdildi")
                return "Tamamlandi";

            if (ucKDurumu == "EksikGeldi")
                return "Eksik";

            // ===== Varsayılan: Her iki taraf da bekliyor =====
            // Grid: Bekliyor, Uretimde, StokHazir → 3K henüz bir şey almamış
            return "Bekliyor";
        }
    }
}
