using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using 3K.Core.Entities;
using 3K.Core.Interfaces;

namespace 3K.Application.Features.SandikIslemleri.Commands
{
    public class FiiliSandikDegistirCommandHandler : IRequestHandler<FiiliSandikDegistirCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public FiiliSandikDegistirCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(FiiliSandikDegistirCommand request, CancellationToken cancellationToken)
    {
        // 1. İlgili Çeki Satırını (Ürünü) Getir
        var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
        var urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId);

        if (urun == null)
            return false; // Ürün bulunamadı

        string eskiSandikNo = urun.FiiliSandikNo;

        // 2. Ürünün Fiili Sandığını Güncelle (Çekide geçen orijinal sandık değişmez)
        urun.FiiliSandikNo = request.YeniFiiliSandikNo;
        cekiSatiriRepo.Update(urun);

        // 3. Revizyon Kaydı Oluştur
        var revizyonRepo = _unitOfWork.GetRepository<Revizyon>();
        var revizyon = new Revizyon
        {
            ProjeId = request.ProjeId,
            Tip = "Sandık Değişikliği",
            EskiDeger = eskiSandikNo,
            YeniDeger = request.YeniFiiliSandikNo,
            Aciklama = $"{eskiSandikNo} no'lu sandıktan {request.YeniFiiliSandikNo} no'lu sandığa alındı", // Otomatik not düşülür
            Tarih = DateTime.UtcNow
        };
        await revizyonRepo.AddAsync(revizyon);

        // 4. Hareket Geçmişi (Log) Kaydı Oluştur
        var hareketRepo = _unitOfWork.GetRepository<HareketGecmisi>();
        var hareket = new HareketGecmisi
        {
            ProjeId = request.ProjeId,
            ReferansTipi = "CekiSatiri",
            ReferansId = urun.Id.ToString(),
            Islem = "Fiili Sandık Değiştirildi",
            KullaniciId = request.KullaniciId,
            Tarih = DateTime.UtcNow
        };
        await hareketRepo.AddAsync(hareket);

        // 5. İşlemleri Veritabanına Yansıt (Tümü başarılı olursa kaydedilir)
        var result = await _unitOfWork.SaveChangesAsync();

        return result > 0;
    }
}
}