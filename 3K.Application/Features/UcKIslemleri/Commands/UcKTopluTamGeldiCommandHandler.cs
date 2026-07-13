using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    public class UcKTopluTamGeldiCommandHandler : IRequestHandler<UcKTopluTamGeldiCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public UcKTopluTamGeldiCommandHandler(
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

        public async Task<Result> Handle(UcKTopluTamGeldiCommand request, CancellationToken cancellationToken)
        {
            var secimler = UcKSandikSecimHelper.Olustur(request.CekiSatiriIdler, request.Secimler);
            if (!secimler.Any())
                return Result.Failure("En az bir ürün seçilmelidir.");

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                secimler.Select(s => s.CekiSatiriId));
            var basarili = 0;
            var hatalar = new List<string>();
            var kaynakSatirIds = new HashSet<int>();

            foreach (var secim in secimler)
            {
                var cekiSatiriId = secim.CekiSatiriId;
                var satir = await repo.GetByIdAsync(cekiSatiriId);
                if (satir == null) { hatalar.Add($"ID {cekiSatiriId}: Ürün bulunamadı."); continue; }
                if (kilitliSatirIdleri.Contains(cekiSatiriId)) { hatalar.Add($"ID {cekiSatiriId}: {SandikSevkKilidiHelper.UrunKilitliMesaji}"); continue; }
                if (await SahaAktarimBlokajHelper.KaynakSatirAktarildiMiAsync(_sahaTamamlamaService, satir, cancellationToken))
                { hatalar.Add($"ID {cekiSatiriId}: {SahaAktarimBlokajHelper.UcKMesaji}"); continue; }

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

                var seciliIcerikResult = await UcKSandikIcerikSenkronizasyonHelper.GetSeciliIcerikAsync(
                    _unitOfWork,
                    satir.Id,
                    secim.SandikIcerikId);
                if (!seciliIcerikResult.IsSuccess)
                { hatalar.Add($"ID {cekiSatiriId}: {seciliIcerikResult.Error!.Message}"); continue; }

                var seciliIcerik = seciliIcerikResult.Value;
                var sandikMiktari = seciliIcerik == null
                    ? satir.IstenenAdet
                    : seciliIcerik.TahsisMiktari > 0 ? seciliIcerik.TahsisMiktari : satir.IstenenAdet;
                var sandikKalan = seciliIcerik == null
                    ? Math.Max(satir.KalanMiktar, 0)
                    : Math.Max(sandikMiktari - seciliIcerik.KonulanAdet, 0);

                // Yalnızca seçili tahsis zaten tamamlandıysa atla.
                if (sandikKalan <= 0) continue;

                var eskiDurum = satir.UcKKarsilamaTipiId;

                // TamGeldi mantığı — mevcut tek handler ile aynı
                var sandikSevkKalan = sandikKalan;
                if (seciliIcerik != null)
                {
                    var sandikSevkPayi = UcKSandikIcerikSenkronizasyonHelper.ToplamdanSeciliTahsisPayi(
                        _unitOfWork,
                        satir,
                        seciliIcerik,
                        satir.GridSevkMiktari ?? satir.GridGelenAdet);
                    var sandikGridKaynakliKonulan = Math.Max(
                        seciliIcerik.KonulanAdet - seciliIcerik.StokKarsilanan - seciliIcerik.ProjeKarsilanan - seciliIcerik.TedarikciKarsilanan,
                        0);
                    sandikSevkKalan = Math.Max(sandikSevkPayi - sandikGridKaynakliKonulan, 0);
                }
                var sevkMiktari = Math.Min(sandikKalan, sandikSevkKalan);
                satir.GelenMiktar += Math.Max(sevkMiktari, 0);
                satir.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
                satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
                satir.TeslimTarihi = TurkeyTime.Now;
                satir.UcKAciklama = request.Aciklama;

                // Genel durumu hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                _durumHesaplaService.HesaplaKalanVeDurum(satir);

                repo.Update(satir);

                // Sandık İçerik Senkronizasyonu
                var senkronizasyonResult = await UcKSandikIcerikSenkronizasyonHelper.SenkronizeAsync(
                    _unitOfWork,
                    satir,
                    secim.SandikIcerikId);
                if (!senkronizasyonResult.IsSuccess)
                    return Result.Failure($"ID {cekiSatiriId}: {senkronizasyonResult.Error!.Message}");

                var ilgiliIcerikler = senkronizasyonResult.Value ?? new List<SandikIcerik>();

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
                    Islem = "Toplu Sevk Adeti Tam Geldi",
                    IslemTipiId = (int)IslemTipi.UcKDurumGuncellendi,
                    EskiDeger = eskiDurum.ToString(),
                    YeniDeger = ((int)UcKDurum.TamGeldi).ToString(),
                    Aciklama = $"Toplu TamGeldi — {(string.IsNullOrWhiteSpace(request.Aciklama) ? "Açıklama yok" : request.Aciklama)}"
                });
            }

            await _unitOfWork.SaveChangesAsync();

            if (kaynakSatirIds.Count > 0)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(kaynakSatirIds, cancellationToken);

            if (hatalar.Any())
                return Result.Failure($"{basarili} ürün güncellendi, {hatalar.Count} hata: {string.Join("; ", hatalar.Take(3))}");

            return Result.Success();
        }
    }
}
