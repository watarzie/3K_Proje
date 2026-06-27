using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class UcKTopluTeslimAlCommandHandler : IRequestHandler<UcKTopluTeslimAlCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public UcKTopluTeslimAlCommandHandler(
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

        public async Task<Result> Handle(UcKTopluTeslimAlCommand request, CancellationToken cancellationToken)
        {
            if (request.Urunler == null || request.Urunler.Count == 0)
                return Result.Failure("En az bir ürün seçilmelidir.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var idler = request.Urunler.Select(u => u.CekiSatiriId).ToList();
            var satirlar = (await repo.FindAsync(cs => idler.Contains(cs.Id))).ToDictionary(s => s.Id);

            if (!satirlar.Any())
                return Result.Failure("Seçilen ürünler bulunamadı.", 404);

            var now = TurkeyTime.Now;
            var kullaniciId = _currentUserService.UserId ?? 0;
            int teslimAlinan = 0;
            var kilitliSatirIdleri = await SandikSevkKilidiHelper.GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                _unitOfWork,
                idler);
            var sahayaAktarilanSatirIdleri = await SahaAktarimBlokajHelper.GetAktarilanKaynakSatirIdleriAsync(
                _sahaTamamlamaService,
                satirlar.Values,
                cancellationToken);
            var kaynakSatirIds = new HashSet<int>();

            foreach (var item in request.Urunler)
            {
                if (!satirlar.TryGetValue(item.CekiSatiriId, out var satir))
                    continue;

                if (kilitliSatirIdleri.Contains(item.CekiSatiriId))
                    continue;

                if (sahayaAktarilanSatirIdleri.Contains(item.CekiSatiriId))
                    continue;

                if (item.GelenMiktar <= 0)
                    continue;

                satir.GelenMiktar += item.GelenMiktar;
                satir.TeslimTarihi = now;
                satir.UcKAciklama = request.Aciklama;

                // 3K durumunu otomatik belirle
                if (satir.GelenMiktar >= satir.IstenenAdet)
                    satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
                else
                    satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;

                // Genel durumu otomatik hesapla
                satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);
                // KURAL 2: Merkezi kalan hesaplaması ve durum override
                _durumHesaplaService.HesaplaKalanVeDurum(satir);

                repo.Update(satir);

                var ilgiliIcerikler = (await sandikIcerikRepo.FindAsync(x => x.CekiSatiriId == satir.Id)).ToList();
                if (ilgiliIcerikler.Any())
                {
                    var anaIcerik = ilgiliIcerikler.First();
                    anaIcerik.KonulanAdet = Math.Max(satir.GelenMiktar + satir.KarsilananMiktar - satir.ProjeGonderilen, 0);
                    anaIcerik.StokKarsilanan = satir.StokKarsilanan;
                    anaIcerik.ProjeKarsilanan = satir.ProjeKarsilanan;
                    anaIcerik.TedarikciKarsilanan = satir.TedarikciKarsilanan;
                    sandikIcerikRepo.Update(anaIcerik);
                }

                await SandikLokasyonHelper.VarsayilanUcKDepoLokasyonuAtaAsync(_unitOfWork, satir.Id);

                if (satir.KaynakCekiSatiriId.HasValue)
                    kaynakSatirIds.Add(satir.KaynakCekiSatiriId.Value);

                teslimAlinan++;
            }

            await _unitOfWork.SaveChangesAsync();

            if (kaynakSatirIds.Count > 0)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(kaynakSatirIds, cancellationToken);

            // Toplu hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = kullaniciId,
                ReferansTipi = "TopluTeslim",
                ReferansId = string.Join(",", idler),
                Islem = "3K Toplu Teslim Alma",
                IslemTipiId = (int)IslemTipi.UcKTopluTeslimAlindi,
                YeniDeger = $"{teslimAlinan} ürün teslim alındı",
                Aciklama = request.Aciklama
            });

            return Result.Success();
        }
    }
}
