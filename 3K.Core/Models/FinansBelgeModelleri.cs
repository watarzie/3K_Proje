namespace _3K.Core.Models;

public sealed record FinansBelgeYukleModel(string HedefTuru, int HedefId, string OrijinalAd, byte[] Icerik);

public sealed record FinansBelgeModel(int Id, string HedefTuru, int HedefId, int Surum, string OrijinalAd, long Boyut,
    string IcerikTuru, string Hash, string Yukleyen, DateTime YuklemeTarihi);

public sealed record FinansKaliciSilModel(string VarlikTuru, int Id, string Surum, bool IkinciOnay, string Aciklama);

public sealed record FinansBagimlilikModel(string VarlikTuru, int Id, string Referans);

public sealed record FinansKaliciSilOnizlemeModel(string VarlikTuru, int Id, string Referans, string Surum, bool Silinebilir,
    IReadOnlyList<FinansBagimlilikModel> Bagimliliklar, IReadOnlyList<string> Engeller);
