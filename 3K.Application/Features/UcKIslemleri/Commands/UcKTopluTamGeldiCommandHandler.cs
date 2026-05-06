using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    public class UcKTopluTamGeldiCommandHandler : IRequestHandler<UcKTopluTamGeldiCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        public UcKTopluTamGeldiCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(UcKTopluTamGeldiCommand request, CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || !request.CekiSatiriIdler.Any())
                return Result.Failure("En az bir ürün seçilmelidir.");

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var basarili = 0;
            var hatalar = new List<string>();

            foreach (var cekiSatiriId in request.CekiSatiriIdler)
            {
                var satir = await repo.GetByIdAsync(cekiSatiriId);
                if (satir == null) { hatalar.Add($"ID {cekiSatiriId}: Ürün bulunamadı."); continue; }

                // Grid blokaj kontrolleri
                if (satir.GridDurumuId == (int)GridDurum.Iptal ||
                    satir.GridDurumuId == (int)GridDurum.GridKapandi)
                { hatalar.Add($"ID {cekiSatiriId}: Grid durumu uygun değil."); continue; }

                if (satir.GridDurumuId == (int)GridDurum.TrafoSevk &&
                    (satir.GridSevkDurumuId != (int)GridSevkDurum.SevkEdildi || (satir.GridSevkMiktari ?? 0) <= 0))
                { hatalar.Add($"ID {cekiSatiriId}: Trafo sevk satirinda 3K'ya sevk edilmis Grid gelen miktar yok."); continue; }

                // Grid sevk kontrolü
                if (satir.GridSevkDurumuId != (int)GridSevkDurum.SevkEdildi)
                { hatalar.Add($"ID {cekiSatiriId}: Grid henüz sevk etmedi."); continue; }

                // Zaten TamGeldi ise atla
                if (satir.UcKKarsilamaTipiId == (int)UcKDurum.TamGeldi) continue;

                var eskiDurum = satir.UcKKarsilamaTipiId;

                // TamGeldi mantığı — mevcut tek handler ile aynı
                var sevkMiktari = satir.GridSevkMiktari ?? (satir.IstenenAdet - satir.GelenMiktar - satir.StokKarsilanan - satir.ProjeKarsilanan - satir.TedarikciKarsilanan);
                satir.GelenMiktar += Math.Max(sevkMiktari, 0);
                satir.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
                satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
                satir.TeslimTarihi = DateTime.UtcNow;
                satir.UcKAciklama = request.Aciklama;

                // Genel durumu hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                _durumHesaplaService.HesaplaKalanVeDurum(satir);

                repo.Update(satir);

                // Sandık İçerik Senkronizasyonu
                var ilgiliIcerikler = await sandikIcerikRepo.FindAsync(x => x.CekiSatiriId == satir.Id);
                if (ilgiliIcerikler.Any())
                {
                    var anaIcerik = ilgiliIcerikler.First();
                    var toplam = satir.GelenMiktar + satir.KarsilananMiktar;
                    anaIcerik.KonulanAdet = toplam;
                    anaIcerik.StokKarsilanan = satir.StokKarsilanan;
                    anaIcerik.ProjeKarsilanan = satir.ProjeKarsilanan;
                    anaIcerik.TedarikciKarsilanan = satir.TedarikciKarsilanan;
                    sandikIcerikRepo.Update(anaIcerik);
                }

                basarili++;

                // Hareket kaydı
                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    ReferansTipi = "CekiSatiri",
                    ReferansId = satir.Id.ToString(),
                    Islem = "Toplu Sevk Adeti Tam Geldi",
                    IslemTipiId = (int)IslemTipi.UcKDurumGuncellendi,
                    EskiDeger = eskiDurum.ToString(),
                    YeniDeger = ((int)UcKDurum.TamGeldi).ToString(),
                    Aciklama = $"Toplu TamGeldi — {(string.IsNullOrWhiteSpace(request.Aciklama) ? "Açıklama yok" : request.Aciklama)}"
                });
            }

            await _unitOfWork.SaveChangesAsync();

            if (hatalar.Any())
                return Result.Failure($"{basarili} ürün güncellendi, {hatalar.Count} hata: {string.Join("; ", hatalar.Take(3))}");

            return Result.Success();
        }
    }
}
