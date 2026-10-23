using System.Globalization;
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
    /// Mal kabul kayıtlarını <see cref="IGridHareketOkuyucu.GridHareketleriAsync"/> ile (canlı ∪ arşiv, tarihe göre,
    /// sayfalı) okur; çeki satırı referanslı kayıtlara ürün/sandık bilgisini ve satırın güncel Grid gelen adedini,
    /// projelere tipini ekler, önceki Grid durumunu lookup metnine çevirir ve açıklamadaki "Gelen:" ekini ayrıştırır.
    /// Toplu sevk kaydı "Toplu sevk (n ürün)" olarak döner. Durum değiştirmez.
    /// </summary>
    public sealed class GetGridMalKabulGecmisiQueryHandler(
        IGridHareketOkuyucu hareketOkuyucu,
        IUnitOfWork unitOfWork,
        IReadQueryExecutor queryExecutor,
        ILookupCacheService lookupCache)
        : IRequestHandler<GetGridMalKabulGecmisiQuery, Result<GridMalKabulGecmisiDto>>
    {
        public async Task<Result<GridMalKabulGecmisiDto>> Handle(GetGridMalKabulGecmisiQuery request, CancellationToken cancellationToken)
        {
            var sayfa = Math.Max(request.PageNumber, 1);
            var sayfaBoyutu = Math.Clamp(request.PageSize, 1, 100);
            var (baslangic, bitis) = request.EtkinAralik(TurkeyTime.Now);

            var sonuc = await hareketOkuyucu.GridHareketleriAsync(
                new GridHareketFiltresi(
                    baslangic,
                    bitis.AddDays(1),
                    GridMalKabulTuru.Cevir(request.Tur),
                    request.PersonelId,
                    request.ProjeId,
                    string.IsNullOrWhiteSpace(request.Arama) ? null : request.Arama.Trim(),
                    sayfa,
                    sayfaBoyutu),
                cancellationToken);

            var satirlar = await SatirBilgileriAsync(sonuc.Kayitlar, cancellationToken);
            var projeTipleri = await ProjeTipleriAsync(sonuc.Kayitlar, cancellationToken);

            return Result<GridMalKabulGecmisiDto>.Success(new GridMalKabulGecmisiDto
            {
                Baslangic = baslangic,
                Bitis = bitis,
                Liste = new PaginatedList<GridMalKabulHareketDto>(
                    sonuc.Kayitlar.Select(k => Esle(k, satirlar, projeTipleri)).ToList(), sonuc.ToplamKayit, sayfa, sayfaBoyutu),
                PersonelOzeti = sonuc.PersonelOzeti
                    .Select(p => new GridMalKabulPersonelDto { KullaniciId = p.KullaniciId, KullaniciAdi = p.KullaniciAdi, IslemSayisi = p.IslemSayisi })
                    .ToList()
            });
        }

        private static int? CekiSatiriId(GridHareketKaydi k) =>
            k.ReferansTipi == GridHareketSabitleri.CekiSatiriReferansi &&
            int.TryParse(k.ReferansId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                ? id
                : null;

        private async Task<Dictionary<int, SatirBilgisi>> SatirBilgileriAsync(IReadOnlyList<GridHareketKaydi> kayitlar, CancellationToken cancellationToken)
        {
            var idler = kayitlar.Select(CekiSatiriId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
            if (idler.Length == 0)
                return [];
            var satirlar = await queryExecutor.ToListAsync(
                queryExecutor.AsNoTracking(unitOfWork.GetRepository<CekiSatiri>().Queryable())
                    .Where(cs => idler.Contains(cs.Id))
                    .Select(cs => new SatirBilgisi
                    {
                        Id = cs.Id,
                        SiraNo = cs.SiraNo,
                        BarkodNo = cs.BarkodNo,
                        OlcuResmiPozNo = cs.OlcuResmiPozNo,
                        Aciklama = cs.Aciklama,
                        FiiliSandikNo = cs.FiiliSandikNo,
                        CekideGecenSandikNo = cs.CekideGecenSandikNo,
                        GridGelenAdet = cs.GridGelenAdet
                    }),
                cancellationToken);
            return satirlar.ToDictionary(x => x.Id);
        }

        private async Task<Dictionary<int, int>> ProjeTipleriAsync(IReadOnlyList<GridHareketKaydi> kayitlar, CancellationToken cancellationToken)
        {
            var projeIdler = kayitlar.Select(k => k.ProjeId).Distinct().ToArray();
            if (projeIdler.Length == 0)
                return [];
            var projeler = await queryExecutor.ToListAsync(
                queryExecutor.AsNoTracking(unitOfWork.GetRepository<Proje>().Queryable())
                    .Where(p => projeIdler.Contains(p.Id))
                    .Select(p => new { p.Id, p.ProjeTipiId }),
                cancellationToken);
            return projeler.ToDictionary(x => x.Id, x => x.ProjeTipiId);
        }

        private GridMalKabulHareketDto Esle(GridHareketKaydi k, Dictionary<int, SatirBilgisi> satirlar, Dictionary<int, int> projeTipleri)
        {
            var topluSevk = k.ReferansTipi == GridHareketSabitleri.TopluSevkReferansi;
            var manuel = k.Islem == GridHareketSabitleri.ManuelUrunIslemi;
            var cekiSatiriId = CekiSatiriId(k);
            SatirBilgisi? satir = cekiSatiriId.HasValue ? satirlar.GetValueOrDefault(cekiSatiriId.Value) : null;
            int? topluUrunSayisi = topluSevk
                ? (k.ReferansId ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().Count()
                : null;

            string urunMetni;
            if (topluSevk)
                urunMetni = $"Toplu sevk ({topluUrunSayisi} ürün)";
            else if (satir != null)
                urunMetni = $"{(string.IsNullOrWhiteSpace(satir.BarkodNo) ? $"#{satir.SiraNo}" : satir.BarkodNo)} - {satir.Aciklama}";
            else
                urunMetni = k.ReferansMetni ?? "Silinmiş ürün";

            var islemMetni = topluSevk
                ? "Toplu sevk"
                : manuel
                    ? "Manuel ürün"
                    : DurumMetni(k.YeniDeger) ?? k.Islem;

            return new GridMalKabulHareketDto
            {
                Anahtar = (k.ArsivdenMi ? "A-" : "C-") + k.Id.ToString(CultureInfo.InvariantCulture),
                Tarih = k.Tarih,
                KullaniciId = k.KullaniciId,
                KullaniciAdi = k.KullaniciAdi,
                ProjeId = k.ProjeId,
                ProjeNo = k.ProjeNo,
                ProjeTipiId = projeTipleri.TryGetValue(k.ProjeId, out var tip) ? tip : null,
                IslemMetni = islemMetni,
                TopluMu = topluSevk || k.Islem.StartsWith(GridHareketSabitleri.TopluIslemOneki, StringComparison.Ordinal),
                CekiSatiriId = topluSevk ? null : cekiSatiriId,
                UrunMetni = urunMetni,
                SandikNo = satir == null ? null : string.IsNullOrWhiteSpace(satir.FiiliSandikNo) ? satir.CekideGecenSandikNo : satir.FiiliSandikNo,
                OncekiDurumMetni = topluSevk || manuel ? null : DurumMetni(k.EskiDeger),
                GelenAdet = GridMalKabulAciklamaKurali.GelenMiktariniAyristir(k.Aciklama),
                GuncelGridGelenAdet = satir?.GridGelenAdet,
                TopluUrunSayisi = topluUrunSayisi,
                Aciklama = k.Aciklama
            };
        }

        /// <summary>Hareketteki Grid durum kimliğini lookup metnine çevirir ("8" → "Tam Geldi"); sayı değilse null.</summary>
        private string? DurumMetni(string? deger)
        {
            if (!int.TryParse(deger, NumberStyles.Integer, CultureInfo.InvariantCulture, out var durum) || !Enum.IsDefined(typeof(GridDurum), durum))
                return null;
            var metin = lookupCache.GetDeger<LookupGridDurum>(durum);
            return string.IsNullOrWhiteSpace(metin) || metin == "?" ? null : metin;
        }

        private sealed class SatirBilgisi
        {
            public int Id { get; init; }
            public int SiraNo { get; init; }
            public string BarkodNo { get; init; } = string.Empty;
            public string? OlcuResmiPozNo { get; init; }
            public string Aciklama { get; init; } = string.Empty;
            public string? FiiliSandikNo { get; init; }
            public string CekideGecenSandikNo { get; init; } = string.Empty;
            public decimal GridGelenAdet { get; init; }
        }
    }
}
