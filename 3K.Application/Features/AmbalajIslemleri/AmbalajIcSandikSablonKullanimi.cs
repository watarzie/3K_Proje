using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri;

/// <summary>
/// Kayıtlı iç sandık tipinin kullanımı: bağlı ambalaj kaydı (iptal edilmiş dahil; FK silmeyi engeller), finans işi ve
/// finans tarife eşleşmesi. Kullanılan tip silinmez, pasife alınır; finans bağları FK'siz olduğu için silme onları
/// yetim bırakırdı.
/// </summary>
internal static class AmbalajIcSandikSablonKullanimi
{
    public const string KullanimdaMesaji = "Bu iç sandık tipi kullanımda; silinemez. Pasife alabilirsiniz.";

    public const string KatlanirGecisiMesaji =
        "Kullanımdaki iç sandık tipinin sandık tipi İç Sandık Katlanır'a çevrilemez veya ondan değiştirilemez.";

    /// <summary>Tip kimliğine göre toplam kullanım; <paramref name="sablonId"/> verilirse yalnız o tip sayılır.</summary>
    public static Dictionary<int, int> Sayilar(IUnitOfWork unitOfWork, int? sablonId = null)
    {
        var ambalaj = unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
            .Where(x => x.IcSandikSablonId.HasValue && (sablonId == null || x.IcSandikSablonId == sablonId))
            .GroupBy(x => x.IcSandikSablonId!.Value)
            .Select(g => new { Id = g.Key, Sayi = g.Count() })
            .ToList();
        var finans = unitOfWork.GetRepository<FinansIsKaydi>().Queryable()
            .Where(x => x.IcSandikSablonId.HasValue && (sablonId == null || x.IcSandikSablonId == sablonId))
            .GroupBy(x => x.IcSandikSablonId!.Value)
            .Select(g => new { Id = g.Key, Sayi = g.Count() })
            .ToList();
        var eslesme = unitOfWork.GetRepository<FinansUrunEslesmesi>().Queryable()
            .Where(x => x.IcSandikSablonId.HasValue && (sablonId == null || x.IcSandikSablonId == sablonId))
            .GroupBy(x => x.IcSandikSablonId!.Value)
            .Select(g => new { Id = g.Key, Sayi = g.Count() })
            .ToList();
        return ambalaj.Concat(finans).Concat(eslesme)
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Sayi));
    }

    public static int Sayi(IUnitOfWork unitOfWork, int sablonId) =>
        Sayilar(unitOfWork, sablonId).GetValueOrDefault(sablonId);

    public static AmbalajIcSandikSablonDto Dto(AmbalajIcSandikSablonu sablon, int kullanimSayisi) =>
        new(sablon.Id, sablon.Ad,
            AmbalajPlanlamaYardimcisi.SandikTipiMetni(sablon.SandikCinsi, sablon.DigerSandikCinsi),
            sablon.Boy, sablon.En, sablon.Yukseklik, sablon.Aktif, kullanimSayisi);
}
