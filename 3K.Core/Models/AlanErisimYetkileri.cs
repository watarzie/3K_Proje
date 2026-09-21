namespace _3K.Core.Models;

/// <summary>Parasal alt alanlar ana izinle birlikte değerlendirilir.</summary>
public sealed record AlanErisimYetkileri(
    bool Olcu, bool UretimM3, bool Sarf, bool ParasalVeri,
    bool BirimFiyat, bool Tutar, bool Gelir, bool Gider, bool Karlilik)
{
    public static AlanErisimYetkileri Yok { get; } = new(false, false, false, false, false, false, false, false, false);
}
