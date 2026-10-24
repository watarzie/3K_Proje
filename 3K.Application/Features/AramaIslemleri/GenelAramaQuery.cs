using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AramaIslemleri
{
    /// <summary>
    /// Üst çubuktaki genel arama: proje no/müşteri, barkod ve sandık no ile gruplu sonuç. Her grup kendi izniyle süzülür:
    /// projeler ve sandıklar projenin gerçek tipinin okuma izniyle (<see cref="RequestMenuPermissionResolver.ProjectPolicy"/>
    /// ProjectRead / CrateRead), ürünler <see cref="YetkiKodlari.Sandik.BarkodUrunArama"/> R ile. İsteğin kendisi bu
    /// izinlerden herhangi birini ister; hiçbirini sağlamayan kullanıcı 403 alır. Durum değiştirmez.
    /// </summary>
    public sealed class GenelAramaQuery : IRequest<Result<GenelAramaSonucuDto>>, ISecuredRequest, IRequiresMenuPermissions
    {
        public string? Q { get; set; }
        /// <summary>Grup başına en fazla sonuç: açılır panel için az, tam sonuç sayfası için çok.</summary>
        public int Limit { get; set; } = GenelAramaKurali.PanelLimiti;

        public IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => GenelAramaKurali.HerhangiIzin;
        public MenuPermissionMatch PermissionMatch => MenuPermissionMatch.Any;
    }

    public static class GenelAramaKurali
    {
        public const int EnAzUzunluk = 2;
        public const int EnFazlaUzunluk = 100;
        public const int PanelLimiti = 6;
        public const int EnFazlaLimit = 50;

        internal static readonly ProjeTipi[] Tipler = [ProjeTipi.Normal, ProjeTipi.Saha, ProjeTipi.Yedek];

        /// <summary>Gruplardan en az birini açan bütün okuma izinleri (isteğin kendi yetki tanımı).</summary>
        public static IReadOnlyCollection<MenuPermissionRequirement> HerhangiIzin { get; } = Tipler
            .SelectMany(t => new[]
            {
                RequestMenuPermissionResolver.ProjectPolicy((int)t, ProjectMenuOperation.ProjectRead, YetkiTipi.R),
                RequestMenuPermissionResolver.ProjectPolicy((int)t, ProjectMenuOperation.CrateRead, YetkiTipi.R)
            })
            .SelectMany(p => p.Groups.SelectMany(g => g.Requirements))
            .Append(MenuPermissionRequirement.Read(YetkiKodlari.Sandik.BarkodUrunArama))
            .Distinct()
            .ToArray();

        /// <summary>Kırpılmış, büyük harfe çevrilmiş arama metni; boşsa null.</summary>
        public static string? Normalize(string? q) => string.IsNullOrWhiteSpace(q) ? null : q.Trim().ToUpperInvariant();

        public static bool Gecerli(string? q) => Normalize(q) is { Length: >= EnAzUzunluk and <= EnFazlaUzunluk };
    }

    public sealed class GenelAramaQueryHandler(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor readQueries,
        IRolService rolService,
        ICurrentUserService currentUser)
        : IRequestHandler<GenelAramaQuery, Result<GenelAramaSonucuDto>>
    {
        public async Task<Result<GenelAramaSonucuDto>> Handle(GenelAramaQuery request, CancellationToken cancellationToken)
        {
            if (currentUser.UserId is not int kullaniciId)
                return Result<GenelAramaSonucuDto>.Failure("Kullanıcı bilgisi alınamadı.", 401);
            var q = GenelAramaKurali.Normalize(request.Q)!;
            var limit = Math.Clamp(request.Limit, 1, GenelAramaKurali.EnFazlaLimit);
            var izin = new MenuIzinDegerlendirici(rolService, kullaniciId);

            var projeTipleri = new List<int>();
            var sandikTipleri = new List<int>();
            foreach (var tip in GenelAramaKurali.Tipler.Select(t => (int)t))
            {
                if (await izin.SaglarMiAsync(RequestMenuPermissionResolver.ProjectPolicy(tip, ProjectMenuOperation.ProjectRead, YetkiTipi.R), cancellationToken))
                    projeTipleri.Add(tip);
                if (await izin.SaglarMiAsync(RequestMenuPermissionResolver.ProjectPolicy(tip, ProjectMenuOperation.CrateRead, YetkiTipi.R), cancellationToken))
                    sandikTipleri.Add(tip);
            }
            var barkodIzni = await izin.SaglarMiAsync(
                new RequestMenuPermissions([new([MenuPermissionRequirement.Read(YetkiKodlari.Sandik.BarkodUrunArama)])]), cancellationToken);

            return Result<GenelAramaSonucuDto>.Success(new GenelAramaSonucuDto
            {
                Q = request.Q!.Trim(),
                ProjeAranabilir = projeTipleri.Count > 0,
                SandikAranabilir = sandikTipleri.Count > 0,
                UrunAranabilir = barkodIzni,
                Projeler = projeTipleri.Count == 0 ? [] : await ProjelerAsync(q, projeTipleri, limit, cancellationToken),
                Sandiklar = sandikTipleri.Count == 0 ? [] : await SandiklarAsync(q, sandikTipleri, limit, cancellationToken),
                Urunler = barkodIzni ? await UrunlerAsync(q, limit, cancellationToken) : []
            });
        }

        /// <summary>Proje no veya müşteri adı içinde geçen; tam eşleşen proje no önce, sonra en yeni proje.</summary>
        private Task<List<GenelAramaProjeDto>> ProjelerAsync(string q, List<int> tipler, int limit, CancellationToken cancellationToken) =>
            readQueries.ToListAsync(unitOfWork.GetRepository<Proje>().Queryable()
                .Where(p => tipler.Contains(p.ProjeTipiId))
                .Where(p => p.ProjeNo.ToUpper().Contains(q) || p.Musteri.ToUpper().Contains(q))
                .OrderByDescending(p => p.ProjeNo.ToUpper() == q)
                .ThenByDescending(p => p.ProjeNo.ToUpper().StartsWith(q))
                .ThenByDescending(p => p.CreatedDate)
                .Take(limit)
                .Select(p => new GenelAramaProjeDto
                {
                    Id = p.Id,
                    ProjeNo = p.ProjeNo,
                    Musteri = p.Musteri,
                    ProjeTipiId = p.ProjeTipiId,
                    DurumMetni = p.DurumLookup != null ? p.DurumLookup.Deger : null,
                    SandikSayisi = p.Sandiklar.Count
                }), cancellationToken);

        /// <summary>Sandık no tam eşleşen (büyük/küçük harf önemsiz) sandıklar; en yeni proje önce.</summary>
        private Task<List<GenelAramaSandikDto>> SandiklarAsync(string q, List<int> tipler, int limit, CancellationToken cancellationToken) =>
            readQueries.ToListAsync(unitOfWork.GetRepository<Sandik>().Queryable()
                .Where(s => tipler.Contains(s.Proje.ProjeTipiId))
                .Where(s => s.SandikNo.Trim().ToUpper() == q)
                .OrderByDescending(s => s.Proje.CreatedDate)
                .ThenBy(s => s.Id)
                .Take(limit)
                .Select(s => new GenelAramaSandikDto
                {
                    Id = s.Id,
                    SandikNo = s.SandikNo,
                    Ad = s.Ad,
                    ProjeId = s.ProjeId,
                    ProjeNo = s.Proje.ProjeNo,
                    ProjeTipiId = s.Proje.ProjeTipiId,
                    DurumMetni = s.DurumLookup != null ? s.DurumLookup.Deger : null
                }), cancellationToken);

        /// <summary>
        /// Barkodu aranan metinle başlayan ürünler (barkod başına tek satır, kaç projede geçtiği); tam eşleşen önce. Seçilen
        /// barkod, barkod arama ekranında tam eşleşmeyle açılır.
        /// </summary>
        private async Task<List<GenelAramaUrunDto>> UrunlerAsync(string q, int limit, CancellationToken cancellationToken)
        {
            var ceki = await readQueries.ToListAsync(unitOfWork.GetRepository<CekiSatiri>().Queryable()
                .Where(cs => cs.BarkodNo.Trim().ToUpper().StartsWith(q))
                .GroupBy(cs => cs.BarkodNo.Trim().ToUpper())
                .Select(g => new GenelAramaUrunDto
                {
                    Barkod = g.Key,
                    Aciklama = g.Max(cs => cs.Aciklama),
                    ProjeSayisi = g.Select(cs => cs.Ceki.ProjeId).Distinct().Count()
                })
                .OrderByDescending(x => x.Barkod == q)
                .ThenBy(x => x.Barkod)
                .Take(limit), cancellationToken);
            var elle = await readQueries.ToListAsync(unitOfWork.GetRepository<SandikIcerik>().Queryable()
                .Where(si => si.CekiSatiriId == null && si.BarkodNo != null && si.BarkodNo.Trim().ToUpper().StartsWith(q))
                .GroupBy(si => si.BarkodNo!.Trim().ToUpper())
                .Select(g => new GenelAramaUrunDto
                {
                    Barkod = g.Key,
                    Aciklama = g.Max(si => si.Isim),
                    ProjeSayisi = g.Select(si => si.Sandik.ProjeId).Distinct().Count()
                })
                .OrderBy(x => x.Barkod)
                .Take(limit), cancellationToken);

            // Aynı barkod hem çeki satırında hem elle eklenmişse tek satır; proje sayısı üst sınırdır (aynı proje iki kez sayılabilir).
            return ceki.Concat(elle)
                .GroupBy(x => x.Barkod)
                .Select(g => new GenelAramaUrunDto
                {
                    Barkod = g.Key,
                    Aciklama = g.Select(x => x.Aciklama).FirstOrDefault(a => !string.IsNullOrWhiteSpace(a)),
                    ProjeSayisi = g.Sum(x => x.ProjeSayisi)
                })
                .OrderByDescending(x => x.Barkod == q)
                .ThenBy(x => x.Barkod, StringComparer.Ordinal)
                .Take(limit)
                .ToList();
        }
    }

    public sealed class GenelAramaSonucuDto
    {
        public string Q { get; init; } = string.Empty;
        /// <summary>Kullanıcının bu grubu görme izni var mı (yoksa grup ekranda hiç gösterilmez).</summary>
        public bool ProjeAranabilir { get; init; }
        public bool SandikAranabilir { get; init; }
        public bool UrunAranabilir { get; init; }
        public List<GenelAramaProjeDto> Projeler { get; init; } = [];
        public List<GenelAramaUrunDto> Urunler { get; init; } = [];
        public List<GenelAramaSandikDto> Sandiklar { get; init; } = [];
    }

    public sealed class GenelAramaProjeDto
    {
        public int Id { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public string Musteri { get; init; } = string.Empty;
        public int ProjeTipiId { get; init; }
        public string? DurumMetni { get; init; }
        public int SandikSayisi { get; init; }
    }

    public sealed class GenelAramaUrunDto
    {
        public string Barkod { get; init; } = string.Empty;
        public string? Aciklama { get; init; }
        public int ProjeSayisi { get; init; }
    }

    public sealed class GenelAramaSandikDto
    {
        public int Id { get; init; }
        public string SandikNo { get; init; } = string.Empty;
        public string? Ad { get; init; }
        public int ProjeId { get; init; }
        public string ProjeNo { get; init; } = string.Empty;
        public int ProjeTipiId { get; init; }
        public string? DurumMetni { get; init; }
    }
}
