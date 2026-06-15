using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiSatirlariSilCommand : IRequest<Result<CekiSatirlariSilDto>>, ISecuredRequest
    {
        public List<int> CekiSatiriIds { get; set; } = new();
    }

    public class CekiSatirlariSilDto
    {
        public int SilinenSatirSayisi { get; set; }
        public int SilinenSandikSayisi { get; set; }
        public int SilinenCekiSayisi { get; set; }
        public int IadeEdilenStokHareketiSayisi { get; set; }
        public int PasifeAlinanTransferSayisi { get; set; }
    }

    public class CekiSatirlariSilCommandHandler : IRequestHandler<CekiSatirlariSilCommand, Result<CekiSatirlariSilDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;

        public CekiSatirlariSilCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
        }

        public async Task<Result<CekiSatirlariSilDto>> Handle(CekiSatirlariSilCommand request, CancellationToken cancellationToken)
        {
            var idler = request.CekiSatiriIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (idler.Count == 0)
                return Result<CekiSatirlariSilDto>.Failure("Silinecek çeki satırı seçilmedi.");

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = (await cekiSatiriRepo.FindAsync(s => idler.Contains(s.Id))).ToList();

            if (satirlar.Count != idler.Count)
                return Result<CekiSatirlariSilDto>.Failure("Çeki satırı bulunamadı.", 404);

            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(_unitOfWork, idler);
            if (kilitliSatirIdleri.Any())
                return Result<CekiSatirlariSilDto>.Failure(
                    $"Seçili satırlardan {kilitliSatirIdleri.Count} tanesi sevk edilmiş sandıkta olduğu için silinemez.");

            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiIdler = satirlar.Select(s => s.CekiId).Distinct().ToList();
            var cekiler = (await cekiRepo.FindAsync(c => cekiIdler.Contains(c.Id))).ToDictionary(c => c.Id);

            if (cekiler.Count != cekiIdler.Count)
                return Result<CekiSatirlariSilDto>.Failure("Çeki bulunamadı.", 404);

            var transferRepo = _unitOfWork.GetRepository<ProjeTransfer>();
            var transferler = (await transferRepo.FindAsync(t =>
                    idler.Contains(t.KaynakCekiSatiriId) ||
                    (t.HedefCekiSatiriId.HasValue && idler.Contains(t.HedefCekiSatiriId.Value))))
                .ToList();

            var aktifTransferler = transferler
                .Where(t => t.DurumId == (int)ProjeTransferDurum.Aktif)
                .ToList();

            var disariGidenTransferVar = aktifTransferler.Any(t =>
                idler.Contains(t.KaynakCekiSatiriId) &&
                (!t.HedefCekiSatiriId.HasValue || !idler.Contains(t.HedefCekiSatiriId.Value)));

            if (disariGidenTransferVar)
            {
                return Result<CekiSatirlariSilDto>.Failure(
                    "Seçili satırlardan bazıları başka bir proje/satıra kaynak olmuş. Önce hedef karşılamayı geri alın veya hedef satırları da seçin.");
            }

            var kaynakSatirIdler = aktifTransferler
                .Where(t => t.HedefCekiSatiriId.HasValue &&
                    idler.Contains(t.HedefCekiSatiriId.Value) &&
                    !idler.Contains(t.KaynakCekiSatiriId))
                .Select(t => t.KaynakCekiSatiriId)
                .Distinct()
                .ToList();

            var kaynakSatirlar = kaynakSatirIdler.Count == 0
                ? new Dictionary<int, CekiSatiri>()
                : (await cekiSatiriRepo.FindAsync(s => kaynakSatirIdler.Contains(s.Id))).ToDictionary(s => s.Id);

            var hedefiSecilenTransferler = aktifTransferler
                .Where(t => t.HedefCekiSatiriId.HasValue &&
                    idler.Contains(t.HedefCekiSatiriId.Value) &&
                    !idler.Contains(t.KaynakCekiSatiriId))
                .ToList();

            foreach (var transfer in hedefiSecilenTransferler)
            {
                if (kaynakSatirlar.TryGetValue(transfer.KaynakCekiSatiriId, out var kaynakSatir))
                {
                    kaynakSatir.ProjeGonderilen = Math.Max(kaynakSatir.ProjeGonderilen - transfer.Miktar, 0);
                    kaynakSatir.DurumId = _durumHesaplaService.HesaplaGenelDurum(kaynakSatir.GridDurumuId, kaynakSatir.UcKDurumuId);
                    _durumHesaplaService.HesaplaKalanVeDurum(kaynakSatir);
                    cekiSatiriRepo.Update(kaynakSatir);
                }

                transfer.DurumId = (int)ProjeTransferDurum.GeriAlindi;
                transfer.IptalTarihi = TurkeyTime.Now;
                transfer.IptalAciklama = "Hedef çeki satırı silindiği için transfer pasife alındı.";
                transfer.HedefCekiSatiriId = null;
                transferRepo.Update(transfer);
            }

            foreach (var transfer in transferler.Where(t => idler.Contains(t.KaynakCekiSatiriId)))
                transferRepo.Remove(transfer);

            var stokHareketRepo = _unitOfWork.GetRepository<StokHareketi>();
            var stokHareketleri = (await stokHareketRepo.FindAsync(h => idler.Contains(h.CekiSatiriId))).ToList();
            var stokGeriAlSonucu = await UcKStokHareketGeriAlHelper.GeriAlAsync(_unitOfWork, idler);
            if (!stokGeriAlSonucu.IsSuccess)
            {
                return Result<CekiSatirlariSilDto>.Failure(
                    stokGeriAlSonucu.Error?.Message ?? "Stok hareketi geri alınamadı.",
                    stokGeriAlSonucu.StatusCode);
            }

            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = (await icerikRepo.FindAsync(i =>
                    i.CekiSatiriId.HasValue && idler.Contains(i.CekiSatiriId.Value)))
                .ToList();

            var silinenIcerikIds = icerikler.Select(i => i.Id).ToHashSet();
            var etkilenenSandikIds = icerikler.Select(i => i.SandikId).Distinct().ToList();
            var silinecekSandikIds = await GetSilinecekSandikIdsAsync(etkilenenSandikIds, silinenIcerikIds, icerikRepo);
            await KalanSandikDurumlariniGuncelleAsync(
                etkilenenSandikIds,
                silinenIcerikIds,
                silinecekSandikIds,
                icerikRepo,
                cekiSatiriRepo);

            foreach (var icerik in icerikler)
                icerikRepo.Remove(icerik);

            var silinecekCekiIds = await GetSilinecekCekiIdsAsync(cekiIdler, idler, cekiSatiriRepo);
            var hareketRepo = _unitOfWork.GetRepository<HareketGecmisi>();
            await HareketleriEkleAsync(satirlar, cekiler, hareketRepo);

            foreach (var satir in satirlar)
                cekiSatiriRepo.Remove(satir);

            var silinenSandikSayisi = await BosSandiklariSilAsync(silinecekSandikIds);

            foreach (var cekiId in silinecekCekiIds)
            {
                if (cekiler.TryGetValue(cekiId, out var ceki))
                    cekiRepo.Remove(ceki);
            }

            await _unitOfWork.SaveChangesAsync();

            return Result<CekiSatirlariSilDto>.Success(new CekiSatirlariSilDto
            {
                SilinenSatirSayisi = satirlar.Count,
                SilinenSandikSayisi = silinenSandikSayisi,
                SilinenCekiSayisi = silinecekCekiIds.Count,
                IadeEdilenStokHareketiSayisi = stokHareketleri.Count,
                PasifeAlinanTransferSayisi = hedefiSecilenTransferler.Count
            });
        }

        private static async Task<List<int>> GetSilinecekSandikIdsAsync(
            List<int> etkilenenSandikIds,
            HashSet<int> silinenIcerikIds,
            IGenericRepository<SandikIcerik> icerikRepo)
        {
            if (etkilenenSandikIds.Count == 0)
                return new List<int>();

            var tumIcerikler = await icerikRepo.FindAsync(i => etkilenenSandikIds.Contains(i.SandikId));

            return etkilenenSandikIds
                .Where(sandikId => tumIcerikler
                    .Where(i => i.SandikId == sandikId)
                    .All(i => silinenIcerikIds.Contains(i.Id)))
                .ToList();
        }

        private static async Task<List<int>> GetSilinecekCekiIdsAsync(
            List<int> cekiIdler,
            List<int> silinenSatirIds,
            IGenericRepository<CekiSatiri> cekiSatiriRepo)
        {
            var tumSatirlar = await cekiSatiriRepo.FindAsync(s => cekiIdler.Contains(s.CekiId));
            var silinenSet = silinenSatirIds.ToHashSet();

            return cekiIdler
                .Where(cekiId => tumSatirlar
                    .Where(s => s.CekiId == cekiId)
                    .All(s => silinenSet.Contains(s.Id)))
                .ToList();
        }

        private async Task KalanSandikDurumlariniGuncelleAsync(
            List<int> etkilenenSandikIds,
            HashSet<int> silinenIcerikIds,
            List<int> silinecekSandikIds,
            IGenericRepository<SandikIcerik> icerikRepo,
            IGenericRepository<CekiSatiri> cekiSatiriRepo)
        {
            var kalanSandikIds = etkilenenSandikIds
                .Except(silinecekSandikIds)
                .ToList();

            if (kalanSandikIds.Count == 0)
                return;

            var kalanIcerikler = (await icerikRepo.FindAsync(i => kalanSandikIds.Contains(i.SandikId)))
                .Where(i => !silinenIcerikIds.Contains(i.Id))
                .ToList();

            var kalanSatirIds = kalanIcerikler
                .Where(i => i.CekiSatiriId.HasValue)
                .Select(i => i.CekiSatiriId!.Value)
                .Distinct()
                .ToList();

            var kalanSatirlar = kalanSatirIds.Count == 0
                ? new Dictionary<int, CekiSatiri>()
                : (await cekiSatiriRepo.FindAsync(s => kalanSatirIds.Contains(s.Id))).ToDictionary(s => s.Id);

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandiklar = await sandikRepo.FindAsync(s => kalanSandikIds.Contains(s.Id));

            foreach (var sandik in sandiklar)
            {
                if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                    continue;

                var sandikIcerikleri = kalanIcerikler
                    .Where(i => i.SandikId == sandik.Id)
                    .ToList();

                var satirIds = sandikIcerikleri
                    .Where(i => i.CekiSatiriId.HasValue)
                    .Select(i => i.CekiSatiriId!.Value)
                    .ToList();

                var hepsiTamamlandi = satirIds.Count > 0 &&
                    satirIds.All(id => kalanSatirlar.TryGetValue(id, out var satir) &&
                        satir.DurumId == (int)UrunDurum.Tamamlandi);
                var enAzBiriKonuldu = sandikIcerikleri.Any(i => i.KonulanAdet > 0);

                var yeniDurumId = hepsiTamamlandi
                    ? (int)SandikDurum.Kapandi
                    : enAzBiriKonuldu
                        ? (int)SandikDurum.Hazirlaniyor
                        : (int)SandikDurum.Bos;

                if (sandik.DurumId == yeniDurumId)
                    continue;

                sandik.DurumId = yeniDurumId;
                sandikRepo.Update(sandik);
            }
        }

        private async Task HareketleriEkleAsync(
            List<CekiSatiri> satirlar,
            Dictionary<int, Ceki> cekiler,
            IGenericRepository<HareketGecmisi> hareketRepo)
        {
            var kullaniciId = _currentUserService.UserId ?? 0;

            foreach (var satir in satirlar)
            {
                if (!cekiler.TryGetValue(satir.CekiId, out var ceki))
                    continue;

                await hareketRepo.AddAsync(new HareketGecmisi
                {
                    ProjeId = ceki.ProjeId,
                    KullaniciId = kullaniciId,
                    ReferansTipi = "CekiSatiri",
                    ReferansId = satir.Id.ToString(),
                    ReferansMetni = $"Poz: {satir.OlcuResmiPozNo ?? satir.BarkodNo} - {satir.Aciklama}",
                    Islem = "Çeki Satırı Silindi",
                    Aciklama = $"Revizyon nedeniyle çeki satırı silindi. Sıra: {satir.SiraNo}, Barkod: {satir.BarkodNo}, Sandık: {satir.CekideGecenSandikNo}"
                });
            }
        }

        private async Task<int> BosSandiklariSilAsync(List<int> silinecekSandikIds)
        {
            if (silinecekSandikIds.Count == 0)
                return 0;

            var sevkiyatSandikRepo = _unitOfWork.GetRepository<SevkiyatSandik>();
            var sevkiyatSandiklari = await sevkiyatSandikRepo.FindAsync(ss => silinecekSandikIds.Contains(ss.SandikId));
            foreach (var sevkiyatSandik in sevkiyatSandiklari)
                sevkiyatSandikRepo.Remove(sevkiyatSandik);

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandiklar = await sandikRepo.FindAsync(s => silinecekSandikIds.Contains(s.Id));
            var silinen = 0;

            foreach (var sandik in sandiklar)
            {
                sandikRepo.Remove(sandik);
                silinen++;
            }

            return silinen;
        }
    }
}
