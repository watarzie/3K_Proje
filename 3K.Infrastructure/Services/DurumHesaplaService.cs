using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// GridDurumu + UcKDurumu kesişiminden genel UrunDurum hesaplar. ID bazlı.
    /// </summary>
    public class DurumHesaplaService : IDurumHesaplaService
    {
        public int HesaplaGenelDurum(int gridDurumuId, int ucKDurumuId)
        {
            // ===== 3K Override Kuralları (Grid'i ezer) =====
            if (ucKDurumuId == (int)UcKDurum.Paketlendi)
                return (int)UrunDurum.Tamamlandi;

            if (ucKDurumuId == (int)UcKDurum.IadeEdildi)
                return (int)UrunDurum.GeriGonderildi;

            // ===== Grid Tarafı Kuralları =====

            // Grid iptal ettiyse
            if (gridDurumuId == (int)GridDurum.Iptal)
                return (int)UrunDurum.IptalVeyaPasif;

            // Grid'de sipariş sürecinde
            if (gridDurumuId == (int)GridDurum.Sipariste)
                return (int)UrunDurum.Sipariste;

            // Grid'e gelmedi
            if (gridDurumuId == (int)GridDurum.Gelmedi)
                return (int)UrunDurum.Gelmedi;

            // Trafo sevk — kısmen veya tamamen trafoya gitti
            if (gridDurumuId == (int)GridDurum.TrafoSevk)
            {
                return ucKDurumuId switch
                {
                    (int)UcKDurum.TamGeldi => (int)UrunDurum.Tamamlandi,
                    (int)UcKDurum.KontrolEdildi => (int)UrunDurum.Tamamlandi,
                    (int)UcKDurum.EksikGeldi => (int)UrunDurum.Eksik,
                    (int)UcKDurum.Bekliyor => (int)UrunDurum.TrafoSevk,
                    _ => (int)UrunDurum.TrafoSevk
                };
            }

            // ===== Grid TamGeldi =====
            if (gridDurumuId == (int)GridDurum.TamGeldi)
            {
                return ucKDurumuId switch
                {
                    (int)UcKDurum.TamGeldi => (int)UrunDurum.Tamamlandi,
                    (int)UcKDurum.KontrolEdildi => (int)UrunDurum.Tamamlandi,
                    (int)UcKDurum.EksikGeldi => (int)UrunDurum.Eksik,
                    (int)UcKDurum.Gelmedi => (int)UrunDurum.Kayip,
                    (int)UcKDurum.Bekliyor => (int)UrunDurum.GriddeHazir,
                    _ => (int)UrunDurum.GriddeHazir
                };
            }

            // ===== Grid EksikGeldi =====
            if (gridDurumuId == (int)GridDurum.EksikGeldi)
            {
                return ucKDurumuId switch
                {
                    (int)UcKDurum.TamGeldi => (int)UrunDurum.KismiTamamlandi,
                    (int)UcKDurum.KontrolEdildi => (int)UrunDurum.KismiTamamlandi,
                    (int)UcKDurum.EksikGeldi => (int)UrunDurum.Eksik,
                    (int)UcKDurum.Gelmedi => (int)UrunDurum.Kayip,
                    (int)UcKDurum.Bekliyor => (int)UrunDurum.GriddeEksik,
                    _ => (int)UrunDurum.GriddeEksik
                };
            }

            // ===== 3K teslim aldıysa ama Grid henüz durum seçmemiş =====
            if (ucKDurumuId == (int)UcKDurum.TamGeldi || ucKDurumuId == (int)UcKDurum.KontrolEdildi)
                return (int)UrunDurum.Tamamlandi;

            if (ucKDurumuId == (int)UcKDurum.EksikGeldi)
                return (int)UrunDurum.Eksik;

            // 3K karşılama tipleri
            if (ucKDurumuId == (int)UcKDurum.ProjedenKarsilandi)
                return (int)UrunDurum.Tamamlandi;

            if (ucKDurumuId == (int)UcKDurum.StoktanKarsilandi)
                return (int)UrunDurum.Tamamlandi;

            if (ucKDurumuId == (int)UcKDurum.TedarikcidenGeldi)
                return (int)UrunDurum.Tamamlandi;

            if (ucKDurumuId == 11) // BaskaProyeVerildi (kaldırıldı, eski veriler için)
                return (int)UrunDurum.BaskaProyeVerildi;

            if (ucKDurumuId == (int)UcKDurum.GeriGonderildi)
                return (int)UrunDurum.GeriGonderildi;

            if (ucKDurumuId == (int)UcKDurum.HataliUrun)
                return (int)UrunDurum.HataliUrun;

            // Varsayılan: Her iki taraf da bekliyor
            return (int)UrunDurum.Bekliyor;
        }
    }
}
