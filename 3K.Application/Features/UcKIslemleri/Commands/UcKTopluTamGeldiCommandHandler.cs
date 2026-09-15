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
        private readonly ILookupCacheService _lookupCache;

        public UcKTopluTamGeldiCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService,
            ISahaTamamlamaService sahaTamamlamaService,
            ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
            _sahaTamamlamaService = sahaTamamlamaService;
            _lookupCache = lookupCache;
        }

        public async Task<Result> Handle(UcKTopluTamGeldiCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionCancellationToken => HandleInTransactionAsync(request, transactionCancellationToken),
                cancellationToken);
        }

        private async Task<Result> HandleInTransactionAsync(
            UcKTopluTamGeldiCommand request,
            CancellationToken cancellationToken)
        {
            var secimler = UcKSandikSecimHelper.Olustur(request.CekiSatiriIdler, request.Secimler);
            if (!secimler.Any())
                return Result.Failure("En az bir ürün seçilmelidir.");

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var seciliSatirIdleri = secimler.Select(s => s.CekiSatiriId).Distinct().ToList();
            // Aynı parent için birden fazla sandık seçilebildiğinden tüm child kayıtlar
            // tek tracked sorguda yüklenir. Böylece detached kopyaların sırayla attach
            // edilmesi ve aktif-parti sayaçlarının yalnız bir child'da kalması önlenir.
            var iceriklerBySatirId = _unitOfWork.GetRepository<SandikIcerik>()
                .Queryable()
                .Where(i => i.CekiSatiriId.HasValue && seciliSatirIdleri.Contains(i.CekiSatiriId.Value))
                .ToList()
                .GroupBy(i => i.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<SandikIcerik>)g.ToList());
            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                secimler.Select(s => s.CekiSatiriId));
            var basarili = 0;
            var hatalar = new List<string>();
            var telafiConflictVar = false;
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

                if (satir.KaliteDurumId.HasValue &&
                    _lookupCache.GetDeger<LookupKaliteDurum>(satir.KaliteDurumId.Value) == "Tadilatta")
                { hatalar.Add($"ID {cekiSatiriId}: Kalite Tadilatta."); continue; }

                if (satir.GridDurumuId == (int)GridDurum.TrafoSevk &&
                    !GridUcKSevkPartisiKurali.AktifPartiTeslimeAcikMi(satir))
                { hatalar.Add($"ID {cekiSatiriId}: Trafo sevk satirinda 3K'ya sevk edilmis Grid gelen miktar yok."); continue; }

                // Grid sevk kontrolü
                if (!GridUcKSevkPartisiKurali.AktifPartiTeslimeAcikMi(satir))
                { hatalar.Add($"ID {cekiSatiriId}: Grid henüz sevk etmedi."); continue; }

                var projeTransferTelafiPaketi = false;
                var satirIcerikleri = iceriklerBySatirId.GetValueOrDefault(satir.Id)
                    ?? Array.Empty<SandikIcerik>();
                if (UcKProjeTransferTelafiTeslimKural.AdayMi(satir))
                {
                    projeTransferTelafiPaketi =
                        UcKProjeTransferTelafiTeslimKural.AktifMi(satir, satirIcerikleri.Count);
                }

                var seciliIcerik = secim.SandikIcerikId.HasValue
                    ? satirIcerikleri.FirstOrDefault(i => i.Id == secim.SandikIcerikId.Value)
                    : null;
                if (secim.SandikIcerikId.HasValue && seciliIcerik == null)
                { hatalar.Add($"ID {cekiSatiriId}: Seçilen sandık içeriği bu ürüne ait değil."); continue; }
                var sandikMiktari = seciliIcerik == null
                    ? satir.IstenenAdet
                    : seciliIcerik.TahsisMiktari > 0 ? seciliIcerik.TahsisMiktari : satir.IstenenAdet;
                var sandikKalan = seciliIcerik == null
                    ? Math.Max(satir.KalanMiktar, 0)
                    : Math.Max(sandikMiktari - seciliIcerik.KonulanAdet, 0);

                // Normal akışta dolu tahsisi atla. Proje transferi telafi paketinde ise
                // sandık mevcudu yeni paketin öncesinden kalmış olabileceği için işleme devam et.
                if (!UcKProjeTransferTelafiTeslimKural.TeslimIslemiGerekliMi(
                        projeTransferTelafiPaketi,
                        sandikKalan))
                    continue;

                var eskiDurum = satir.UcKKarsilamaTipiId;

                // TamGeldi mantığı — mevcut tek handler ile aynı
                var teslimMiktariResult = GridUcKSevkPartisiKurali.TamKarsilamaMiktariniHesapla(
                    _unitOfWork,
                    satir,
                    seciliIcerik,
                    sandikKalan,
                    satirIcerikleri);
                if (!teslimMiktariResult.IsSuccess)
                {
                    hatalar.Add($"ID {cekiSatiriId}: {teslimMiktariResult.Error!.Message}");
                    continue;
                }

                var sevkMiktari = teslimMiktariResult.Value;
                var telafiTeslimResult = UcKProjeTransferTelafiTeslimKural.TeslimMiktariniHesapla(
                    projeTransferTelafiPaketi,
                    sevkMiktari,
                    satir);
                if (!telafiTeslimResult.IsSuccess)
                {
                    telafiConflictVar = true;
                    hatalar.Add($"ID {cekiSatiriId}: {telafiTeslimResult.Error!.Message}");
                    continue;
                }

                sevkMiktari = telafiTeslimResult.Value;
                var aktifPartiKayitResult = GridUcKSevkPartisiKurali.AktifPartiKarsilamasiniKaydet(
                    _unitOfWork,
                    satir,
                    seciliIcerik,
                    Math.Max(sevkMiktari, 0),
                    projeTransferTelafiPaketi,
                    satirIcerikleri);
                if (!aktifPartiKayitResult.IsSuccess)
                {
                    hatalar.Add($"ID {cekiSatiriId}: {aktifPartiKayitResult.Error!.Message}");
                    continue;
                }

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
                return Result.Failure(
                    $"{basarili} ürün güncellendi, {hatalar.Count} hata: {string.Join("; ", hatalar.Take(3))}",
                    telafiConflictVar ? 409 : 400);

            return Result.Success();
        }
    }
}
