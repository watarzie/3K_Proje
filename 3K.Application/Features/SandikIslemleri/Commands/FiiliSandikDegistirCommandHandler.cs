using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;

using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class FiiliSandikDegistirCommandHandler : IRequestHandler<FiiliSandikDegistirCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public FiiliSandikDegistirCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(FiiliSandikDegistirCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                transactionCancellationToken => HandleInTransactionAsync(request, transactionCancellationToken),
                cancellationToken);
        }

        private async Task<Result> HandleInTransactionAsync(
            FiiliSandikDegistirCommand request,
            CancellationToken cancellationToken)
        {
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var revizyonRepo = _unitOfWork.GetRepository<Revizyon>();

            var urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null) return Result.Failure("Ürün bulunamadı.", 404);

            if (await SahaAktarimBlokajHelper.KaynakSatirAktarildiMiAsync(
                    _sahaTamamlamaService, urun, cancellationToken))
                return Result.Failure(SahaAktarimBlokajHelper.SandikMesaji, 409);

            string eskiSandikNo = urun.FiiliSandikNo ?? urun.CekideGecenSandikNo;
            var eskiSandik = (await sandikRepo.FindAsync(s =>
                    s.ProjeId == request.ProjeId &&
                    s.SandikNo == eskiSandikNo))
                .FirstOrDefault();

            if (eskiSandik != null && SandikSevkKilidiHelper.SandikKilitliMi(eskiSandik))
                return Result.Failure("Ürün sevk edilmiş sandıkta olduğu için sandığı değiştirilemez.");

            var eskiIcerikler = (await sandikIcerikRepo.FindAsync(
                    si => si.CekiSatiriId == request.CekiSatiriId))
                .ToList();

            if (eskiIcerikler.Count > 1)
            {
                return Result.Failure(
                    "Ürün birden fazla sandığa parçalı olarak tahsis edilmiş. Tamamını tek seferde değiştirmek yerine sandık yönetimindeki miktarlı taşıma işlemini kullanın.",
                    409);
            }

            var eskiIcerik = eskiIcerikler.SingleOrDefault();

            var aktifPartiSayacKontrolu =
                GridUcKSevkPartisiKurali.AktifPartiSandikSayaclariniDogrula(urun, eskiIcerikler);
            if (!aktifPartiSayacKontrolu.IsSuccess)
                return aktifPartiSayacKontrolu;

            if (eskiIcerik == null &&
                urun.AktifGridSevkKarsilananMiktari.GetValueOrDefault() > 0)
            {
                return Result.Failure(
                    "Aktif Grid sevk karşılamasının bağlı olduğu kaynak sandık içeriği bulunamadı. Veri mutabakatı yapılmadan sandık değiştirilemez.",
                    409);
            }

            // Veri bütünlüğü kontrolleri tamamlanmadan hedef sandık üretmeyiz. Aksi
            // halde reddedilen bir taşıma boş bir sandığı kalıcı bırakabilir.
            var hedefSandiklar = await sandikRepo.FindAsync(s =>
                s.ProjeId == request.ProjeId && s.SandikNo == request.YeniFiiliSandikNo);
            var hedefSandik = hedefSandiklar.FirstOrDefault();

            if (hedefSandik != null && SandikSevkKilidiHelper.SandikKilitliMi(hedefSandik))
                return Result.Failure("Hedef sandık sevk edildiği için bu sandığa ürün taşınamaz.");

            if (hedefSandik == null)
            {
                hedefSandik = new Sandik
                {
                    ProjeId = request.ProjeId,
                    SandikNo = request.YeniFiiliSandikNo,
                    DurumId = (int)SandikDurum.Hazirlaniyor
                };
                await sandikRepo.AddAsync(hedefSandik);
                await _unitOfWork.SaveChangesAsync();
            }

            decimal konulanAdet = eskiIcerik?.KonulanAdet ?? urun.IstenenAdet;
            decimal eksikAdet = eskiIcerik?.EksikAdet ?? 0;

            if (eskiIcerik != null) sandikIcerikRepo.Remove(eskiIcerik);

            await sandikIcerikRepo.AddAsync(new SandikIcerik
            {
                SandikId = hedefSandik.Id,
                CekiSatiriId = request.CekiSatiriId,
                TahsisMiktari = eskiIcerik?.TahsisMiktari > 0
                    ? eskiIcerik.TahsisMiktari
                    : urun.IstenenAdet,
                KonulanAdet = konulanAdet,
                EksikAdet = eksikAdet,
                AktifGridSevkKarsilananMiktari = eskiIcerik?.AktifGridSevkKarsilananMiktari ??
                    (urun.AktifGridSevkKarsilananMiktari.HasValue ? 0 : null),
                StokKarsilanan = eskiIcerik?.StokKarsilanan ?? 0,
                ProjeKarsilanan = eskiIcerik?.ProjeKarsilanan ?? 0,
                TedarikciKarsilanan = eskiIcerik?.TedarikciKarsilanan ?? 0,
                Miktar = eskiIcerik?.Miktar ?? 0,
                BirimId = eskiIcerik?.BirimId ?? urun.BirimId,
                KaynakProjeNo = eskiIcerik?.KaynakProjeNo
            });

            urun.FiiliSandikNo = request.YeniFiiliSandikNo;
            var otomatikNot = $"{eskiSandikNo} → {request.YeniFiiliSandikNo}";
            urun.Remarks = string.IsNullOrEmpty(urun.Remarks) ? otomatikNot : $"{urun.Remarks}; {otomatikNot}";
            cekiSatiriRepo.Update(urun);

            await revizyonRepo.AddAsync(new Revizyon
            {
                ProjeId = request.ProjeId,
                KullaniciId = request.KullaniciId,
                Tip = "Sandık Değişikliği",
                EskiDeger = eskiSandikNo,
                YeniDeger = request.YeniFiiliSandikNo,
                Aciklama = otomatikNot
            });

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "Fiili Sandık Değiştirildi",
                IslemTipiId = (int)IslemTipi.FiiliSandikDegistirildi,
                KullaniciId = request.KullaniciId,
                EskiDeger = eskiSandikNo,
                YeniDeger = request.YeniFiiliSandikNo,
                Aciklama = otomatikNot
            });

            return Result.Success();
        }
    }
}
