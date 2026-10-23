using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    /// <summary>
    /// Grid'e gelmiş ürünleri veritabanında süzer, sıralar ve sayfalar (Grid'de bekleyen miktar
    /// <see cref="GridSorgulari.GriddeBekleyenIfadesi"/> ile SQL'de hesaplanır). Sevk kilidi dışlaması Grid iş
    /// listesiyle ortaktır; aktif saha aktarımlı normal kök satır düşmez, rozetle işaretlenir. Son mal kabulün tarihi ve
    /// personeli yalnız sayfadaki satırlar için hareket geçmişinden okunur (Grid'e geliş tarihi saklanmaz). Özet,
    /// arama ve süzgeçlerden bağımsızdır. Durum değiştirmez.
    /// </summary>
    public sealed class GetGridSahaUrunleriQueryHandler(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor queryExecutor,
        ILookupCacheService lookupCache,
        ISahaTamamlamaService sahaTamamlamaService,
        IGridHareketOkuyucu hareketOkuyucu)
        : IRequestHandler<GetGridSahaUrunleriQuery, Result<GridSahaUrunleriDto>>
    {
        public async Task<Result<GridSahaUrunleriDto>> Handle(GetGridSahaUrunleriQuery request, CancellationToken cancellationToken)
        {
            var sayfa = Math.Max(request.PageNumber, 1);
            var sayfaBoyutu = Math.Clamp(request.PageSize, 1, 100);
            var bugun = TurkeyTime.Now.Date;

            var gelmis = queryExecutor.AsNoTracking(
                GridSorgulari.SevkKilidiDisinda(unitOfWork.GetRepository<CekiSatiri>().Queryable())
                    .Where(GridSorgulari.GridGelmisDurumIfadesi));
            var bekleyenler = gelmis.Where(GridSorgulari.BekleyenPozitifIfadesi);

            var ozetSatirlari = await queryExecutor.ToListAsync(
                bekleyenler.Select(GridSorgulari.BekleyenIle((cs, bekleyen) => new OzetSatiri
                {
                    ProjeId = cs.Ceki.ProjeId,
                    GridDurumuId = cs.GridDurumuId,
                    Bekleyen = bekleyen
                })),
                cancellationToken);
            var bugunMalKabul = await hareketOkuyucu.MalKabulSayisiAsync(bugun, bugun.AddDays(1), cancellationToken);

            var filtreli = Suz(GridSahaKapsami.TumuMu(request.Kapsam) ? gelmis : bekleyenler, request);
            var toplam = await queryExecutor.CountAsync(filtreli, cancellationToken);
            var satirlar = await queryExecutor.ToListAsync(
                Sirala(filtreli, request)
                    .Skip((sayfa - 1) * sayfaBoyutu)
                    .Take(sayfaBoyutu)
                    .Select(GridSorgulari.BekleyenIle((cs, bekleyen) => new UrunSatiri
                    {
                        Id = cs.Id,
                        ProjeId = cs.Ceki.ProjeId,
                        ProjeNo = cs.Ceki.Proje.ProjeNo,
                        ProjeTipiId = cs.Ceki.Proje.ProjeTipiId,
                        Musteri = cs.Ceki.Proje.Musteri,
                        FiiliSandikNo = cs.FiiliSandikNo,
                        CekideGecenSandikNo = cs.CekideGecenSandikNo,
                        SiraNo = cs.SiraNo,
                        BarkodNo = cs.BarkodNo,
                        OlcuResmiPozNo = cs.OlcuResmiPozNo,
                        Aciklama = cs.Aciklama,
                        BirimId = cs.BirimId,
                        IstenenAdet = cs.IstenenAdet,
                        GridGelenAdet = cs.GridGelenAdet,
                        GelenMiktar = cs.GelenMiktar,
                        GridDurumuId = cs.GridDurumuId,
                        GridSevkDurumuId = cs.GridSevkDurumuId,
                        GridSevkMiktari = cs.GridSevkMiktari,
                        AktifGridSevkKarsilananMiktari = cs.AktifGridSevkKarsilananMiktari,
                        AktifGridSevkPartisiErkenSonuclandirildiMi = cs.AktifGridSevkPartisiErkenSonuclandirildiMi,
                        UcKDurumuId = cs.UcKDurumuId,
                        KaynakCekiSatiriId = cs.KaynakCekiSatiriId,
                        GridAciklama = cs.GridAciklama,
                        Bekleyen = bekleyen
                    })),
                cancellationToken);

            var sahayaAktarilanlar = await SahayaAktarilanlarAsync(satirlar, cancellationToken);
            var sonMalKabuller = await hareketOkuyucu.SonMalKabulleriAsync(satirlar.Select(x => x.Id).ToList(), cancellationToken);

            return Result<GridSahaUrunleriDto>.Success(new GridSahaUrunleriDto
            {
                Ozet = new GridSahaOzetDto
                {
                    BekleyenSatir = ozetSatirlari.Count,
                    BekleyenAdet = ozetSatirlari.Sum(x => x.Bekleyen),
                    ProjeSayisi = ozetSatirlari.Select(x => x.ProjeId).Distinct().Count(),
                    EksikGelenSatir = ozetSatirlari.Count(x => x.GridDurumuId == (int)GridDurum.EksikGeldi),
                    BugunMalKabul = bugunMalKabul
                },
                Liste = new PaginatedList<GridSahaUrunDto>(
                    satirlar.Select(x => Esle(x, sahayaAktarilanlar.Contains(x.Id), sonMalKabuller.GetValueOrDefault(x.Id))).ToList(),
                    toplam,
                    sayfa,
                    sayfaBoyutu)
            });
        }

        private static IQueryable<CekiSatiri> Suz(IQueryable<CekiSatiri> sorgu, GetGridSahaUrunleriQuery request)
        {
            if (request.ProjeTipiId is > 0)
            {
                var tip = request.ProjeTipiId.Value;
                sorgu = sorgu.Where(cs => cs.Ceki.Proje.ProjeTipiId == tip);
            }
            if (request.ProjeId is > 0)
            {
                var projeId = request.ProjeId.Value;
                sorgu = sorgu.Where(cs => cs.Ceki.ProjeId == projeId);
            }
            if (request.GridDurumuId is > 0)
            {
                var durum = request.GridDurumuId.Value;
                sorgu = sorgu.Where(cs => cs.GridDurumuId == durum);
            }
            if (!string.IsNullOrWhiteSpace(request.Arama))
            {
                var terim = request.Arama.Trim().ToLower();
                sorgu = sorgu.Where(cs =>
                    cs.Ceki.Proje.ProjeNo.ToLower().Contains(terim) ||
                    cs.Ceki.Proje.Musteri.ToLower().Contains(terim) ||
                    cs.BarkodNo.ToLower().Contains(terim) ||
                    cs.Aciklama.ToLower().Contains(terim) ||
                    (cs.OlcuResmiPozNo != null && cs.OlcuResmiPozNo.ToLower().Contains(terim)) ||
                    (string.IsNullOrWhiteSpace(cs.FiiliSandikNo) ? cs.CekideGecenSandikNo : cs.FiiliSandikNo).ToLower().Contains(terim));
            }
            return sorgu;
        }

        private static IOrderedQueryable<CekiSatiri> Sirala(IQueryable<CekiSatiri> sorgu, GetGridSahaUrunleriQuery request)
        {
            var azalan = request.Azalan;
            IOrderedQueryable<CekiSatiri> sirali = GridSahaSiralama.Normalize(request.Siralama) switch
            {
                GridSahaSiralama.Bekleyen => azalan
                    ? sorgu.OrderByDescending(GridSorgulari.GriddeBekleyenIfadesi)
                    : sorgu.OrderBy(GridSorgulari.GriddeBekleyenIfadesi),
                GridSahaSiralama.Sandik => azalan
                    ? sorgu.OrderByDescending(cs => string.IsNullOrWhiteSpace(cs.FiiliSandikNo) ? cs.CekideGecenSandikNo : cs.FiiliSandikNo)
                    : sorgu.OrderBy(cs => string.IsNullOrWhiteSpace(cs.FiiliSandikNo) ? cs.CekideGecenSandikNo : cs.FiiliSandikNo),
                _ => azalan
                    ? sorgu.OrderByDescending(cs => cs.Ceki.Proje.ProjeNo)
                    : sorgu.OrderBy(cs => cs.Ceki.Proje.ProjeNo)
            };
            return sirali
                .ThenBy(cs => cs.Ceki.Proje.ProjeNo)
                .ThenBy(cs => string.IsNullOrWhiteSpace(cs.FiiliSandikNo) ? cs.CekideGecenSandikNo : cs.FiiliSandikNo)
                .ThenBy(cs => cs.SiraNo)
                .ThenBy(cs => cs.Id);
        }

        private async Task<HashSet<int>> SahayaAktarilanlarAsync(List<UrunSatiri> satirlar, CancellationToken cancellationToken)
        {
            var kokSatirlar = satirlar
                .Where(x => x.ProjeTipiId == (int)ProjeTipi.Normal && !x.KaynakCekiSatiriId.HasValue)
                .Select(x => x.Id)
                .ToList();
            if (kokSatirlar.Count == 0)
                return [];
            var map = await sahaTamamlamaService.GetAktifTamamlamaMapAsync(kokSatirlar, cancellationToken);
            return map.Keys.ToHashSet();
        }

        private GridSahaUrunDto Esle(UrunSatiri x, bool sahayaAktarildi, GridSonMalKabul? sonMalKabul)
        {
            // Yoldaki miktar saf kuralla hesaplanır; bekleyen miktar SQL ikizinden gelir (eşdeğerlik testle sabit).
            var yolda = GridSahaBekleyenKurali.YoldakiMiktar(new CekiSatiri
            {
                GridGelenAdet = x.GridGelenAdet,
                GelenMiktar = x.GelenMiktar,
                GridSevkDurumuId = x.GridSevkDurumuId,
                GridSevkMiktari = x.GridSevkMiktari,
                AktifGridSevkKarsilananMiktari = x.AktifGridSevkKarsilananMiktari,
                AktifGridSevkPartisiErkenSonuclandirildiMi = x.AktifGridSevkPartisiErkenSonuclandirildiMi,
                UcKDurumuId = x.UcKDurumuId
            });
            return new GridSahaUrunDto
            {
                CekiSatiriId = x.Id,
                ProjeId = x.ProjeId,
                ProjeNo = x.ProjeNo,
                ProjeTipiId = x.ProjeTipiId,
                Musteri = x.Musteri,
                SandikNo = string.IsNullOrWhiteSpace(x.FiiliSandikNo) ? x.CekideGecenSandikNo : x.FiiliSandikNo!,
                SiraNo = x.SiraNo,
                BarkodNo = x.BarkodNo,
                OlcuResmiPozNo = x.OlcuResmiPozNo,
                Aciklama = x.Aciklama,
                Birim = lookupCache.GetDeger<LookupBirim>(x.BirimId),
                IstenenAdet = x.IstenenAdet,
                GridGelenAdet = x.GridGelenAdet,
                UcKGelenMiktar = x.GelenMiktar,
                YoldakiMiktar = yolda,
                GriddeBekleyenMiktar = x.Bekleyen,
                GridDurumuId = x.GridDurumuId,
                GridDurumuMetni = lookupCache.GetDeger<LookupGridDurum>(x.GridDurumuId),
                GridSevkDurumuId = x.GridSevkDurumuId,
                GridSevkDurumuMetni = lookupCache.GetDeger<LookupGridSevkDurum>(x.GridSevkDurumuId),
                SonMalKabulTarihi = sonMalKabul?.Tarih,
                SonMalKabulPersoneli = sonMalKabul?.KullaniciAdi,
                SahayaAktarildiMi = sahayaAktarildi,
                GridAciklama = x.GridAciklama
            };
        }

        private sealed class OzetSatiri
        {
            public int ProjeId { get; init; }
            public int GridDurumuId { get; init; }
            public decimal Bekleyen { get; init; }
        }

        private sealed class UrunSatiri
        {
            public int Id { get; init; }
            public int ProjeId { get; init; }
            public string ProjeNo { get; init; } = string.Empty;
            public int ProjeTipiId { get; init; }
            public string Musteri { get; init; } = string.Empty;
            public string? FiiliSandikNo { get; init; }
            public string CekideGecenSandikNo { get; init; } = string.Empty;
            public int SiraNo { get; init; }
            public string BarkodNo { get; init; } = string.Empty;
            public string? OlcuResmiPozNo { get; init; }
            public string Aciklama { get; init; } = string.Empty;
            public int BirimId { get; init; }
            public decimal IstenenAdet { get; init; }
            public decimal GridGelenAdet { get; init; }
            public decimal GelenMiktar { get; init; }
            public int GridDurumuId { get; init; }
            public int GridSevkDurumuId { get; init; }
            public decimal? GridSevkMiktari { get; init; }
            public decimal? AktifGridSevkKarsilananMiktari { get; init; }
            public bool? AktifGridSevkPartisiErkenSonuclandirildiMi { get; init; }
            public int UcKDurumuId { get; init; }
            public int? KaynakCekiSatiriId { get; init; }
            public string? GridAciklama { get; init; }
            public decimal Bekleyen { get; init; }
        }
    }
}
