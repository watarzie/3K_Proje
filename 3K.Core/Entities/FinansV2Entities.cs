namespace _3K.Core.Entities;

public sealed class FinansKaynakBastirma : BaseEntity
{
    public string KaynakTuru { get; set; } = string.Empty;
    public string KaynakKayitId { get; set; } = string.Empty;
    public string KaynakBileseni { get; set; } = "NET";
    public string Aciklama { get; set; } = string.Empty;
}

public sealed class FinansBelge : BaseEntity
{
    public string HedefTuru { get; set; } = string.Empty;
    public int HedefId { get; set; }
    public int Surum { get; set; }
    public string OrijinalAd { get; set; } = string.Empty;
    public string GuvenliAd { get; set; } = string.Empty;
    public long Boyut { get; set; }
    public string IcerikTuru { get; set; } = "application/pdf";
    public string Hash { get; set; } = string.Empty;
    public string Yukleyen { get; set; } = string.Empty;
    // PDF ve metadata tek DB transaction'ında saklanır; dosya/DB yarım commit'i oluşmaz.
    public byte[] Icerik { get; set; } = Array.Empty<byte>();
}

public sealed class FinansIsSablonu : BaseEntity
{
    public string Kod { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public bool Aktif { get; set; } = true;
    public ICollection<FinansIsSablonSurumu> Surumler { get; set; } = new List<FinansIsSablonSurumu>();
}

public sealed class FinansIsSablonSurumu : BaseEntity
{
    public int FinansIsSablonuId { get; set; }
    public int Surum { get; set; }
    public string AlanlarJson { get; set; } = "[]";
    public FinansIsSablonu Sablon { get; set; } = null!;
}
