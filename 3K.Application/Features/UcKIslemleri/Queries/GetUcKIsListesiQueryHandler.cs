using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Queries
{
    public class GetUcKIsListesiQueryHandler : IRequestHandler<GetUcKIsListesiQuery, Result<UcKIsListesiDto>>
    {
        private const string TipTeslim = "teslim";
        private const string TipYeniden = "yeniden";
        private const string TipEksik = "eksik";
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public GetUcKIsListesiQueryHandler(IUnitOfWork unitOfWork, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
        }

        public Task<Result<UcKIsListesiDto>> Handle(GetUcKIsListesiQuery request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var isTipi = NormalizeTip(request.IsTipi);

            var query = _unitOfWork.GetRepository<CekiSatiri>()
                .Queryable()
                .Where(cs => cs.Ceki.Proje.DurumId != (int)ProjeDurum.SevkEdildi)
                .Where(cs => !cs.SandikIcerikleri.Any() ||
                    cs.SandikIcerikleri.Any(si => si.Sandik.DurumId != (int)SandikDurum.Sevkedildi));

            if (request.ProjeId.HasValue)
            {
                query = query.Where(cs => cs.Ceki.ProjeId == request.ProjeId.Value);
            }

            var rows = query
                .Select(cs => new IsListesiRow
                {
                    Id = cs.Id,
                    SiraNo = cs.SiraNo,
                    BarkodNo = cs.BarkodNo,
                    OlcuResmiPozNo = cs.OlcuResmiPozNo,
                    Aciklama = cs.Aciklama,
                    CekideGecenSandikNo = cs.CekideGecenSandikNo,
                    FiiliSandikNo = cs.FiiliSandikNo,
                    IstenenAdet = cs.IstenenAdet,
                    BirimId = cs.BirimId,
                    GridDurumuId = cs.GridDurumuId,
                    GridGelenAdet = cs.GridGelenAdet,
                    TrafoSevkAdet = cs.TrafoSevkAdet,
                    GridSevkDurumuId = cs.GridSevkDurumuId,
                    GridSevkMiktari = cs.GridSevkMiktari,
                    YenidenSevkGerekliAdet = cs.YenidenSevkGerekliAdet,
                    GridAciklama = cs.GridAciklama,
                    GridSevkTarihi = cs.GridSevkTarihi,
                    UcKDurumuId = cs.UcKDurumuId,
                    UcKKarsilamaTipiId = cs.UcKKarsilamaTipiId,
                    GelenMiktar = cs.GelenMiktar,
                    StokKarsilanan = cs.StokKarsilanan,
                    ProjeKarsilanan = cs.ProjeKarsilanan,
                    ProjeGonderilen = cs.ProjeGonderilen,
                    TedarikciKarsilanan = cs.TedarikciKarsilanan,
                    UpdatedDate = cs.UpdatedDate,
                    CreatedDate = cs.CreatedDate,
                    ProjeId = cs.Ceki.ProjeId,
                    ProjeNo = cs.Ceki.Proje.ProjeNo,
                    Musteri = cs.Ceki.Proje.Musteri
                })
                .ToList();

            var items = rows
                .Select(MapItem)
                .Where(item => item != null)
                .Select(item => item!)
                .ToList();

            var dto = new UcKIsListesiDto
            {
                Toplam = items.Count,
                TeslimBekleyen = items.Count(i => i.IsTipi == TipTeslim),
                EksikGelen = items.Count(i => i.IsTipi == TipEksik),
                TrafoSevk = 0,
                YenidenSevkGerekli = items.Count(i => i.IsTipi == TipYeniden),
                GridKapandi = 0,
                BugunGridIslemi = items.Count(IsTodayGridOperation)
            };

            if (request.SadeceBugun)
            {
                items = items.Where(IsTodayGridOperation).ToList();
            }

            if (!string.IsNullOrWhiteSpace(isTipi))
            {
                items = items.Where(i => i.IsTipi == isTipi).ToList();
            }

            var orderedProjectGroups = items
                .GroupBy(i => i.ProjeId)
                .Select(group =>
                {
                    var orderedGroupItems = group
                        .OrderByDescending(GetOperationDate)
                        .ThenBy(i => i.Oncelik)
                        .ThenBy(i => GetSandikSira(i.SandikNo))
                        .ThenBy(i => i.SiraNo)
                        .ToList();

                    return new
                    {
                        ProjeId = group.Key,
                        ProjeNo = orderedGroupItems.First().ProjeNo,
                        LatestOperationDate = orderedGroupItems.Max(GetOperationDate),
                        Priority = orderedGroupItems.Min(i => i.Oncelik),
                        Items = orderedGroupItems
                    };
                })
                .OrderByDescending(group => group.LatestOperationDate)
                .ThenBy(group => group.Priority)
                .ThenBy(group => group.ProjeNo)
                .ToList();

            var pagedItems = orderedProjectGroups
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .SelectMany(group => group.Items)
                .ToList();

            dto.Liste = new UcKPagedResultDto<UcKIsListesiItemDto>
            {
                Items = pagedItems,
                TotalCount = orderedProjectGroups.Count,
                Page = page,
                PageSize = pageSize,
                HasMore = page * pageSize < orderedProjectGroups.Count
            };

            return Task.FromResult(Result<UcKIsListesiDto>.Success(dto));
        }

        private UcKIsListesiItemDto? MapItem(IsListesiRow row)
        {
            var gridSevkMiktari = row.GridSevkMiktari ?? 0;
            var teslimBekleyen = Math.Max(gridSevkMiktari - row.GelenMiktar, 0);
            var gridEksik = CalculateGridEksik(row.GridDurumuId, row.IstenenAdet, row.GridGelenAdet, row.TrafoSevkAdet);
            var kalan = CalculateKalan(row.GridDurumuId, row.IstenenAdet, row.GelenMiktar, row.StokKarsilanan, row.ProjeKarsilanan, row.ProjeGonderilen, row.TedarikciKarsilanan, row.TrafoSevkAdet);

            var tip = GetIsTipi(row.GridDurumuId, row.GridSevkDurumuId, row.YenidenSevkGerekliAdet, row.TrafoSevkAdet, teslimBekleyen, gridEksik, row.UcKKarsilamaTipiId);
            if (tip == null)
            {
                return null;
            }

            var sandikNo = string.IsNullOrWhiteSpace(row.FiiliSandikNo) ? row.CekideGecenSandikNo : row.FiiliSandikNo;

            return new UcKIsListesiItemDto
            {
                CekiSatiriId = row.Id,
                ProjeId = row.ProjeId,
                ProjeNo = row.ProjeNo,
                Musteri = row.Musteri,
                SandikNo = sandikNo,
                SiraNo = row.SiraNo,
                BarkodNo = row.BarkodNo,
                OlcuResmiPozNo = row.OlcuResmiPozNo,
                Aciklama = row.Aciklama,
                Birim = ((Birim)row.BirimId).ToString(),
                IstenenAdet = row.IstenenAdet,
                GridGelenAdet = row.GridGelenAdet,
                GridSevkMiktari = gridSevkMiktari,
                TrafoSevkAdet = row.TrafoSevkAdet,
                YenidenSevkGerekliAdet = row.YenidenSevkGerekliAdet,
                UcKGelenMiktar = row.GelenMiktar,
                KalanMiktar = kalan,
                GridDurumuId = row.GridDurumuId,
                GridDurumuMetni = _lookupCache.GetDeger<LookupGridDurum>(row.GridDurumuId),
                GridSevkDurumuId = row.GridSevkDurumuId,
                GridSevkDurumuMetni = _lookupCache.GetDeger<LookupGridSevkDurum>(row.GridSevkDurumuId),
                UcKDurumuId = row.UcKDurumuId,
                UcKDurumuMetni = _lookupCache.GetDeger<LookupUcKDurum>(row.UcKDurumuId),
                GridAciklama = row.GridAciklama,
                GridSevkTarihi = row.GridSevkTarihi,
                SonIslemTarihi = row.UpdatedDate ?? row.GridSevkTarihi ?? row.CreatedDate,
                IsTipi = tip.Value.IsTipi,
                IsTipiMetni = tip.Value.IsTipiMetni,
                Oncelik = tip.Value.Oncelik
            };
        }

        private static (string IsTipi, string IsTipiMetni, int Oncelik)? GetIsTipi(
            int gridDurumuId,
            int gridSevkDurumuId,
            decimal yenidenSevkGerekliAdet,
            decimal trafoSevkAdet,
            decimal teslimBekleyen,
            decimal gridEksik,
            int ucKKarsilamaTipiId)
        {
            if (yenidenSevkGerekliAdet > 0 || gridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli)
                return (TipYeniden, "Yeniden sevk gerekli", 1);

            if (teslimBekleyen > 0 && gridSevkDurumuId == (int)GridSevkDurum.SevkEdildi)
                return (TipTeslim, "3K teslim bekliyor", 2);

            if (gridDurumuId == (int)GridDurum.EksikGeldi &&
                gridEksik > 0 &&
                gridSevkDurumuId == (int)GridSevkDurum.SevkEdildi)
                return (TipEksik, "Grid eksik geldi", 3);

            return null;
        }

        private static decimal CalculateGridEksik(int gridDurumuId, decimal istenenAdet, decimal gridGelenAdet, decimal trafoSevkAdet)
        {
            if (gridDurumuId == (int)GridDurum.Iptal || gridDurumuId == (int)GridDurum.GridKapandi)
                return 0;

            return Math.Max(istenenAdet - gridGelenAdet - trafoSevkAdet, 0);
        }

        private static decimal CalculateKalan(
            int gridDurumuId,
            decimal istenenAdet,
            decimal gelenMiktar,
            decimal stokKarsilanan,
            decimal projeKarsilanan,
            decimal projeGonderilen,
            decimal tedarikciKarsilanan,
            decimal trafoSevkAdet)
        {
            if (gridDurumuId == (int)GridDurum.GridKapandi || gridDurumuId == (int)GridDurum.Iptal)
                return 0;

            return Math.Max(istenenAdet - gelenMiktar - stokKarsilanan - projeKarsilanan - tedarikciKarsilanan + projeGonderilen - trafoSevkAdet, 0);
        }

        private static string? NormalizeTip(string? isTipi)
        {
            if (string.IsNullOrWhiteSpace(isTipi) || isTipi.Equals("all", StringComparison.OrdinalIgnoreCase))
                return null;

            var normalized = isTipi.Trim().ToLowerInvariant();
            return normalized is TipTeslim or TipYeniden or TipEksik
                ? normalized
                : null;
        }

        private static bool IsTodayGridOperation(UcKIsListesiItemDto item)
        {
            var operationDate = item.SonIslemTarihi ?? item.GridSevkTarihi;
            return operationDate?.Date == TurkeyTime.Now.Date;
        }

        private static DateTime GetOperationDate(UcKIsListesiItemDto item)
        {
            return item.SonIslemTarihi ?? item.GridSevkTarihi ?? DateTime.MinValue;
        }

        private static int GetSandikSira(string? sandikNo)
        {
            if (string.IsNullOrWhiteSpace(sandikNo))
                return int.MaxValue;

            var digits = new string(sandikNo.Trim().TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var value) ? value : int.MaxValue;
        }

        private sealed class IsListesiRow
        {
            public int Id { get; set; }
            public int SiraNo { get; set; }
            public string BarkodNo { get; set; } = string.Empty;
            public string? OlcuResmiPozNo { get; set; }
            public string Aciklama { get; set; } = string.Empty;
            public string CekideGecenSandikNo { get; set; } = string.Empty;
            public string? FiiliSandikNo { get; set; }
            public decimal IstenenAdet { get; set; }
            public int BirimId { get; set; }
            public int GridDurumuId { get; set; }
            public decimal GridGelenAdet { get; set; }
            public decimal TrafoSevkAdet { get; set; }
            public int GridSevkDurumuId { get; set; }
            public decimal? GridSevkMiktari { get; set; }
            public decimal YenidenSevkGerekliAdet { get; set; }
            public string? GridAciklama { get; set; }
            public DateTime? GridSevkTarihi { get; set; }
            public int UcKDurumuId { get; set; }
            public int UcKKarsilamaTipiId { get; set; }
            public decimal GelenMiktar { get; set; }
            public decimal StokKarsilanan { get; set; }
            public decimal ProjeKarsilanan { get; set; }
            public decimal ProjeGonderilen { get; set; }
            public decimal TedarikciKarsilanan { get; set; }
            public DateTime? UpdatedDate { get; set; }
            public DateTime CreatedDate { get; set; }
            public int ProjeId { get; set; }
            public string ProjeNo { get; set; } = string.Empty;
            public string Musteri { get; set; } = string.Empty;
        }
    }
}
