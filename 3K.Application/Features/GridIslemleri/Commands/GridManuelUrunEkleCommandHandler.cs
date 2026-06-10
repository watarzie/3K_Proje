using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    public class GridManuelUrunEkleCommandHandler : IRequestHandler<GridManuelUrunEkleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public GridManuelUrunEkleCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(GridManuelUrunEkleCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SandikNo))
                return Result.Failure("Sandık numarası zorunludur.", 400);

            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var sonCeki = (await cekiRepo.FindAsync(c => c.ProjeId == request.ProjeId))
                          .OrderByDescending(c => c.YuklemeTarihi)
                          .FirstOrDefault();
            
            if (sonCeki == null) return Result.Failure("Projeye ait çeki bulunamadı.", 404);

            // Sandık kontrolü veya oluşturma
            var sandik = (await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId && s.SandikNo == request.SandikNo)).FirstOrDefault();
            if (sandik != null && SandikSevkKilidiHelper.SandikKilitliMi(sandik))
                return Result.Failure("Bu sandık sevk edildiği için Grid manuel ürün ekleyemez.");

            if (sandik == null)
            {
                sandik = new Sandik
                {
                    ProjeId = request.ProjeId,
                    SandikNo = request.SandikNo,
                    Ad = request.SandikIsmi,
                    TipId = 1, // Standart Sandık (opsiyonel, 3K'da da genelde 1)
                    DurumId = (int)SandikDurum.Hazirlaniyor,
                    DepoLokasyonId = (int)DepoLokasyon.Belirsiz
                };
                await sandikRepo.AddAsync(sandik);
                await _unitOfWork.SaveChangesAsync(); // Sandik.Id'yi almak için
            }

            var yeniUrun = new CekiSatiri
            {
                CekiId = sonCeki.Id,
                SiraNo = 9999, // Manuel eklenen
                BarkodNo = request.BarkodNo,
                Aciklama = request.Aciklama,
                IstenenAdet = request.IstenenAdet,
                BirimId = request.BirimId ?? (int)Birim.Adet,
                CekideGecenSandikNo = request.SandikNo,
                FiiliSandikNo = request.SandikNo,
                DurumId = (int)UrunDurum.Bekliyor,
                IsManuelEklenen = true,
                EklemeNedeni = request.EklemeNedeni,
                // Grid tarafından eklendiğinde doğrudan "Tam Geldi" yapılıp sevk edilmesi bekleniyor
                GridDurumuId = (int)GridDurum.TamGeldi,
                GridGelenAdet = request.IstenenAdet,
                GridSevkDurumuId = 3, // Sevk Edilmedi
                UcKDurumuId = (int)UcKDurum.Bekliyor,
                SurecDurumId = (int)SurecDurum.Tamamlandi
            };

            await cekiSatiriRepo.AddAsync(yeniUrun);
            await _unitOfWork.SaveChangesAsync();

            // SandikIcerik eklenerek ürün 3K sandığına da düşürülüyor
            await sandikIcerikRepo.AddAsync(new SandikIcerik
            {
                SandikId = sandik.Id,
                CekiSatiriId = yeniUrun.Id,
                KonulanAdet = request.IstenenAdet,
                EksikAdet = 0
            });
            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = yeniUrun.Id.ToString(),
                Islem = "Grid Manuel Ürün Eklendi",
                IslemTipiId = (int)IslemTipi.ManuelUrunEklendi,
                KullaniciId = _currentUserService.UserId ?? 0,
                Aciklama = $"Neden: {request.EklemeNedeni}, Miktar: {request.IstenenAdet} {((Birim?)request.BirimId)?.ToString()}, Sandık: {request.SandikNo}"
            });

            return Result.Success();
        }
    }
}
