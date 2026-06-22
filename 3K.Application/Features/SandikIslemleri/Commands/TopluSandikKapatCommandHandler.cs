using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class TopluSandikKapatCommandHandler : IRequestHandler<TopluSandikKapatCommand, TopluSandikKapatResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public TopluSandikKapatCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IHareketService hareketService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _hareketService = hareketService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<TopluSandikKapatResult> Handle(TopluSandikKapatCommand request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated)
            {
                return new TopluSandikKapatResult { IsSuccess = false, Message = "Oturum açmanız gerekiyor." };
            }

            if (request.SandikIds == null || !request.SandikIds.Any())
            {
                return new TopluSandikKapatResult { IsSuccess = false, Message = "Herhangi bir sandık seçilmedi." };
            }

            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var uyarilar = new List<SandikUyariDetay>();
            var kapatilacakSandiklar = new List<Sandik>();
            var sevkEdilmisSandiklar = new List<string>();
            var sahayaAktarilanSandiklar = new List<string>();

            foreach (var sandikId in request.SandikIds)
            {
                var sandik = await sandikRepo.GetByIdAsync(sandikId);
                if (sandik == null) continue;

                if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                {
                    sevkEdilmisSandiklar.Add(sandik.SandikNo);
                    continue;
                }

                if (sandik.DurumId == (int)SandikDurum.Kapandi)
                {
                    continue; // Zaten kapalı
                }

                var icerikler = await icerikRepo.FindAsync(si => si.SandikId == sandikId);
                var icerikSatirIds = icerikler
                    .Where(i => i.CekiSatiriId.HasValue)
                    .Select(i => i.CekiSatiriId!.Value)
                    .Distinct()
                    .ToList();

                if (icerikSatirIds.Count > 0)
                {
                    var icerikSatirlari = await cekiSatiriRepo.FindAsync(cs => icerikSatirIds.Contains(cs.Id));
                    var sahayaAktarilanSatirIdleri = await SahaAktarimBlokajHelper.GetAktarilanKaynakSatirIdleriAsync(
                        _sahaTamamlamaService,
                        icerikSatirlari,
                        cancellationToken);

                    if (sahayaAktarilanSatirIdleri.Any())
                    {
                        sahayaAktarilanSandiklar.Add(sandik.SandikNo);
                        continue;
                    }
                }

                var hataliUrunler = new List<object>();

                foreach (var icerik in icerikler)
                {
                    if (!icerik.CekiSatiriId.HasValue) continue;
                    var urun = await cekiSatiriRepo.GetByIdAsync(icerik.CekiSatiriId.Value);
                    if (urun == null) continue;

                    if (urun.KalanMiktar > 0)
                    {
                        hataliUrunler.Add(new
                        {
                            SiraNo = urun.SiraNo,
                            Barkod = urun.BarkodNo,
                            Aciklama = urun.Aciklama,
                            Kalan = urun.KalanMiktar,
                            Durum = urun.UcKDurumuId,
                            GridDurum = urun.GridDurumuId
                        });
                    }
                }

                if (hataliUrunler.Any())
                {
                    uyarilar.Add(new SandikUyariDetay
                    {
                        SandikNo = sandik.SandikNo,
                        UrunHatalari = hataliUrunler
                    });
                }
                else
                {
                    kapatilacakSandiklar.Add(sandik);
                }
            }

            if (sevkEdilmisSandiklar.Any())
            {
                return new TopluSandikKapatResult
                {
                    IsSuccess = false,
                    Message = $"{string.Join(", ", sevkEdilmisSandiklar)} numaralı sandık(lar) sevk edildiği için üzerinde işlem yapılamaz."
                };
            }

            // Uyarı varsa ve ForceClose değilse işlemi tamamen reddet
            if (sahayaAktarilanSandiklar.Any())
            {
                return new TopluSandikKapatResult
                {
                    IsSuccess = false,
                    Message = $"{string.Join(", ", sahayaAktarilanSandiklar)} numaralı sandık(lar) içinde sahaya aktarılmış ürünler var. Kaynak proje sandıkları kapatılamaz; işlem saha projesinde yürütülmelidir."
                };
            }

            if (uyarilar.Any() && !request.ForceClose)
            {
                return new TopluSandikKapatResult
                {
                    IsSuccess = false,
                    HasMissingOrDefectiveItems = true,
                    Message = "Seçilen sandıklarda eksik veya hatalı ürünler var. Eksikler giderilmeden sandıklar kapatılamaz.",
                    UyariDetaylari = uyarilar
                };
            }

            // Geri kalanları kapat (veya Force ise hepsini kapat)
            var islenecekler = request.ForceClose ? await sandikRepo.FindAsync(s => request.SandikIds.Contains(s.Id)) : kapatilacakSandiklar;

            if (!islenecekler.Any())
            {
                return new TopluSandikKapatResult { IsSuccess = true, Message = "İşlem yapılacak yeni bir sandık bulunamadı." };
            }

            foreach (var s in islenecekler)
            {
                if (s.DurumId != (int)SandikDurum.Kapandi)
                {
                    var eskiDurumId = s.DurumId;
                    var eskiDurumMetni = Enum.GetName(typeof(SandikDurum), eskiDurumId) ?? eskiDurumId.ToString();
                    var yeniDurumMetni = Enum.GetName(typeof(SandikDurum), SandikDurum.Kapandi) ?? "Hazir";

                    s.DurumId = (int)SandikDurum.Kapandi;
                    sandikRepo.Update(s);

                    await _hareketService.HareketKaydetAsync(new HareketGecmisi
                    {
                        ProjeId = s.ProjeId,
                        KullaniciId = _currentUserService.UserId ?? 0,
                        ReferansTipi = "Sandik",
                        ReferansId = s.Id.ToString(),
                        Islem = "Toplu Sandık Kapatma",
                        IslemTipiId = (int)IslemTipi.TopluSandikKapatildi,
                        EskiDeger = eskiDurumMetni,
                        YeniDeger = yeniDurumMetni,
                        Aciklama = request.ForceClose
                            ? $"Sandık {s.SandikNo} (eksik ürün loguna rağmen zorunlu onayla) toplu işlemle kapatıldı."
                            : $"Sandık {s.SandikNo} toplu işlemle başarıyla kapatıldı."
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return new TopluSandikKapatResult { IsSuccess = true, Message = $"{islenecekler.Count()} adet sandık başarıyla hazırlandı." };
        }
    }
}
