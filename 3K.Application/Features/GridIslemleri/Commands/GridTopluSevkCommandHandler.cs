using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    public class GridTopluSevkCommandHandler : IRequestHandler<GridTopluSevkCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;
        private readonly ILookupCacheService _lookupCache;

        public GridTopluSevkCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService,
            ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
            _lookupCache = lookupCache;
        }

        public async Task<Result> Handle(GridTopluSevkCommand request, CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || request.CekiSatiriIdler.Count == 0)
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = await repo.FindAsync(cs =>
                request.CekiSatiriIdler.Contains(cs.Id));

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var tadilattakiSatirlar = satirlar
                .Where(s => s.KaliteDurumId.HasValue
                    && _lookupCache.GetDeger<LookupKaliteDurum>(s.KaliteDurumId.Value) == "Tadilatta")
                .ToList();

            if (tadilattakiSatirlar.Any())
            {
                var detay = string.Join(", ", tadilattakiSatirlar
                    .Take(5)
                    .Select(s => $"{(string.IsNullOrWhiteSpace(s.BarkodNo) ? s.SiraNo.ToString() : s.BarkodNo)} - {s.Aciklama}"));
                var kalan = tadilattakiSatirlar.Count > 5 ? $" (+{tadilattakiSatirlar.Count - 5})" : string.Empty;

                return Result.Failure(
                    $"Kalite durumu 'Tadilatta' olan ürünler toplu sevk edilemez: {detay}{kalan}",
                    400);
            }

            var now = DateTime.UtcNow;
            var kullaniciId = _currentUserService.UserId ?? 0;
            int guncellenen = 0;
            var atlananlar = new List<string>();
            var sevkEdilenSatirlar = new List<CekiSatiri>();

            foreach (var satir in satirlar)
            {
                var yenidenSevkAkisi =
                    satir.GridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli &&
                    satir.YenidenSevkGerekliAdet > 0;
                var projeTransferYenidenSevkAkisi =
                    satir.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi &&
                    (satir.GridSevkMiktari ?? 0) > 0 &&
                    satir.ProjeGonderilen > 0 &&
                    satir.KalanMiktar > 0;

                if (!yenidenSevkAkisi && !projeTransferYenidenSevkAkisi &&
                    (satir.UcKDurumuId != (int)UcKDurum.Bekliyor || satir.GelenMiktar > 0 || satir.KarsilananMiktar > 0))
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - 3K tarafÄ±nda iÅŸlem yapÄ±lmÄ±ÅŸ");
                    continue;
                }

                var sevkMiktari = satir.IstenenAdet;

                if (yenidenSevkAkisi)
                {
                    sevkMiktari = satir.YenidenSevkGerekliAdet;
                    satir.YenidenSevkGerekliAdet = 0;
                    satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
                    satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
                    satir.TeslimTarihi = null;
                }
                else if (projeTransferYenidenSevkAkisi)
                {
                    sevkMiktari = Math.Min(satir.ProjeGonderilen, satir.KalanMiktar);
                    satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
                    satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
                    satir.TeslimTarihi = null;
                }
                else if (satir.GridDurumuId == (int)GridDurum.TrafoSevk)
                {
                    if (satir.GridGelenAdet <= 0)
                    {
                        atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - Trafo sevk, Grid'e gelen miktar yok");
                        continue;
                    }

                    sevkMiktari = satir.GridGelenAdet;
                }
                else
                {
                    satir.GridDurumuId = (int)GridDurum.TamGeldi;
                    satir.GridGelenAdet = satir.IstenenAdet;
                    satir.TrafoSevkAdet = 0;
                }

                satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
                satir.GridSevkMiktari = sevkMiktari;
                satir.GridSevkTarihi = now;
                satir.GridPersonelId = kullaniciId;
                satir.GridAciklama = request.Aciklama;

                // Genel durumu otomatik hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);

                repo.Update(satir);
                guncellenen++;
                sevkEdilenSatirlar.Add(satir);
            }

            if (guncellenen == 0)
                return Result.Failure("Sevk edilebilecek urun bulunamadi. Trafo sevk satirlari icin Grid gelen adet 0 olamaz.", 400);

            await _unitOfWork.SaveChangesAsync();

            var sandikGruplari = sevkEdilenSatirlar.GroupBy(s => s.FiiliSandikNo ?? s.CekideGecenSandikNo ?? "Belirsiz");
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Sevk Durumu: Sevk Edildi");
            sb.AppendLine($"{guncellenen} adet ürün toplu sevk edildi.\n");
            if (atlananlar.Any())
            {
                sb.AppendLine($"Atlanan ({atlananlar.Count}):");
                foreach (var atlanan in atlananlar.Take(10))
                    sb.AppendLine($"  - {atlanan}");
                sb.AppendLine();
            }
            
            foreach (var grup in sandikGruplari)
            {
                sb.AppendLine($"📦 Sandık: {grup.Key}");
                foreach (var s in grup)
                {
                    sb.AppendLine($"  • {s.OlcuResmiPozNo ?? s.SiraNo.ToString()} - {s.Aciklama}");
                }
                sb.AppendLine(); // Boşluk
            }

            if (!string.IsNullOrWhiteSpace(request.Aciklama))
            {
                sb.AppendLine($"Not: {request.Aciklama}");
            }

            // Toplu hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = kullaniciId,
                ReferansTipi = "TopluSevk",
                ReferansId = string.Join(",", request.CekiSatiriIdler),
                Islem = "Grid Toplu Sevk",
                IslemTipiId = (int)IslemTipi.GridTopluSevkEdildi,
                EskiDeger = ((int)GridDurum.Bekliyor).ToString(),
                YeniDeger = ((int)GridDurum.TamGeldi).ToString(),
                Aciklama = sb.ToString().TrimEnd()
            });

            return Result.Success();
        }
    }
}
