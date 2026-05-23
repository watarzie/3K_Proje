using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.HareketGecmisiIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.HareketGecmisiIslemleri.Queries
{
    public class GetProjeHareketleriQueryHandler : IRequestHandler<GetProjeHareketleriQuery, Result<PaginatedList<HareketGecmisiDto>>>
    {
        private readonly IHareketService _hareketService;
        private readonly IUnitOfWork _unitOfWork;

        public GetProjeHareketleriQueryHandler(IHareketService hareketService, IUnitOfWork unitOfWork)
        {
            _hareketService = hareketService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PaginatedList<HareketGecmisiDto>>> Handle(GetProjeHareketleriQuery request, CancellationToken cancellationToken)
        {
            var pagedData = await _hareketService.GetPaginatedProjeHareketleriAsync(
                request.ProjeId, request.SearchTerm, request.IslemTipiId, request.PageNumber, request.PageSize);
            var hareketler = pagedData.Items;

            var cekiSatirlari = await _unitOfWork.GetRepository<CekiSatiri>().FindAsync(x => x.Ceki.ProjeId == request.ProjeId);
            var sandiklar = await _unitOfWork.GetRepository<Sandik>().FindAsync(x => x.ProjeId == request.ProjeId);

            var cekiDict = cekiSatirlari.ToDictionary(x => x.Id.ToString(), x => x);
            var sandikDict = sandiklar.ToDictionary(x => x.Id.ToString(), x => x);
            
            var sandikIdList = sandiklar.Select(s => s.Id).ToList();
            var icerikler = await _unitOfWork.GetRepository<SandikIcerik>().FindAsync(x => sandikIdList.Contains(x.SandikId));
            var icerikDict = icerikler.ToDictionary(x => x.Id.ToString(), x => x);

            var result = hareketler.Select(h => 
            {
                string refTipi = h.ReferansTipi;
                string refId = h.ReferansId ?? "";

                // Use denormalized ReferansMetni if available (new records)
                if (!string.IsNullOrWhiteSpace(h.ReferansMetni))
                {
                    // Still map ReferansTipi to readable name
                    refTipi = h.ReferansTipi switch
                    {
                        "CekiSatiri" => "Ürün",
                        "Sandik" => "Sandık",
                        "SandikIcerik" => "Sandık İçeriği",
                        "StokHareketi" => "Stok Hareketi",
                        "Proje" => "Proje Geneli",
                        _ when h.ReferansTipi.Contains("Toplu", StringComparison.OrdinalIgnoreCase) => "Toplu İşlem",
                        _ => h.ReferansTipi
                    };
                    refId = h.ReferansMetni;
                }
                else
                {
                    // Fallback: dictionary resolution for old records (existing logic)
                    if (h.ReferansTipi.Equals("CekiSatiri", StringComparison.OrdinalIgnoreCase))
                    {
                        refTipi = "Ürün";
                        if (cekiDict.TryGetValue(refId, out var ceki))
                            refId = $"Poz: {ceki.OlcuResmiPozNo} - {ceki.Aciklama}";
                        else
                            refId = "Eski / Silinmiş Ürün";
                    }
                    else if (h.ReferansTipi.Equals("Sandik", StringComparison.OrdinalIgnoreCase))
                    {
                        refTipi = "Sandık";
                        if (sandikDict.TryGetValue(refId, out var sandik))
                            refId = $"No: {sandik.SandikNo}";
                        else
                            refId = "Eski / Silinmiş Sandık";
                    }
                    else if (h.ReferansTipi.Equals("SandikIcerik", StringComparison.OrdinalIgnoreCase))
                    {
                        refTipi = "Sandık İçeriği";
                        if (icerikDict.TryGetValue(refId, out var icerik))
                        {
                            var urunTanim = cekiDict.TryGetValue(icerik.CekiSatiriId.ToString(), out var urun) ? urun.Aciklama : "Ürün";
                            var sandikNo = sandikDict.TryGetValue(icerik.SandikId.ToString(), out var s) ? s.SandikNo : "?";
                            refId = $"Sandık {sandikNo} -> {urunTanim}";
                        }
                        else
                        {
                            refId = "Geçmiş / Taşınmış Kayıt";
                        }
                    }
                    else if (h.ReferansTipi.Equals("StokHareketi", StringComparison.OrdinalIgnoreCase))
                    {
                        refTipi = "Stok Hareketi";
                        refId = "Geçmiş Stok İşlemi";
                    }
                    else if (h.ReferansTipi.Equals("Proje", StringComparison.OrdinalIgnoreCase))
                    {
                        refTipi = "Proje Geneli";
                        refId = "-";
                    }
                    else if (h.ReferansTipi.Contains("Toplu", StringComparison.OrdinalIgnoreCase))
                    {
                        refTipi = "Toplu İşlem";
                        refId = "Birden fazla kayıt";
                    }
                }

                return new HareketGecmisiDto
                {
                    Id = h.Id,
                    Islem = h.Islem,
                    IslemTipiMetni = h.IslemTipiLookup?.Deger ?? h.Islem,
                    ReferansTipi = refTipi,
                    ReferansId = refId,
                    EskiDeger = GetDegerMetni(h.IslemTipiId, h.Islem, h.EskiDeger),
                    YeniDeger = GetDegerMetni(h.IslemTipiId, h.Islem, h.YeniDeger),
                    Aciklama = h.Aciklama,
                    KullaniciAdi = h.Kullanici?.AdSoyad ?? "",
                    Tarih = h.Tarih
                };
            }).ToList();

            var paginatedList = new PaginatedList<HareketGecmisiDto>(result, pagedData.TotalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedList<HareketGecmisiDto>>.Success(paginatedList);
        }

        private string? GetDegerMetni(int? islemTipiId, string? islemMetni, string? deger)
        {
            if (string.IsNullOrWhiteSpace(deger) || !int.TryParse(deger, out int intVal))
                return deger;

            if (islemTipiId.HasValue)
            {
                switch ((IslemTipi)islemTipiId.Value)
                {
                    case IslemTipi.GridDurumGuncellendi:
                    case IslemTipi.GridTopluSevkEdildi:
                        return Enum.GetName(typeof(GridDurum), intVal) ?? deger;

                    case IslemTipi.UcKDurumGuncellendi:
                    case IslemTipi.UcKTeslimAlindi:
                    case IslemTipi.UcKTopluTeslimAlindi:
                        return Enum.GetName(typeof(UcKDurum), intVal) ?? deger;

                    case IslemTipi.SandikKapatildi:
                    case IslemTipi.TopluSandikKapatildi:
                    case IslemTipi.SandikOtomatikHazirlandi:
                        return Enum.GetName(typeof(SandikDurum), intVal) ?? deger;

                    case IslemTipi.SandikLokasyonGuncellendi:
                        return Enum.GetName(typeof(DepoLokasyon), intVal) ?? deger;

                    case IslemTipi.UrunTasindi:
                    case IslemTipi.FiiliSandikDegistirildi:
                        return $"Sandık {deger}";
                }
            }
            else if (!string.IsNullOrWhiteSpace(islemMetni))
            {
                if (islemMetni.Contains("Grid", StringComparison.OrdinalIgnoreCase))
                    return Enum.GetName(typeof(GridDurum), intVal) ?? deger;
                if (islemMetni.Contains("3K", StringComparison.OrdinalIgnoreCase))
                    return Enum.GetName(typeof(UcKDurum), intVal) ?? deger;
                if (islemMetni.Contains("Sandık", StringComparison.OrdinalIgnoreCase) && islemMetni.Contains("Kapat", StringComparison.OrdinalIgnoreCase))
                    return Enum.GetName(typeof(SandikDurum), intVal) ?? deger;
                if (islemMetni.Contains("Lokasyon", StringComparison.OrdinalIgnoreCase))
                    return Enum.GetName(typeof(DepoLokasyon), intVal) ?? deger;
                if (islemMetni.Contains("Taşıma", StringComparison.OrdinalIgnoreCase) || islemMetni.Contains("Fiili", StringComparison.OrdinalIgnoreCase))
                    return $"Sandık {deger}";
                if (islemMetni.Contains("Proje", StringComparison.OrdinalIgnoreCase) && (islemMetni.Contains("Kilit", StringComparison.OrdinalIgnoreCase) || islemMetni.Contains("Sevk", StringComparison.OrdinalIgnoreCase)))
                    return Enum.GetName(typeof(ProjeDurum), intVal) ?? deger;
            }

            return deger;
        }
    }
}
