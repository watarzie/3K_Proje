using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using System.Globalization;
using _3K.Core.Helpers;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    public class UcKTopluTedarikciCommandHandler : IRequestHandler<UcKTopluTedarikciCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public UcKTopluTedarikciCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(UcKTopluTedarikciCommand request, CancellationToken cancellationToken)
        {
            if (request.CekiSatiriIdler == null || !request.CekiSatiriIdler.Any())
                return Result.Failure("En az bir ürün seçilmelidir.");

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                request.CekiSatiriIdler);
            var basarili = 0;
            var hatalar = new List<string>();
            var kaynakSatirIds = new HashSet<int>();

            foreach (var cekiSatiriId in request.CekiSatiriIdler)
            {
                var satir = await repo.GetByIdAsync(cekiSatiriId);
                if (satir == null) { hatalar.Add($"ID {cekiSatiriId}: Ürün bulunamadı."); continue; }
                if (kilitliSatirIdleri.Contains(cekiSatiriId)) { hatalar.Add($"ID {cekiSatiriId}: {SandikSevkKilidiHelper.UrunKilitliMesaji}"); continue; }
                if (await SahaAktarimBlokajHelper.KaynakSatirAktarildiMiAsync(_sahaTamamlamaService, satir, cancellationToken))
                { hatalar.Add($"ID {cekiSatiriId}: {SahaAktarimBlokajHelper.UcKMesaji}"); continue; }

                // Grid blokaj kontrolleri
                if (satir.GridDurumuId == (int)GridDurum.Iptal ||
                    satir.GridDurumuId == (int)GridDurum.GridKapandi)
                { hatalar.Add($"ID {cekiSatiriId}: Grid durumu uygun değil."); continue; }

                // Kalan miktarı hesapla
                var kalan = satir.KalanMiktar;
                if (kalan <= 0) continue; // Zaten tamamlanmış, atla

                if (!TedarikcidenKarsilamaYapilabilir(satir))
                {
                    hatalar.Add($"ID {cekiSatiriId}: Tedarikçiden karşılama için Grid/3K durumu uygun değil.");
                    continue;
                }

                var eskiDurum = satir.UcKKarsilamaTipiId;

                // Tedarikçiden Geldi mantığı — kalan miktar kadar karşıla
                satir.KarsilananMiktar += kalan;
                satir.TedarikciKarsilanan += kalan;
                satir.UcKKarsilamaTipiId = (int)UcKDurum.TedarikcidenGeldi;
                satir.UcKDurumuId = (int)UcKDurum.TedarikcidenGeldi;
                satir.TeslimTarihi = TurkeyTime.Now;
                satir.UcKAciklama = request.Aciklama;
                KapatYenidenSevkIhtiyaci(satir, kalan);

                // Genel durumu hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                _durumHesaplaService.HesaplaKalanVeDurum(satir);

                repo.Update(satir);

                // Sandık İçerik Senkronizasyonu
                var ilgiliIcerikler = (await sandikIcerikRepo.FindAsync(x => x.CekiSatiriId == satir.Id)).ToList();
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

                await SandikLokasyonHelper.VarsayilanUcKDepoLokasyonuAtaAsync(_unitOfWork, ilgiliIcerikler);

                if (satir.KaynakCekiSatiriId.HasValue)
                    kaynakSatirIds.Add(satir.KaynakCekiSatiriId.Value);

                basarili++;

                // Hareket kaydı
                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    ReferansTipi = "CekiSatiri",
                    ReferansId = satir.Id.ToString(),
                    Islem = "Toplu Tedarikçiden Karşılandı",
                    IslemTipiId = (int)IslemTipi.UcKDurumGuncellendi,
                    EskiDeger = eskiDurum.ToString(),
                    YeniDeger = ((int)UcKDurum.TedarikcidenGeldi).ToString(),
                    Aciklama = $"Toplu Tedari̇kçi — {FormatAdet(kalan)} adet — {(string.IsNullOrWhiteSpace(request.Aciklama) ? "Açıklama yok" : request.Aciklama)}"
                });
            }

            await _unitOfWork.SaveChangesAsync();

            if (kaynakSatirIds.Count > 0)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(kaynakSatirIds, cancellationToken);

            if (hatalar.Any())
                return Result.Failure($"{basarili} ürün güncellendi, {hatalar.Count} hata: {string.Join("; ", hatalar.Take(3))}");

            return Result.Success();
        }

        private static void KapatYenidenSevkIhtiyaci(CekiSatiri satir, decimal karsilananAdet)
        {
            if (satir.YenidenSevkGerekliAdet <= 0 || karsilananAdet <= 0)
                return;

            satir.YenidenSevkGerekliAdet = Math.Max(satir.YenidenSevkGerekliAdet - karsilananAdet, 0);

            if (satir.YenidenSevkGerekliAdet <= 0 &&
                satir.GridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli)
            {
                satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
            }
        }

        private static bool TedarikcidenKarsilamaYapilabilir(CekiSatiri satir)
        {
            var gridKaynakKarsilamaAcik =
                satir.GridDurumuId == (int)GridDurum.EksikGeldi ||
                satir.GridDurumuId == (int)GridDurum.Gelmedi ||
                satir.GridDurumuId == (int)GridDurum.TrafoSevk;

            var geriGonderimSonrasiKaynakAcik =
                satir.KalanMiktar > 0 &&
                (satir.UcKKarsilamaTipiId == (int)UcKDurum.GeriGonderildi ||
                 satir.GeriGonderilenMiktar > 0 ||
                 satir.YenidenSevkGerekliAdet > 0);

            return gridKaynakKarsilamaAcik || geriGonderimSonrasiKaynakAcik;
        }

        private static string FormatAdet(decimal value)
        {
            if (decimal.Truncate(value) == value)
                return decimal.Truncate(value).ToString(CultureInfo.InvariantCulture);
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
