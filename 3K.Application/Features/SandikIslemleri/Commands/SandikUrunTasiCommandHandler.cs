using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikUrunTasiCommandHandler : IRequestHandler<SandikUrunTasiCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHareketService _hareketService;

        public SandikUrunTasiCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(SandikUrunTasiCommand request, CancellationToken cancellationToken)
        {
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();

            // ===== 1. Kaynak SandikIcerik bul =====
            var kaynakIcerik = await sandikIcerikRepo.GetByIdAsync(request.KaynakSandikIcerikId);
            if (kaynakIcerik == null)
                return Result.Failure("Kaynak sandık içeriği bulunamadı.", 404);

            // ===== 2. Validasyonlar =====
            if (request.TasinanAdet <= 0)
                return Result.Failure("Taşınan adet 0'dan büyük olmalıdır.");

            if (request.TasinanAdet > kaynakIcerik.KonulanAdet)
                return Result.Failure($"Taşınan adet ({request.TasinanAdet}), mevcut adetten ({kaynakIcerik.KonulanAdet}) büyük olamaz.");

            // Hedef sandık var mı?
            var hedefSandik = await sandikRepo.GetByIdAsync(request.HedefSandikId);
            if (hedefSandik == null)
                return Result.Failure("Hedef sandık bulunamadı.", 404);

            // Aynı proje kontrolü
            if (hedefSandik.ProjeId != request.ProjeId)
                return Result.Failure("Hedef sandık farklı bir projeye ait.");

            // Kendine taşıma kontrolü
            if (kaynakIcerik.SandikId == request.HedefSandikId)
                return Result.Failure("Kaynak ve hedef sandık aynı olamaz.");

            // ===== 3. Transaction: Kaynak düş, Hedef ekle =====

            // Kaynak: adeti düşür
            kaynakIcerik.KonulanAdet -= request.TasinanAdet;

            if (kaynakIcerik.KonulanAdet <= 0)
            {
                // Adet 0 olduysa kaydı sil (veya 0 bırak)
                sandikIcerikRepo.Remove(kaynakIcerik);
            }
            else
            {
                sandikIcerikRepo.Update(kaynakIcerik);
            }

            // Hedef: aynı CekiSatiriId var mı kontrol et
            var hedefIcerikler = await sandikIcerikRepo.FindAsync(si =>
                si.SandikId == request.HedefSandikId && si.CekiSatiriId == kaynakIcerik.CekiSatiriId);
            var hedefIcerik = hedefIcerikler.FirstOrDefault();

            if (hedefIcerik != null)
            {
                // Zaten varsa adeti artır (UPDATE)
                hedefIcerik.KonulanAdet += request.TasinanAdet;
                sandikIcerikRepo.Update(hedefIcerik);
            }
            else
            {
                // Yoksa yeni kayıt (INSERT)
                await sandikIcerikRepo.AddAsync(new SandikIcerik
                {
                    SandikId = request.HedefSandikId,
                    CekiSatiriId = kaynakIcerik.CekiSatiriId,
                    KonulanAdet = request.TasinanAdet,
                    EksikAdet = 0
                });
            }

            // CekiSatiri.FiiliSandikNo güncelle (son taşındığı sandığa)
            var cekiSatiri = await cekiSatiriRepo.GetByIdAsync(kaynakIcerik.CekiSatiriId);
            if (cekiSatiri != null)
            {
                cekiSatiri.FiiliSandikNo = hedefSandik.SandikNo;
                cekiSatiriRepo.Update(cekiSatiri);
            }

            // Hedef sandık durumunu güncelle
            var hedefEskiDurumId = hedefSandik.DurumId;

            if (hedefSandik.DurumId == (int)SandikDurum.Bos || hedefSandik.DurumId == (int)SandikDurum.Hazir)
            {
                hedefSandik.DurumId = (int)SandikDurum.Hazirlaniyor;
                sandikRepo.Update(hedefSandik);
            }



            await _unitOfWork.SaveChangesAsync();

            if (hedefEskiDurumId == (int)SandikDurum.Hazir && hedefSandik.DurumId == (int)SandikDurum.Hazirlaniyor)
            {
                var eskiDurumMetni = Enum.GetName(typeof(SandikDurum), hedefEskiDurumId) ?? "Hazir";
                var yeniDurumMetni = Enum.GetName(typeof(SandikDurum), hedefSandik.DurumId) ?? "Hazirlaniyor";

                await _hareketService.HareketKaydetAsync(new HareketGecmisi
                {
                    ProjeId = request.ProjeId,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    ReferansTipi = "Sandik",
                    ReferansId = hedefSandik.Id.ToString(),
                    Islem = "Sandık Geri Açıldı",
                    IslemTipiId = null,
                    EskiDeger = eskiDurumMetni,
                    YeniDeger = yeniDurumMetni,
                    Aciklama = $"İçine yeni ürün/içerik taşınması nedeniyle sandık tekrar '{yeniDurumMetni}' konumuna getirildi."
                });
            }

            await _unitOfWork.SaveChangesAsync();

            // ===== 4. Hareket kaydı =====
            var kaynakSandik = await sandikRepo.GetByIdAsync(kaynakIcerik.SandikId);
            var cekiSatiriText = cekiSatiri?.Aciklama ?? "ürün";
            
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = kaynakIcerik.CekiSatiriId.ToString(),
                Islem = "Sandık Ürün Taşıma",
                IslemTipiId = (int)IslemTipi.UrunTasindi,
                EskiDeger = kaynakSandik?.SandikNo ?? "?",
                YeniDeger = hedefSandik.SandikNo,
                Aciklama = $"{request.TasinanAdet} adet '{cekiSatiriText}', Sandık {kaynakSandik?.SandikNo}'den Sandık {hedefSandik.SandikNo}'e taşındı."
            });

            return Result.Success();
        }
    }
}
