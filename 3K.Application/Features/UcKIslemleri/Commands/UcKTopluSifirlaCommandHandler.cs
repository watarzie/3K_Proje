using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    /// <summary>
    /// Seçili ürünlerin 3K durumlarını toplu sıfırlar — tekli sıfırlama mantığı toplu uygulanır.
    /// </summary>
    public class UcKTopluSifirlaCommandHandler : IRequestHandler<UcKTopluSifirlaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public UcKTopluSifirlaCommandHandler(
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

        public async Task<Result> Handle(UcKTopluSifirlaCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(
                    async transactionCancellationToken =>
                    {
                        var result = await HandleInTransactionAsync(request, transactionCancellationToken);
                        if (!result.IsSuccess)
                            throw new UcKTopluSifirlamaRollbackException(result);

                        return result;
                    },
                    cancellationToken);
            }
            catch (UcKTopluSifirlamaRollbackException exception)
            {
                return exception.Result;
            }
        }

        private async Task<Result> HandleInTransactionAsync(
            UcKTopluSifirlaCommand request,
            CancellationToken cancellationToken)
        {
            var secimler = UcKSandikSecimHelper.Olustur(request.CekiSatiriIdler, request.Secimler);
            if (!secimler.Any())
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var transferRepo = _unitOfWork.GetRepository<ProjeTransfer>();
            var seciliSatirIdleri = secimler.Select(s => s.CekiSatiriId).Distinct().ToList();
            var satirlar = (await repo.FindAsync(cs => seciliSatirIdleri.Contains(cs.Id)))
                .ToDictionary(cs => cs.Id);

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var kullaniciId = _currentUserService.UserId ?? 0;
            int basarili = 0;
            var hatalar = new List<string>();
            var kaynakSatirIds = new HashSet<int>();
            var tamamenGeriAlinanSatirIds = new HashSet<int>();
            var sandikBazliGeriAlinanlar = new Dictionary<int, HashSet<int>>();
            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                satirlar.Keys);

            var sahayaAktarilanSatirIdleri = await SahaAktarimBlokajHelper.GetAktarilanKaynakSatirIdleriAsync(
                _sahaTamamlamaService,
                satirlar.Values,
                cancellationToken);

            if (sahayaAktarilanSatirIdleri.Any())
                return Result.Failure($"Seçili ürünlerden {sahayaAktarilanSatirIdleri.Count} tanesi sahaya aktarıldığı için normal proje üzerinden 3K geri alma işlemi yapılamaz.");

            foreach (var secim in secimler)
            {
                if (!satirlar.TryGetValue(secim.CekiSatiriId, out var satir))
                    continue;

                if (kilitliSatirIdleri.Contains(satir.Id))
                {
                    hatalar.Add($"#{satir.SiraNo}: {SandikSevkKilidiHelper.UrunKilitliMesaji}");
                    continue;
                }

                // Grid İptal blokajı
                if (satir.GridDurumuId == (int)GridDurum.Iptal)
                {
                    hatalar.Add($"#{satir.SiraNo}: Grid İptal.");
                    continue;
                }

                // Zaten başlangıç durumundaysa atla
                if (UcKDurumSifirlamaHelper.TamamenBaslangicDurumunda(satir))
                {
                    continue; // Zaten sıfır, atla
                }

                var seciliIcerikResult = await UcKSandikIcerikSenkronizasyonHelper.GetSeciliIcerikAsync(
                    _unitOfWork,
                    satir.Id,
                    secim.SandikIcerikId);
                if (!seciliIcerikResult.IsSuccess)
                {
                    hatalar.Add($"#{satir.SiraNo}: {seciliIcerikResult.Error!.Message}");
                    continue;
                }

                var seciliIcerik = seciliIcerikResult.Value;
                if (seciliIcerik != null && (seciliIcerik.StokKarsilanan > 0 || seciliIcerik.ProjeKarsilanan > 0))
                {
                    hatalar.Add($"#{satir.SiraNo}: Stoktan veya projeden karşılanan sandık parçası toplu olarak tek başına sıfırlanamaz.");
                    continue;
                }

                var aktifTransferler = (await transferRepo.FindAsync(t =>
                    t.DurumId == (int)ProjeTransferDurum.Aktif &&
                    (t.KaynakCekiSatiriId == satir.Id || t.HedefCekiSatiriId == satir.Id)))
                    .Where(t => t.DurumId == (int)ProjeTransferDurum.Aktif)
                    .ToList();
                var aktifGidenTransferler = aktifTransferler
                    .Where(t => t.KaynakCekiSatiriId == satir.Id)
                    .ToList();
                if (aktifGidenTransferler.Any())
                {
                    hatalar.Add($"#{satir.SiraNo}: Bu ürün başka projeye kaynak olarak verilmiş. Önce hedef projedeki karşılamayı geri alın.");
                    continue;
                }

                var aktifGelenTransferler = aktifTransferler
                    .Where(t => t.HedefCekiSatiriId == satir.Id)
                    .ToList();

                var eskiDurum = satir.UcKDurumuId;
                var eskiKaliteDurumId = satir.KaliteDurumId;
                var eskiSurecDurumId = satir.SurecDurumId;

                if (seciliIcerik == null)
                {
                    var stokGeriAlSonucu = await UcKStokHareketGeriAlHelper.GeriAlAsync(_unitOfWork, satir.Id);
                    if (!stokGeriAlSonucu.IsSuccess)
                    {
                        hatalar.Add($"#{satir.SiraNo}: {stokGeriAlSonucu.Error?.Message ?? "Stok hareketi geri alınamadı."}");
                        continue;
                    }
                }

                // 3K alanlarını sıfırla
                if (seciliIcerik == null)
                {
                    satir.UcKDurumuId = (int)UcKDurum.Bekliyor;
                    satir.UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor;
                    satir.GelenMiktar = 0;
                    satir.KarsilananMiktar = 0;
                    satir.StokKarsilanan = 0;
                    satir.ProjeKarsilanan = 0;
                    satir.ProjeGonderilen = 0;
                    satir.TedarikciKarsilanan = 0;
                    satir.HataliMiktar = 0;
                    satir.GeriGonderilenMiktar = 0;
                    satir.TeslimTarihi = null;
                    satir.UcKAciklama = null;
                    satir.KaynakHedefProjeNo = null;
                    satir.KaynakProjeId = null;
                    satir.GeriGonderilmeSebebiId = null;
                    satir.YenidenSevkGerekliAdet = 0;
                }
                else
                {
                    var sandikUcKGelen = Math.Max(
                        seciliIcerik.KonulanAdet - seciliIcerik.StokKarsilanan - seciliIcerik.ProjeKarsilanan - seciliIcerik.TedarikciKarsilanan,
                        0);
                    satir.GelenMiktar = Math.Max(satir.GelenMiktar - sandikUcKGelen, 0);
                    satir.TedarikciKarsilanan = Math.Max(satir.TedarikciKarsilanan - seciliIcerik.TedarikciKarsilanan, 0);
                    satir.KarsilananMiktar = satir.StokKarsilanan + satir.ProjeKarsilanan + satir.TedarikciKarsilanan;
                    var kalanTamamlanan = satir.GelenMiktar + satir.KarsilananMiktar - satir.ProjeGonderilen;
                    satir.UcKDurumuId = kalanTamamlanan > 0 ? (int)UcKDurum.EksikGeldi : (int)UcKDurum.Bekliyor;
                    satir.UcKKarsilamaTipiId = satir.UcKDurumuId;
                    if (kalanTamamlanan <= 0)
                    {
                        satir.TeslimTarihi = null;
                        satir.UcKAciklama = null;
                    }
                }
                if (satir.GridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli)
                    satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;

                UcKDurumSifirlamaHelper.KaliteVeSureciSifirlaEgerBaslangicta(satir);

                // Genel durumu hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                _durumHesaplaService.HesaplaKalanVeDurum(satir);

                repo.Update(satir);

                foreach (var transfer in seciliIcerik == null ? aktifGelenTransferler : Enumerable.Empty<ProjeTransfer>())
                {
                    var kaynakSatir = await repo.GetByIdAsync(transfer.KaynakCekiSatiriId);
                    if (kaynakSatir != null)
                    {
                        kaynakSatir.ProjeGonderilen = Math.Max(kaynakSatir.ProjeGonderilen - transfer.Miktar, 0);
                        kaynakSatir.DurumId = _durumHesaplaService.HesaplaGenelDurum(kaynakSatir.GridDurumuId, kaynakSatir.UcKDurumuId);
                        _durumHesaplaService.HesaplaKalanVeDurum(kaynakSatir);
                        repo.Update(kaynakSatir);
                    }

                    transfer.DurumId = (int)ProjeTransferDurum.GeriAlindi;
                    transfer.IptalTarihi = TurkeyTime.Now;
                    transfer.IptalAciklama = "3K durumu geri alındığı için transfer pasife çekildi.";
                    transferRepo.Update(transfer);
                }

                // SandıkIçerik senkronizasyonu
                var senkronizasyonResult = await UcKSandikIcerikSenkronizasyonHelper.SenkronizeAsync(
                    _unitOfWork,
                    satir,
                    secim.SandikIcerikId);
                if (!senkronizasyonResult.IsSuccess)
                    return Result.Failure($"#{satir.SiraNo}: {senkronizasyonResult.Error!.Message}");

                if (satir.KaynakCekiSatiriId.HasValue)
                    kaynakSatirIds.Add(satir.KaynakCekiSatiriId.Value);

                if (seciliIcerik == null)
                {
                    tamamenGeriAlinanSatirIds.Add(satir.Id);
                }
                else
                {
                    if (!sandikBazliGeriAlinanlar.TryGetValue(satir.Id, out var sandikIds))
                    {
                        sandikIds = new HashSet<int>();
                        sandikBazliGeriAlinanlar[satir.Id] = sandikIds;
                    }

                    sandikIds.Add(seciliIcerik.SandikId);
                }

                basarili++;

                // Hareket kaydı
                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    KullaniciId = kullaniciId,
                    ReferansTipi = "CekiSatiri",
                    ReferansId = satir.Id.ToString(),
                    Islem = "3K Toplu Sıfırlandı",
                    IslemTipiId = (int)IslemTipi.UcKDurumSifirlandi,
                    EskiDeger = $"UcKDurum:{eskiDurum}, KaliteDurum:{eskiKaliteDurumId?.ToString() ?? "null"}, SurecDurum:{eskiSurecDurumId?.ToString() ?? "null"}",
                    YeniDeger = "Bekliyor (Sıfırlandı)",
                    Aciklama = $"Toplu 3K sıfırlama — KaliteDurum:{eskiKaliteDurumId?.ToString() ?? "null"}→{satir.KaliteDurumId?.ToString() ?? "null"}, SurecDurum:{eskiSurecDurumId?.ToString() ?? "null"}→{satir.SurecDurumId?.ToString() ?? "null"}. {(string.IsNullOrWhiteSpace(request.Aciklama) ? "Açıklama yok" : request.Aciklama)}"
                });
            }

            if (basarili == 0)
                return Result.Failure("Hiçbir ürün sıfırlanamadı. " + (hatalar.Any() ? string.Join("; ", hatalar.Take(3)) : ""));

            await SandikDurumSenkronizasyonHelper.IslemGeriAlindigindaSandiklariYenidenAcAsync(
                _unitOfWork,
                tamamenGeriAlinanSatirIds);

            foreach (var (satirId, sandikIds) in sandikBazliGeriAlinanlar)
            {
                await SandikDurumSenkronizasyonHelper.IslemGeriAlindigindaSandiklariYenidenAcAsync(
                    _unitOfWork,
                    new[] { satirId },
                    sandikIds);
            }

            await _unitOfWork.SaveChangesAsync();

            if (kaynakSatirIds.Count > 0)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(kaynakSatirIds, cancellationToken);

            if (hatalar.Any())
                return Result.Success();

            return Result.Success();
        }

        private sealed class UcKTopluSifirlamaRollbackException : Exception
        {
            public UcKTopluSifirlamaRollbackException(Result result)
                : base(result.Error?.Message ?? "Toplu 3K geri alma islemi geri alindi.")
            {
                Result = result;
            }

            public Result Result { get; }
        }
    }
}
