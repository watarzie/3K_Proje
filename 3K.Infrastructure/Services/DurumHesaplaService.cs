using _3K.Core.Enums;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// GridDurumu + UcKDurumu kesişiminden genel UrunDurum hesaplar. ID bazlı.
    /// Ayrıca merkezi KalanMiktar tabanlı GenelDurum override'ı yapar.
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

        /// <summary>
        /// KURAL 2: Merkezi kalan miktar hesaplaması ve GenelDurum override'ı.
        /// Entity'deki computed KalanMiktar property'sini okur.
        /// KalanMiktar <= 0 ve HataliMiktar == 0 ise DurumId = Tamamlandi yapılır.
        /// KalanMiktar > 0 ise fiziksel sandık "Tam Geldi" olsa bile GenelDurum değiştirilmez.
        /// </summary>
        public void HesaplaKalanVeDurum(CekiSatiri satir)
        {
            // Entity'deki computed KalanMiktar zaten doğru formülü kullanıyor:
            // IstenenAdet - GelenMiktar - StokKarsilanan - ProjeKarsilanan - TedarikciKarsilanan - TrafoSevkAdet
            // HataliMiktar > 0 ise kalan en az 1 (entity'de korunan iş kuralı)
            var kalan = satir.KalanMiktar;

            if (kalan <= 0)
            {
                // Tüm kaynaklar toplamı hedefi karşıladı — Tamamlandı
                // HataliUyumsuzGonderim durumunda entity zaten kalan=1 döndürüyor, buraya düşmez
                satir.DurumId = (int)UrunDurum.Tamamlandi;
            }
            // kalan > 0 ise mevcut DurumId (HesaplaGenelDurum sonucu) korunur
            // Yani fiziksel sandık "Tam Geldi" olsa bile çeki satırı henüz tamamlanmamışsa
            // Eksik/KismiTamamlandi/Bekliyor gibi durumlar aktif kalır
        }
    }
}

