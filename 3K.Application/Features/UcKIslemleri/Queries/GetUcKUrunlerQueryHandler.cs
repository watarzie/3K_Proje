using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Common.DTOs;
using _3K.Application.Features.UcKIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Queries
{
    public class GetUcKUrunlerQueryHandler : IRequestHandler<GetUcKUrunlerQuery, Result<List<UcKUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public GetUcKUrunlerQueryHandler(
            IUnitOfWork unitOfWork,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result<List<UcKUrunDto>>> Handle(GetUcKUrunlerQuery request, CancellationToken cancellationToken)
        {
            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result<List<UcKUrunDto>>.Failure("Proje bulunamadı.", 404);

            var projeSandiklari = (await _unitOfWork.GetRepository<Sandik>()
                    .FindAsync(s => s.ProjeId == request.ProjeId))
                .ToList();
            var sandikMap = projeSandiklari.ToDictionary(s => s.Id);
            var sandikIdler = projeSandiklari.Select(s => s.Id).ToList();
            // Tek sorgu: CekiSatirlari → Ceki.ProjeId filtresi ile direkt erişim (AsNoTracking GenericRepository'de)
            var satirlar = (await _unitOfWork.GetRepository<CekiSatiri>()
                .FindAsync(cs => cs.Ceki.ProjeId == request.ProjeId))
                .OrderBy(cs => cs.SiraNo)
                .ToList();

            var satirIdler = satirlar.Select(s => s.Id).ToList();
            var sandikIcerikleri = (await _unitOfWork.GetRepository<SandikIcerik>()
                    .FindAsync(i => i.CekiSatiriId.HasValue && satirIdler.Contains(i.CekiSatiriId.Value)))
                .ToList();
            var manuelSahaIcerikleri = proje.ProjeTipiId == (int)ProjeTipi.Saha && sandikIdler.Any()
                ? (await _unitOfWork.GetRepository<SandikIcerik>()
                    .FindAsync(i => !i.CekiSatiriId.HasValue && sandikIdler.Contains(i.SandikId)))
                    .OrderBy(i => sandikMap.GetValueOrDefault(i.SandikId)?.SandikNo)
                    .ThenBy(i => i.Id)
                    .ToList()
                : new List<SandikIcerik>();

            if (!satirlar.Any() && !manuelSahaIcerikleri.Any())
                return Result<List<UcKUrunDto>>.Failure("Bu projeye ait ürün bulunamadı.", 404);

            var sahaTamamlamaMap = proje.ProjeTipiId == (int)ProjeTipi.Normal
                ? await _sahaTamamlamaService.GetAktifGerceklesenTamamlamaMapAsync(
                    satirlar.Where(s => !s.KaynakCekiSatiriId.HasValue).Select(s => s.Id),
                    cancellationToken)
                : new Dictionary<int, decimal>();
            var sahaTamamlamaIzMap = proje.ProjeTipiId == (int)ProjeTipi.Normal
                ? GetSahaTamamlamaIzMap(satirlar.Where(s => !s.KaynakCekiSatiriId.HasValue).Select(s => s.Id).ToList())
                : new Dictionary<int, List<SahaTamamlamaIzDto>>();
            var sahaKaynakIzMap = proje.ProjeTipiId == (int)ProjeTipi.Saha
                ? GetSahaKaynakIzMap(satirlar.Where(s => s.KaynakCekiSatiriId.HasValue).Select(s => s.KaynakCekiSatiriId!.Value).ToList())
                : new Dictionary<int, SahaKaynakSatirIzDto>();

            var sandikNoMap = projeSandiklari
                .Where(s => !string.IsNullOrWhiteSpace(s.SandikNo))
                .GroupBy(s => s.SandikNo.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var sandikIcerikleriBySatirId = sandikIcerikleri
                .Where(i => i.CekiSatiriId.HasValue)
                .GroupBy(i => i.CekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var transferler = satirIdler.Any()
                ? (await _unitOfWork.GetRepository<ProjeTransfer>()
                    .FindAsync(t => t.DurumId == (int)ProjeTransferDurum.Aktif &&
                        (satirIdler.Contains(t.KaynakCekiSatiriId) ||
                         (t.HedefCekiSatiriId.HasValue && satirIdler.Contains(t.HedefCekiSatiriId.Value)))))
                    .ToList()
                : new List<ProjeTransfer>();

            var transferProjeIdler = transferler
                .SelectMany(t => new[] { t.KaynakProjeId, t.HedefProjeId })
                .Distinct()
                .ToList();
            var transferProjeler = transferProjeIdler.Any()
                ? await _unitOfWork.GetRepository<Proje>().FindAsync(p => transferProjeIdler.Contains(p.Id))
                : Enumerable.Empty<Proje>();
            var projeNoMap = transferProjeler.ToDictionary(p => p.Id, p => p.ProjeNo);

            var result = satirlar
                .Select(cs =>
                {
                    var etkinKalan = CekiSatiriKalanHelper.HesaplaEtkinKalan(cs, sahaTamamlamaMap);
                    var gelenTransferler = transferler.Where(t => t.HedefCekiSatiriId == cs.Id).ToList();
                    var gidenTransferler = transferler.Where(t => t.KaynakCekiSatiriId == cs.Id).ToList();
                    var projeKarsilanan = gelenTransferler.Any() ? gelenTransferler.Sum(t => t.Miktar) : cs.ProjeKarsilanan;
                    var projeGonderilen = gidenTransferler.Any() ? gidenTransferler.Sum(t => t.Miktar) : cs.ProjeGonderilen;
                    var gorunenUcKGelen = Math.Max(cs.GelenMiktar - projeGonderilen, 0);
                    var netKullanilabilir = Math.Max(cs.GelenMiktar + projeKarsilanan - projeGonderilen, 0);
                    var transferZinciri = gelenTransferler
                        .Select(t => MapTransferZincir("Gelen", t, projeNoMap))
                        .Concat(gidenTransferler.Select(t => MapTransferZincir("Giden", t, projeNoMap)))
                        .OrderBy(t => t.ZincirSeviyesi)
                        .ThenBy(t => t.Tarih)
                        .ToList();
                    var sandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo ?? string.Empty;
                    var satirIcerikleri = sandikIcerikleriBySatirId.GetValueOrDefault(cs.Id) ?? new List<SandikIcerik>();
                    var sandik = satirIcerikleri
                        .Select(i => sandikMap.GetValueOrDefault(i.SandikId))
                        .Where(s => s != null)
                        .Select(s => s!)
                        .FirstOrDefault(s => string.Equals(s.SandikNo, sandikNo, StringComparison.OrdinalIgnoreCase))
                        ?? satirIcerikleri
                            .Select(i => sandikMap.GetValueOrDefault(i.SandikId))
                            .FirstOrDefault(s => s != null)
                        ?? (sandikNoMap.TryGetValue(sandikNo.Trim(), out var sandikByNo) ? sandikByNo : null);
                    var gorunenSandikNo = sandik?.SandikNo ?? sandikNo;
                    var kaynakIz = cs.KaynakCekiSatiriId.HasValue
                        ? sahaKaynakIzMap.GetValueOrDefault(cs.KaynakCekiSatiriId.Value)
                        : null;
                    var sahaTamamlamalari = sahaTamamlamaIzMap.GetValueOrDefault(cs.Id) ?? new List<SahaTamamlamaIzDto>();

                    return new UcKUrunDto
                    {
                        CekiSatiriId = cs.Id,
                        KaynakCekiSatiriId = cs.KaynakCekiSatiriId,
                        KaynakProjeNo = kaynakIz?.KaynakProjeNo,
                        KaynakSandikNo = kaynakIz?.KaynakSandikNo,
                        KaynakSiraNo = kaynakIz?.KaynakSiraNo,
                        SahaTamamlamalari = sahaTamamlamalari,
                        SahaAktarildiMi = sahaTamamlamalari.Any(),
                        SahaAktarilanMiktar = sahaTamamlamalari.Sum(i => i.Miktar),
                        SiraNo = cs.SiraNo,
                        BarkodNo = cs.BarkodNo,
                        OlcuResmiPozNo = cs.OlcuResmiPozNo,
                        Aciklama = cs.Aciklama,
                        SandikNo = gorunenSandikNo,
                        SandikDurumId = sandik?.DurumId,
                        SandikDurumMetni = sandik != null ? _lookupCache.GetDeger<LookupSandikDurum>(sandik.DurumId) : null,
                        SandikSevkEdildiMi = sandik != null && SandikSevkKilidiHelper.SandikKilitliMi(sandik),
                        IstenenAdet = cs.IstenenAdet,
                        BirimId = cs.BirimId,
                        Birim = ((Birim)cs.BirimId).ToString(),
                        GridDurumuId = cs.GridDurumuId,
                        GridDurumuMetni = _lookupCache.GetDeger<LookupGridDurum>(cs.GridDurumuId),
                        GridGelenAdet = cs.GridGelenAdet,
                        TrafoSevkAdet = cs.TrafoSevkAdet,
                        GridSevkDurumuId = cs.GridSevkDurumuId,
                        GridSevkDurumuMetni = _lookupCache.GetDeger<LookupGridSevkDurum>(cs.GridSevkDurumuId),
                        GridSevkMiktari = cs.GridSevkMiktari,
                        UcKKarsilamaTipiId = cs.UcKKarsilamaTipiId,
                        UcKKarsilamaTipiMetni = _lookupCache.GetDeger<LookupUcKDurum>(cs.UcKKarsilamaTipiId),
                        GelenMiktar = gorunenUcKGelen,
                        KarsilananMiktar = cs.KarsilananMiktar,
                        HataliMiktar = cs.HataliMiktar,
                        KaynakHedefProjeNo = cs.KaynakHedefProjeNo,
                        GeriGonderilmeSebebiId = cs.GeriGonderilmeSebebiId,
                        GeriGonderilmeSebebiMetni = cs.GeriGonderilmeSebebiId.HasValue
                            ? _lookupCache.GetDeger<LookupGeriGonderilmeSebebi>(cs.GeriGonderilmeSebebiId.Value)
                            : null,
                        GeriGonderilenMiktar = cs.GeriGonderilenMiktar,
                        UcKAciklama = cs.UcKAciklama,
                        GridAciklama = cs.GridAciklama,
                        Kalan = etkinKalan,
                        KontrolUyari = HesaplaKontrolUyari(cs),
                        GenelDurumId = cs.DurumId,
                        GenelDurumMetni = _lookupCache.GetDeger<LookupUrunDurum>(cs.DurumId),
                        // Madde 2: Parçalı karşılama
                        StokKarsilanan = cs.StokKarsilanan,
                        ProjeKarsilanan = projeKarsilanan,
                        ProjeGonderilen = projeGonderilen,
                        NetKullanilabilir = netKullanilabilir,
                        TransferZinciriVar = transferZinciri.Any(),
                        TransferZinciri = transferZinciri,
                        TedarikciKarsilanan = cs.TedarikciKarsilanan,
                        EksikMiktar = etkinKalan,
                        // Kalite & Süreç
                        KaliteDurumId = cs.KaliteDurumId,
                        KaliteDurumMetni = cs.KaliteDurumId.HasValue ? _lookupCache.GetDeger<LookupKaliteDurum>(cs.KaliteDurumId.Value) : null,
                        SurecDurumId = cs.SurecDurumId,
                        SurecDurumMetni = cs.SurecDurumId.HasValue ? _lookupCache.GetDeger<LookupSurecDurum>(cs.SurecDurumId.Value) : null,
                        IsManuelEklenen = cs.IsManuelEklenen
                    };
                })
                .ToList();

            var manuelSiraNo = satirlar.Any() ? satirlar.Max(s => s.SiraNo) : 0;
            result.AddRange(manuelSahaIcerikleri.Select((icerik, index) =>
                MapSahaManuelIcerik(icerik, sandikMap.GetValueOrDefault(icerik.SandikId), manuelSiraNo + index + 1)));

            return Result<List<UcKUrunDto>>.Success(result);
        }

        private UcKUrunDto MapSahaManuelIcerik(SandikIcerik icerik, Sandik? sandik, int siraNo)
        {
            var birimId = icerik.BirimId ?? (int)Birim.Adet;
            var miktar = icerik.Miktar > 0 ? icerik.Miktar : icerik.KonulanAdet;
            var gorunenAd = !string.IsNullOrWhiteSpace(icerik.Isim)
                ? icerik.Isim!
                : icerik.Aciklama ?? string.Empty;

            return new UcKUrunDto
            {
                CekiSatiriId = -icerik.Id,
                SandikIcerikId = icerik.Id,
                IsSahaManuelSandikIcerigi = true,
                SiraNo = siraNo,
                BarkodNo = icerik.BarkodNo ?? string.Empty,
                OlcuResmiPozNo = null,
                Aciklama = gorunenAd,
                SandikNo = sandik?.SandikNo ?? string.Empty,
                SandikDurumId = sandik?.DurumId,
                SandikDurumMetni = sandik != null ? _lookupCache.GetDeger<LookupSandikDurum>(sandik.DurumId) : null,
                SandikSevkEdildiMi = sandik != null && SandikSevkKilidiHelper.SandikKilitliMi(sandik),
                IstenenAdet = miktar,
                BirimId = birimId,
                Birim = ((Birim)birimId).ToString(),
                GridDurumuId = (int)GridDurum.TamGeldi,
                GridDurumuMetni = _lookupCache.GetDeger<LookupGridDurum>((int)GridDurum.TamGeldi),
                GridGelenAdet = miktar,
                TrafoSevkAdet = 0,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
                GridSevkDurumuMetni = _lookupCache.GetDeger<LookupGridSevkDurum>((int)GridSevkDurum.SevkEdildi),
                GridSevkMiktari = miktar,
                UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi,
                UcKKarsilamaTipiMetni = _lookupCache.GetDeger<LookupUcKDurum>((int)UcKDurum.TamGeldi),
                GelenMiktar = miktar,
                KarsilananMiktar = miktar,
                HataliMiktar = 0,
                KaynakHedefProjeNo = icerik.KaynakProjeNo,
                GeriGonderilenMiktar = 0,
                UcKAciklama = icerik.Aciklama,
                GridAciklama = null,
                Kalan = 0,
                KontrolUyari = "MANUEL SAHA ÜRÜNÜ",
                GenelDurumId = (int)UrunDurum.Tamamlandi,
                GenelDurumMetni = _lookupCache.GetDeger<LookupUrunDurum>((int)UrunDurum.Tamamlandi),
                StokKarsilanan = 0,
                ProjeKarsilanan = 0,
                ProjeGonderilen = 0,
                NetKullanilabilir = miktar,
                TedarikciKarsilanan = 0,
                EksikMiktar = 0,
                IsManuelEklenen = true
            };
        }

        private static ProjeTransferZincirDto MapTransferZincir(string yon, ProjeTransfer transfer, Dictionary<int, string> projeNoMap)
        {
            return new ProjeTransferZincirDto
            {
                Id = transfer.Id,
                Yon = yon,
                KaynakProjeNo = projeNoMap.TryGetValue(transfer.KaynakProjeId, out var kaynakProjeNo) ? kaynakProjeNo : transfer.KaynakProjeId.ToString(),
                HedefProjeNo = projeNoMap.TryGetValue(transfer.HedefProjeId, out var hedefProjeNo) ? hedefProjeNo : transfer.HedefProjeId.ToString(),
                BarkodNo = transfer.BarkodNo,
                UrunAdi = transfer.UrunAdi,
                Miktar = transfer.Miktar,
                TransferTipi = ((ProjeTransferTipi)transfer.TransferTipiId).ToString(),
                Durum = ((ProjeTransferDurum)transfer.DurumId).ToString(),
                ParentTransferId = transfer.ParentTransferId,
                RootTransferId = transfer.RootTransferId,
                ZincirSeviyesi = transfer.ZincirSeviyesi,
                Aciklama = transfer.Aciklama,
                Tarih = transfer.Tarih
            };
        }

        private Dictionary<int, List<SahaTamamlamaIzDto>> GetSahaTamamlamaIzMap(List<int> kaynakSatirIds)
        {
            if (!kaynakSatirIds.Any())
                return new Dictionary<int, List<SahaTamamlamaIzDto>>();

            var izler = _unitOfWork.GetRepository<SandikIcerik>().Queryable()
                .Where(i =>
                    i.CekiSatiriId.HasValue &&
                    i.CekiSatiri!.KaynakCekiSatiriId.HasValue &&
                    kaynakSatirIds.Contains(i.CekiSatiri.KaynakCekiSatiriId.Value) &&
                    i.Sandik.Proje.ProjeTipiId == (int)ProjeTipi.Saha)
                .Select(i => new SahaTamamlamaIzDto
                {
                    KaynakCekiSatiriId = i.CekiSatiri!.KaynakCekiSatiriId!.Value,
                    KaynakProjeId = i.CekiSatiri.KaynakCekiSatiri!.Ceki.ProjeId,
                    KaynakProjeNo = i.CekiSatiri.KaynakCekiSatiri.Ceki.Proje.ProjeNo,
                    KaynakSandikNo = i.CekiSatiri.KaynakCekiSatiri.FiiliSandikNo ?? i.CekiSatiri.KaynakCekiSatiri.CekideGecenSandikNo,
                    KaynakSiraNo = i.CekiSatiri.KaynakCekiSatiri.SiraNo,
                    KaynakUrunAdi = i.CekiSatiri.KaynakCekiSatiri.Aciklama,
                    SahaProjeId = i.Sandik.ProjeId,
                    SahaProjeNo = i.Sandik.Proje.ProjeNo,
                    SahaSandikId = i.SandikId,
                    SahaSandikNo = i.Sandik.SandikNo,
                    SahaCekiSatiriId = i.CekiSatiriId.GetValueOrDefault(),
                    Miktar = i.KonulanAdet > 0 ? i.KonulanAdet : i.CekiSatiri.IstenenAdet,
                    BirimId = i.CekiSatiri.BirimId,
                    DurumId = i.Sandik.DurumId,
                    SevkEdildiMi = i.Sandik.DurumId == (int)SandikDurum.Sevkedildi,
                    SevkTarihi = i.Sandik.Proje.GerceklesenSevkTarihi
                })
                .ToList();

            foreach (var iz in izler)
            {
                iz.Birim = ((Birim)iz.BirimId).ToString();
                iz.DurumMetni = _lookupCache.GetDeger<LookupSandikDurum>(iz.DurumId);
            }

            return izler
                .OrderBy(i => i.SahaProjeNo)
                .ThenBy(i => i.SahaSandikNo)
                .GroupBy(i => i.KaynakCekiSatiriId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        private Dictionary<int, SahaKaynakSatirIzDto> GetSahaKaynakIzMap(List<int> kaynakSatirIds)
        {
            if (!kaynakSatirIds.Any())
                return new Dictionary<int, SahaKaynakSatirIzDto>();

            return _unitOfWork.GetRepository<CekiSatiri>().Queryable()
                .Where(cs => kaynakSatirIds.Contains(cs.Id))
                .Select(cs => new SahaKaynakSatirIzDto
                {
                    CekiSatiriId = cs.Id,
                    KaynakProjeId = cs.Ceki.ProjeId,
                    KaynakProjeNo = cs.Ceki.Proje.ProjeNo,
                    KaynakSandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo,
                    KaynakSiraNo = cs.SiraNo
                })
                .ToList()
                .ToDictionary(x => x.CekiSatiriId);
        }

        private string HesaplaKontrolUyari(CekiSatiri cs)
        {
            var tip = cs.UcKKarsilamaTipiId;

            return tip switch
            {
                // Sevk adeti tam geldi ama Grid eksik sevk ettiyse → her iki durumu da göster
                (int)UcKDurum.TamGeldi when cs.GridDurumuId == (int)GridDurum.EksikGeldi && cs.KalanMiktar <= 0
                    => "GRİD EKSİK SEVK, SEVK ADETİ TAM GELDİ — TAMAMLANDI",
                (int)UcKDurum.TamGeldi when cs.GridDurumuId == (int)GridDurum.EksikGeldi
                    => "GRİD EKSİK SEVK, SEVK ADETİ TAM GELDİ",
                // Grid tam sevk + Sevk adeti tam geldi
                (int)UcKDurum.TamGeldi when cs.KalanMiktar <= 0 => "TAMAMLANDI",
                (int)UcKDurum.TamGeldi => "SEVK ADETİ TAM GELDİ",
                (int)UcKDurum.EksikGeldi => "SEVK ADETİ EKSİK GELDİ",
                (int)UcKDurum.Gelmedi => "GELMEDİ",
                (int)UcKDurum.GeriGonderildi => $"GERİ GÖNDERİLDİ – {(cs.GeriGonderilmeSebebiId.HasValue ? _lookupCache.GetDeger<LookupGeriGonderilmeSebebi>(cs.GeriGonderilmeSebebiId.Value) : "Belirtilmemiş")}",
                (int)UcKDurum.ProjedenKarsilandi => $"PROJEDEN KARŞILANDI – {cs.KaynakHedefProjeNo ?? ""}",
                (int)UcKDurum.StoktanKarsilandi => "STOKTAN KARŞILANDI",
                (int)UcKDurum.TedarikcidenGeldi => "TEDARİKÇİDEN GELDİ",
                (int)UcKDurum.FazlaGeldi => "FAZLA GELDİ - STOKA AKTARILDI",
                (int)UcKDurum.HataliUrun => $"HATALI ÜRÜN – {cs.HataliMiktar} adet",
                _ when cs.GridDurumuId == (int)GridDurum.Iptal => "GRİD İPTAL – İŞLEM YAPILAMAZ",
                _ when cs.GridDurumuId == (int)GridDurum.TrafoSevk
                    && cs.GridSevkDurumuId == (int)GridSevkDurum.SevkEdildi
                    && (cs.GridSevkMiktari ?? 0) > 0 => "KISMİ TRAFO SEVK – 3K SEVK BEKLİYOR",
                _ when cs.GridDurumuId == (int)GridDurum.TrafoSevk => "TRAFO SEVK – 3K FİZİKSEL İŞLEM YOK",
                _ when cs.GridDurumuId == (int)GridDurum.TamGeldi && cs.GelenMiktar < cs.IstenenAdet =>
                    "UYARI: GRİD TAM SEVK, 3K EKSİK GELİŞ",
                _ => "BEKLİYOR"
            };
        }
    }
}

