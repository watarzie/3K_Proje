using _3K.Core.Interfaces;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// GridDurumu + UcKDurumu kesişiminden genel UrunDurum hesaplar.
    /// Grid Durumları: TamGeldi, EksikGeldi, Gelmedi, TrafoSevk, Iptal, Sipariste, Bekliyor
    /// 3K Durumları: Bekliyor, TamGeldi, EksikGeldi, Gelmedi, KontrolEdildi, Paketlendi, IadeEdildi
    /// Kural: 3K tarafı "Paketlendi" veya "IadeEdildi" ise Grid durumunu ezer.
    /// </summary>
    public class DurumHesaplaService : IDurumHesaplaService
    {
        public string HesaplaGenelDurum(string gridDurumu, string ucKDurumu)
        {
            // ===== 3K Override Kuralları (Grid'i ezer) =====
            if (ucKDurumu == "Paketlendi")
                return "Tamamlandi";

            if (ucKDurumu == "IadeEdildi")
                return "GeriGonderildi";

            // ===== Grid Tarafı Kuralları =====

            // Grid iptal ettiyse
            if (gridDurumu == "Iptal")
                return "IptalVeyaPasif";

            // Grid'de sipariş sürecinde
            if (gridDurumu == "Sipariste")
                return "Sipariste";

            // Grid'e gelmedi
            if (gridDurumu == "Gelmedi")
                return "Gelmedi";

            // Trafo sevk — kısmen veya tamamen trafoya gitti
            if (gridDurumu == "TrafoSevk")
            {
                return ucKDurumu switch
                {
                    "TamGeldi" => "Tamamlandi",
                    "KontrolEdildi" => "Tamamlandi",
                    "EksikGeldi" => "Eksik",
                    "Bekliyor" => "TrafoSevk",
                    _ => "TrafoSevk"
                };
            }

            // ===== Grid TamGeldi =====
            if (gridDurumu == "TamGeldi")
            {
                return ucKDurumu switch
                {
                    "TamGeldi" => "Tamamlandi",
                    "KontrolEdildi" => "Tamamlandi",
                    "EksikGeldi" => "Eksik",
                    "Gelmedi" => "Kayip",
                    "Bekliyor" => "GriddeHazir",   // Grid'de tam, henüz 3K'ya gelmedi
                    _ => "GriddeHazir"
                };
            }

            // ===== Grid EksikGeldi =====
            if (gridDurumu == "EksikGeldi")
            {
                return ucKDurumu switch
                {
                    "TamGeldi" => "KismiTamamlandi",
                    "KontrolEdildi" => "KismiTamamlandi",
                    "EksikGeldi" => "Eksik",
                    "Gelmedi" => "Kayip",
                    "Bekliyor" => "GriddeEksik",   // Grid'de eksik, henüz 3K'ya gelmedi
                    _ => "GriddeEksik"
                };
            }

            // ===== 3K teslim aldıysa ama Grid henüz durum seçmemiş =====
            if (ucKDurumu == "TamGeldi" || ucKDurumu == "KontrolEdildi")
                return "Tamamlandi";

            if (ucKDurumu == "EksikGeldi")
                return "Eksik";

            // 3K karşılama tipleri
            if (ucKDurumu == "ProjedenKarsilandi")
                return "Tamamlandi";

            if (ucKDurumu == "StoktanKarsilandi")
                return "Tamamlandi";

            if (ucKDurumu == "TedarikcidenGeldi")
                return "Tamamlandi";

            if (ucKDurumu == "BaskaProyeVerildi")
                return "BaskaProyeVerildi";

            if (ucKDurumu == "GeriGonderildi")
                return "GeriGonderildi";

            if (ucKDurumu == "HataliUrun")
                return "HataliUrun";

            // Varsayılan: Her iki taraf da bekliyor
            return "Bekliyor";
        }
    }
}
