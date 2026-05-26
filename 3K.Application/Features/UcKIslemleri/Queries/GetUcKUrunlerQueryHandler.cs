using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Queries
{
    public class GetUcKUrunlerQueryHandler : IRequestHandler<GetUcKUrunlerQuery, Result<List<UcKUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public GetUcKUrunlerQueryHandler(IUnitOfWork unitOfWork, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
        }

        public async Task<Result<List<UcKUrunDto>>> Handle(GetUcKUrunlerQuery request, CancellationToken cancellationToken)
        {
            // Tek sorgu: CekiSatirlari → Ceki.ProjeId filtresi ile direkt erişim (AsNoTracking GenericRepository'de)
            var satirlar = (await _unitOfWork.GetRepository<CekiSatiri>()
                .FindAsync(cs => cs.Ceki.ProjeId == request.ProjeId))
                .OrderBy(cs => cs.SiraNo)
                .ToList();

            if (!satirlar.Any())
                return Result<List<UcKUrunDto>>.Failure("Bu projeye ait çeki bulunamadı.", 404);

            var satirIdler = satirlar.Select(s => s.Id).ToList();
            var transferler = (await _unitOfWork.GetRepository<ProjeTransfer>()
                .FindAsync(t => t.DurumId == (int)ProjeTransferDurum.Aktif &&
                    (satirIdler.Contains(t.KaynakCekiSatiriId) ||
                     (t.HedefCekiSatiriId.HasValue && satirIdler.Contains(t.HedefCekiSatiriId.Value)))))
                .ToList();

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

                    return new UcKUrunDto
                    {
                        CekiSatiriId = cs.Id,
                        SiraNo = cs.SiraNo,
                        BarkodNo = cs.BarkodNo,
                        OlcuResmiPozNo = cs.OlcuResmiPozNo,
                        Aciklama = cs.Aciklama,
                        SandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo,
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
                        Kalan = cs.KalanMiktar,
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
                        EksikMiktar = cs.EksikMiktar,
                        // Kalite & Süreç
                        KaliteDurumId = cs.KaliteDurumId,
                        KaliteDurumMetni = cs.KaliteDurumId.HasValue ? _lookupCache.GetDeger<LookupKaliteDurum>(cs.KaliteDurumId.Value) : null,
                        SurecDurumId = cs.SurecDurumId,
                        SurecDurumMetni = cs.SurecDurumId.HasValue ? _lookupCache.GetDeger<LookupSurecDurum>(cs.SurecDurumId.Value) : null,
                        IsManuelEklenen = cs.IsManuelEklenen
                    };
                })
                .ToList();

            return Result<List<UcKUrunDto>>.Success(result);
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

