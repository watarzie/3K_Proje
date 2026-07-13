using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

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
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public TopluDurumGuncelleCommandHandler(
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

        public async Task<Result> Handle(TopluDurumGuncelleCommand request, CancellationToken cancellationToken)
        {
            var secimler = UcKSandikSecimHelper.Olustur(request.CekiSatiriIdler, request.Secimler);
            if (!secimler.Any())
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var seciliSatirIdleri = secimler.Select(s => s.CekiSatiriId).Distinct().ToList();
            var satirlar = (await repo.FindAsync(cs => seciliSatirIdleri.Contains(cs.Id)))
                .ToDictionary(cs => cs.Id);

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var now = TurkeyTime.Now;
            var kullaniciId = _currentUserService.UserId ?? 0;
            int guncellenen = 0;
            var atlananlar = new List<string>();

            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                satirlar.Keys);
            var kaynakSatirIds = new HashSet<int>();

            foreach (var secim in secimler)
            {
                if (!satirlar.TryGetValue(secim.CekiSatiriId, out var satir))
                    continue;

                if (kilitliSatirIdleri.Contains(satir.Id))
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - {SandikSevkKilidiHelper.UrunKilitliMesaji}");
                    continue;
                }

                if (await SahaAktarimBlokajHelper.KaynakSatirAktarildiMiAsync(_sahaTamamlamaService, satir, cancellationToken))
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - {SahaAktarimBlokajHelper.UcKMesaji}");
                    continue;
                }

                // Grid İptal veya kapandı → atla
                if (satir.GridDurumuId == (int)GridDurum.Iptal)
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - Grid İptal");
                    continue;
                }
                if (satir.GridDurumuId == (int)GridDurum.GridKapandi)
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - Grid Kapandı");
                    continue;
                }
                if (satir.GridDurumuId == (int)GridDurum.TrafoSevk &&
                    (satir.GridSevkDurumuId != (int)GridSevkDurum.SevkEdildi || (satir.GridSevkMiktari ?? 0) <= 0))
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - Trafo sevk, 3K'ya sevk edilmis Grid gelen miktar yok");
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
                var seciliIcerikResult = await UcKSandikIcerikSenkronizasyonHelper.GetSeciliIcerikAsync(
                    _unitOfWork,
                    satir.Id,
                    secim.SandikIcerikId);
                if (!seciliIcerikResult.IsSuccess)
                {
                    atlananlar.Add($"#{satir.SiraNo} ({satir.Aciklama}) - {seciliIcerikResult.Error!.Message}");
                    continue;
                }

                var seciliIcerik = seciliIcerikResult.Value;
                var sandikKalan = seciliIcerik == null
                    ? Math.Max(satir.KalanMiktar, 0)
                    : Math.Max((seciliIcerik.TahsisMiktari > 0 ? seciliIcerik.TahsisMiktari : satir.IstenenAdet) - seciliIcerik.KonulanAdet, 0);
                if (sandikKalan <= 0)
                    continue;

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
                satir.TeslimTarihi = now;
                satir.UcKAciklama = request.Aciklama;

                // Genel durumu hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                // KURAL 2: Merkezi kalan hesaplaması ve durum override
                _durumHesaplaService.HesaplaKalanVeDurum(satir);

                repo.Update(satir);

                // SandikIcerik senkronizasyonu
                var senkronizasyonResult = await UcKSandikIcerikSenkronizasyonHelper.SenkronizeAsync(
                    _unitOfWork,
                    satir,
                    secim.SandikIcerikId);
                if (!senkronizasyonResult.IsSuccess)
                    return Result.Failure($"#{satir.SiraNo} ({satir.Aciklama}) - {senkronizasyonResult.Error!.Message}");

                var ilgiliIcerikler = senkronizasyonResult.Value ?? new List<SandikIcerik>();

                await SandikLokasyonHelper.VarsayilanUcKDepoLokasyonuAtaAsync(_unitOfWork, ilgiliIcerikler);

                if (satir.KaynakCekiSatiriId.HasValue)
                    kaynakSatirIds.Add(satir.KaynakCekiSatiriId.Value);

                guncellenen++;
            }

            if (guncellenen == 0)
                return Result.Failure("Hiçbir ürün güncellenemedi. Tümü Grid sevk/iptal kontrolünü geçemedi.");

            await _unitOfWork.SaveChangesAsync();

            if (kaynakSatirIds.Count > 0)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(kaynakSatirIds, cancellationToken);

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
