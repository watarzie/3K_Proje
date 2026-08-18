namespace _3K.Core.Models
{
    public enum IkiFaktorHataKodu
    {
        Yok = 0,
        GecersizTalep = 1,
        SuresiDolmusTalep = 2,
        DenemeLimitiAsildi = 3,
        GecersizKod = 4,
        TekrarKullanilanKod = 5,
        KullaniciBulunamadi = 6,
        KurulumZatenTamamlanmis = 7
    }

    public sealed record IkiFaktorTalepSonucu(
        string TalepTokeni,
        int GecerlilikSuresiSaniye);

    public sealed record IkiFaktorAyarDurumu(
        bool EtkinMi,
        DateTime? DogrulandiTarihiUtc);

    public sealed record IkiFaktorKurulumSonucu(
        bool Basarili,
        IkiFaktorHataKodu HataKodu,
        string? TalepTokeni = null,
        int? GecerlilikSuresiSaniye = null,
        string? QrKodDataUri = null,
        string? ManuelAnahtar = null);

    public sealed record IkiFaktorDogrulamaSonucu(
        bool Basarili,
        IkiFaktorHataKodu HataKodu,
        int? KullaniciId = null,
        bool BeniHatirla = false,
        IReadOnlyList<string>? KurtarmaKodlari = null,
        int? KalanDenemeSayisi = null);
}
