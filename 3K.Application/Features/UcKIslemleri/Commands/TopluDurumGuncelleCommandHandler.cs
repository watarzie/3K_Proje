using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// Madde 5: Toplu TamGeldi handler.
    /// Seçilen tüm CekiSatiri kayıtlarını TamGeldi olarak işaretler,
    /// SandikIcerik senkronizasyonu yapar ve tek toplu HareketGecmisi kaydı oluşturur.
    /// </summary>
    public class TopluDurumGuncelleCommandHandler : IRequestHandler<TopluDurumGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        public TopluDurumGuncelleCommandHandler(
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

        public async Task<Result> Handle(TopluDurumGuncelleCommand request, CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || request.CekiSatiriIdler.Count == 0)
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = await repo.FindAsync(cs => request.CekiSatiriIdler.Contains(cs.Id));

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var now = DateTime.UtcNow;
            var kullaniciId = _currentUserService.UserId ?? 0;
            int guncellenen = 0;
            var atlananlar = new List<string>();

            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            foreach (var satir in satirlar)
            {
                // Grid İptal veya TrafoSevk → atla
                if (satir.GridDurumuId == (int)GridDurum.Iptal)
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - Grid İptal");
                    continue;
                }
                if (satir.GridDurumuId == (int)GridDurum.TrafoSevk)
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - Trafo Sevk");
                    continue;
                }

                // Grid sevk edilmemişse TamGeldi yapılamaz
                if (satir.GridSevkDurumuId != (int)GridSevkDurum.SevkEdildi)
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - Grid henüz sevk etmedi");
                    continue;
                }

                // TamGeldi işareti — KURAL 1: Grid'in sevk ettiği miktar kadar teslim al
                satir.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
                satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
                var sevkMiktari = satir.GridSevkMiktari ?? (satir.IstenenAdet - satir.GelenMiktar - satir.StokKarsilanan - satir.ProjeKarsilanan - satir.TedarikciKarsilanan);
                satir.GelenMiktar += Math.Max(sevkMiktari, 0);
                satir.TeslimTarihi = now;
                satir.UcKAciklama = request.Aciklama;

                // Genel durumu hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                // KURAL 2: Merkezi kalan hesaplaması ve durum override
                _durumHesaplaService.HesaplaKalanVeDurum(satir);

                repo.Update(satir);

                // SandikIcerik senkronizasyonu
                var toplam = satir.GelenMiktar + satir.KarsilananMiktar;
                var ilgiliIcerikler = await sandikIcerikRepo.FindAsync(x => x.CekiSatiriId == satir.Id);
                if (ilgiliIcerikler.Any())
                {
                    var anaIcerik = ilgiliIcerikler.First();
                    anaIcerik.KonulanAdet = toplam;
                    anaIcerik.StokKarsilanan = satir.StokKarsilanan;
                    anaIcerik.ProjeKarsilanan = satir.ProjeKarsilanan;
                    anaIcerik.TedarikciKarsilanan = satir.TedarikciKarsilanan;
                    sandikIcerikRepo.Update(anaIcerik);
                }

                guncellenen++;
            }

            if (guncellenen == 0)
                return Result.Failure("Hiçbir ürün güncellenemedi. Tümü Grid sevk/iptal kontrolünü geçemedi.");

            await _unitOfWork.SaveChangesAsync();

            // Toplu hareket kaydı
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"3K Toplu Tam Geldi: {guncellenen}/{request.CekiSatiriIdler.Count} ürün güncellendi.");

            if (atlananlar.Any())
            {
                sb.AppendLine($"\nAtlanan ({atlananlar.Count}):");
                foreach (var a in atlananlar)
                    sb.AppendLine($"  • {a}");
            }

            if (!string.IsNullOrWhiteSpace(request.Aciklama))
                sb.AppendLine($"\nAçıklama: {request.Aciklama}");

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = kullaniciId,
                ReferansTipi = "TopluDurumGuncelleme",
                ReferansId = string.Join(",", request.CekiSatiriIdler),
                Islem = "3K Toplu Tam Geldi",
                IslemTipiId = (int)IslemTipi.TopluDurumGuncellendi,
                YeniDeger = ((int)UcKDurum.TamGeldi).ToString(),
                Aciklama = sb.ToString().TrimEnd()
            });

            return Result.Success();
        }
    }
}
