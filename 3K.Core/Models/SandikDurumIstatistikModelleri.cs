using _3K.Core.Enums;

namespace _3K.Core.Models
{
    /// <summary>
    /// Sandık açılış/kapanış istatistiği süzgeci. Tarihler gün olarak kapsayıcıdır; proje araması proje numarasında,
    /// müşteri araması müşteri adında geçen metindir (büyük/küçük harf duyarsız).
    /// </summary>
    public sealed record SandikDurumIstatistikKriteri(
        DateTime? Baslangic,
        DateTime? Bitis,
        string? ProjeArama,
        string? Musteri);

    /// <summary>Tek açılış/kapanış olayı (sandık zaman çizelgesi ve olay listesi).</summary>
    public sealed class SandikDurumOlayModeli
    {
        public int Id { get; init; }
        public int ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public string Musteri { get; init; } = string.Empty;
        public int? SandikId { get; init; }
        public string SandikNo { get; init; } = string.Empty;
        public SandikDurumOlayTuru Tur { get; init; }
        public string TurMetni { get; init; } = string.Empty;
        public SandikDurumOlayKaynagi Kaynak { get; init; }
        public string KaynakMetni { get; init; } = string.Empty;
        public string? EskiDurumMetni { get; init; }
        public string YeniDurumMetni { get; init; } = string.Empty;
        public int? SebepId { get; init; }
        public string? SebepMetni { get; init; }
        public string? Not { get; init; }
        public int? KullaniciId { get; init; }
        public string? KullaniciAdi { get; init; }
        public DateTime Tarih { get; init; }
    }

    /// <summary>
    /// Süzgeçteki olayların özeti. Açılmalar kaynağına göre ayrılır: elle (sebepli), otomatik (ürün güncelleme,
    /// taşıma, saha aktarımı gibi işlemlerin yan etkisi) ve geçmişten aktarılan (sebebi bilinmeyen eski kayıt).
    /// </summary>
    public sealed class SandikDurumIstatistikOzeti
    {
        public int KapanmaSayisi { get; init; }
        public int TopluKapanmaSayisi { get; init; }
        public int OtomatikKapanmaSayisi { get; init; }
        public int AcilmaSayisi { get; init; }
        public int ElleAcilmaSayisi { get; init; }
        public int OtomatikAcilmaSayisi { get; init; }
        public int GecmistenAktarilanAcilmaSayisi { get; init; }
        /// <summary>En az bir kez geri açılmış farklı sandık sayısı.</summary>
        public int AcilanSandikSayisi { get; init; }
        public int ProjeSayisi { get; init; }
        /// <summary>Sayfalı sekmelerin toplamları (Müşteriler, Kişiler, Olaylar).</summary>
        public int MusteriSayisi { get; init; }
        public int KisiSayisi { get; init; }
        public int OlaySayisi { get; init; }
    }

    /// <summary>Açılma sebebi dağılımı; sebepsiz açılmalar kaynağına göre ayrı satırdır.</summary>
    public sealed class SandikDurumSebepSatiri
    {
        public int? SebepId { get; init; }
        public string Sebep { get; init; } = string.Empty;
        public int Sayi { get; init; }
    }

    /// <summary>
    /// Kişi bazında kapatma ve geri açma sayıları. Bütün kaynaklar sayılır: otomatik olay, onu tetikleyen işlemi yapan
    /// kullanıcıya yazılır; kullanıcısı olmayan (arka plan, eski kayıt) olaylar "Bilinmiyor / sistem" satırındadır.
    /// </summary>
    public sealed class SandikDurumKisiSatiri
    {
        public int? KullaniciId { get; init; }
        public string KullaniciAdi { get; init; } = string.Empty;
        public int KapatmaSayisi { get; init; }
        public int GeriAcmaSayisi { get; init; }
    }

    /// <summary>Proje bazında sayılar.</summary>
    public sealed class SandikDurumProjeSatiri
    {
        public int ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public string Musteri { get; init; } = string.Empty;
        public int KapanmaSayisi { get; init; }
        public int AcilmaSayisi { get; init; }
        public int ElleAcilmaSayisi { get; init; }
        public int AcilanSandikSayisi { get; init; }
    }

    /// <summary>Müşteri bazında sayılar (müşterinin projelerinin toplamı).</summary>
    public sealed class SandikDurumMusteriSatiri
    {
        public string Musteri { get; init; } = string.Empty;
        public int ProjeSayisi { get; init; }
        public int KapanmaSayisi { get; init; }
        public int AcilmaSayisi { get; init; }
        public int ElleAcilmaSayisi { get; init; }
    }

    /// <summary>En çok geri açılan sandıklar.</summary>
    public sealed class SandikDurumSandikSatiri
    {
        public int? SandikId { get; init; }
        public int ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public string Musteri { get; init; } = string.Empty;
        public string SandikNo { get; init; } = string.Empty;
        public int AcilmaSayisi { get; init; }
        public int ElleAcilmaSayisi { get; init; }
        public int KapanmaSayisi { get; init; }
        public DateTime SonAcilma { get; init; }
    }

    /// <summary>
    /// Sandık açılış/kapanış istatistiğinin özeti: sayılar, sebep dağılımı (sebep sayısıyla sınırlı) ve en çok geri açılan
    /// <see cref="EnCokAcilanSiniri"/> sandık. Proje, müşteri, kişi ve olay tabloları büyüyebildiği için ayrı uçlardan
    /// sayfalı okunur (<see cref="SayfaliSonuc{T}"/>); Excel bütün satırları yazar.
    /// </summary>
    public sealed class SandikDurumIstatistigi
    {
        public const int EnCokAcilanSiniri = 20;

        public SandikDurumIstatistikKriteri Kriter { get; init; } = new(null, null, null, null);
        public DateTime OlusturmaTarihi { get; init; }
        public SandikDurumIstatistikOzeti Ozet { get; init; } = new();
        public IReadOnlyList<SandikDurumSebepSatiri> Sebepler { get; init; } = [];
        public IReadOnlyList<SandikDurumSandikSatiri> EnCokAcilanlar { get; init; } = [];
    }
}
