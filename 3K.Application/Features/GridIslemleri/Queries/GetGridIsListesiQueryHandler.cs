using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    public class GetGridIsListesiQueryHandler
        : IRequestHandler<GetGridIsListesiQuery, Result<GridIsListesiDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public GetGridIsListesiQueryHandler(
            IUnitOfWork unitOfWork,
            ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
        }

        public Task<Result<GridIsListesiDto>> Handle(
            GetGridIsListesiQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var isTipi = NormalizeTip(request.IsTipi);

            var query = _unitOfWork.GetRepository<CekiSatiri>()
                .Queryable()
                .Where(cs =>
                    cs.Ceki.Proje.DurumId != (int)ProjeDurum.SevkEdildi ||
                    cs.SandikIcerikleri.Any(si => si.Sandik.SevkiyatDuzeltmeAcikMi) ||
                    cs.Ceki.Proje.Sandiklar.Any(s =>
                        s.SevkiyatDuzeltmeAcikMi &&
                        s.SandikNo == (string.IsNullOrWhiteSpace(cs.FiiliSandikNo)
                            ? cs.CekideGecenSandikNo
                            : cs.FiiliSandikNo)))
                .Where(cs => !cs.SandikIcerikleri.Any(si =>
                    si.Sandik.DurumId == (int)SandikDurum.Sevkedildi &&
                    !si.Sandik.SevkiyatDuzeltmeAcikMi))
                .Where(cs => !cs.Ceki.Proje.Sandiklar.Any(s =>
                    s.SandikNo == (string.IsNullOrWhiteSpace(cs.FiiliSandikNo)
                        ? cs.CekideGecenSandikNo
                        : cs.FiiliSandikNo) &&
                    s.DurumId == (int)SandikDurum.Sevkedildi &&
                    !s.SevkiyatDuzeltmeAcikMi));

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
                    GelenMiktar = cs.GelenMiktar,
                    StokKarsilanan = cs.StokKarsilanan,
                    ProjeKarsilanan = cs.ProjeKarsilanan,
                    ProjeGonderilen = cs.ProjeGonderilen,
                    TedarikciKarsilanan = cs.TedarikciKarsilanan,
                    HataliMiktar = cs.HataliMiktar,
                    DurumId = cs.DurumId,
                    UpdatedDate = cs.UpdatedDate,
                    CreatedDate = cs.CreatedDate,
                    ProjeId = cs.Ceki.ProjeId,
                    ProjeTipiId = cs.Ceki.Proje.ProjeTipiId,
                    ProjeNo = cs.Ceki.Proje.ProjeNo,
                    Musteri = cs.Ceki.Proje.Musteri
                })
                .ToList();

            var items = rows
                .Select(MapItem)
                .Where(item => item != null)
                .Select(item => item!)
                .ToList();

            var dto = new GridIsListesiDto
            {
                Toplam = items.Count,
                EksikGelen = items.Count(item => item.IsTipi == GridIsListesiSiniflandirma.TipEksik),
                YenidenSevkGerekli = items.Count(item => item.IsTipi == GridIsListesiSiniflandirma.TipYeniden),
                BugunGridIslemi = items.Count(IsTodayGridOperation)
            };

            if (request.SadeceBugun)
            {
                items = items.Where(IsTodayGridOperation).ToList();
            }

            if (!string.IsNullOrWhiteSpace(isTipi))
            {
                items = items.Where(item => item.IsTipi == isTipi).ToList();
            }

            var orderedProjectGroups = items
                .GroupBy(item => item.ProjeId)
                .Select(group =>
                {
                    var orderedGroupItems = group
                        .OrderByDescending(GetOperationDate)
                        .ThenBy(item => item.Oncelik)
                        .ThenBy(item => GetSandikSira(item.SandikNo))
                        .ThenBy(item => item.SiraNo)
                        .ToList();

                    return new
                    {
                        ProjeId = group.Key,
                        ProjeNo = orderedGroupItems.First().ProjeNo,
                        LatestOperationDate = orderedGroupItems.Max(GetOperationDate),
                        Priority = orderedGroupItems.Min(item => item.Oncelik),
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

            dto.Liste = new GridPagedResultDto<GridIsListesiItemDto>
            {
                Items = pagedItems,
                TotalCount = orderedProjectGroups.Count,
                Page = page,
                PageSize = pageSize,
                HasMore = page * pageSize < orderedProjectGroups.Count
            };

            return Task.FromResult(Result<GridIsListesiDto>.Success(dto));
        }

        private GridIsListesiItemDto? MapItem(IsListesiRow row)
        {
            var gridEksikMiktar = CalculateGridEksik(
                row.GridDurumuId,
                row.IstenenAdet,
                row.GridGelenAdet,
                row.TrafoSevkAdet);
            var kalanMiktar = CalculateKalan(
                row.GridDurumuId,
                row.IstenenAdet,
                row.GelenMiktar,
                row.StokKarsilanan,
                row.ProjeKarsilanan,
                row.ProjeGonderilen,
                row.TedarikciKarsilanan,
                row.TrafoSevkAdet,
                row.HataliMiktar,
                row.DurumId);

            var tip = GridIsListesiSiniflandirma.Belirle(
                row.GridDurumuId,
                row.GridSevkDurumuId,
                row.GridSevkMiktari ?? 0,
                row.YenidenSevkGerekliAdet,
                row.ProjeGonderilen,
                gridEksikMiktar,
                kalanMiktar);
            if (tip == null)
            {
                return null;
            }

            var sandikNo = string.IsNullOrWhiteSpace(row.FiiliSandikNo)
                ? row.CekideGecenSandikNo
                : row.FiiliSandikNo;

            return new GridIsListesiItemDto
            {
                CekiSatiriId = row.Id,
                ProjeId = row.ProjeId,
                ProjeTipiId = row.ProjeTipiId,
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
                GridEksikMiktar = gridEksikMiktar,
                GridSevkMiktari = row.GridSevkMiktari ?? 0,
                TrafoSevkAdet = row.TrafoSevkAdet,
                YenidenSevkGerekliAdet = row.YenidenSevkGerekliAdet,
                UcKGelenMiktar = row.GelenMiktar,
                KalanMiktar = kalanMiktar,
                GridDurumuId = row.GridDurumuId,
                GridDurumuMetni = _lookupCache.GetDeger<LookupGridDurum>(row.GridDurumuId),
                GridSevkDurumuId = row.GridSevkDurumuId,
                GridSevkDurumuMetni = _lookupCache.GetDeger<LookupGridSevkDurum>(row.GridSevkDurumuId),
                UcKDurumuId = row.UcKDurumuId,
                UcKDurumuMetni = _lookupCache.GetDeger<LookupUcKDurum>(row.UcKDurumuId),
                GridAciklama = row.GridAciklama,
                GridSevkTarihi = row.GridSevkTarihi,
                SonIslemTarihi = row.UpdatedDate ?? row.GridSevkTarihi ?? row.CreatedDate,
                IsTipi = tip.IsTipi,
                IsTipiMetni = tip.IsTipiMetni,
                Oncelik = tip.Oncelik
            };
        }

        private static decimal CalculateGridEksik(
            int gridDurumuId,
            decimal istenenAdet,
            decimal gridGelenAdet,
            decimal trafoSevkAdet)
        {
            if (gridDurumuId == (int)GridDurum.Iptal ||
                gridDurumuId == (int)GridDurum.GridKapandi)
            {
                return 0;
            }

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
            decimal trafoSevkAdet,
            decimal hataliMiktar,
            int durumId)
        {
            if (gridDurumuId == (int)GridDurum.GridKapandi ||
                gridDurumuId == (int)GridDurum.Iptal)
            {
                return 0;
            }

            var kalan =
                istenenAdet - gelenMiktar - stokKarsilanan - projeKarsilanan -
                tedarikciKarsilanan + projeGonderilen - trafoSevkAdet;

            if ((hataliMiktar > 0 || durumId == (int)UrunDurum.HataliUyumsuzGonderim) &&
                kalan <= 0)
            {
                return 1;
            }

            return Math.Max(kalan, 0);
        }

        private static string? NormalizeTip(string? isTipi)
        {
            if (string.IsNullOrWhiteSpace(isTipi) ||
                isTipi.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var normalized = isTipi.Trim().ToLowerInvariant();
            return normalized is GridIsListesiSiniflandirma.TipYeniden or GridIsListesiSiniflandirma.TipEksik
                ? normalized
                : null;
        }

        private static bool IsTodayGridOperation(GridIsListesiItemDto item)
        {
            var operationDate = item.SonIslemTarihi ?? item.GridSevkTarihi;
            return operationDate?.Date == TurkeyTime.Now.Date;
        }

        private static DateTime GetOperationDate(GridIsListesiItemDto item)
        {
            return item.SonIslemTarihi ?? item.GridSevkTarihi ?? DateTime.MinValue;
        }

        private static int GetSandikSira(string? sandikNo)
        {
            if (string.IsNullOrWhiteSpace(sandikNo))
            {
                return int.MaxValue;
            }

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
            public decimal GelenMiktar { get; set; }
            public decimal StokKarsilanan { get; set; }
            public decimal ProjeKarsilanan { get; set; }
            public decimal ProjeGonderilen { get; set; }
            public decimal TedarikciKarsilanan { get; set; }
            public decimal HataliMiktar { get; set; }
            public int DurumId { get; set; }
            public DateTime? UpdatedDate { get; set; }
            public DateTime CreatedDate { get; set; }
            public int ProjeId { get; set; }
            public int ProjeTipiId { get; set; }
            public string ProjeNo { get; set; } = string.Empty;
            public string Musteri { get; set; } = string.Empty;
        }
    }
}
