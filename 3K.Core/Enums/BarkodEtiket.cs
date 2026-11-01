namespace _3K.Core.Enums;

/// <summary>Barkod etiketinin çıktı biçimi: tarayıcıdan basılan PDF veya Godex yazıcıya ham gönderilen EZPL.</summary>
public enum BarkodEtiketBicimi
{
    Pdf = 1,
    Ezpl = 2
}

/// <summary>Ürün etiketinin kaynağı: çeki satırı ya da saha/yedek sandığına elle eklenen malzeme.</summary>
public enum BarkodEtiketUrunKaynagi
{
    CekiSatiri = 1,
    SandikIcerik = 2
}
