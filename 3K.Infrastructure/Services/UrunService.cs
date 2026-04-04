using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    public class UrunService : IUrunService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public UrunService(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<CekiSatiri?> GetUrunDetayAsync(int cekiSatiriId)
        {
            return await _context.CekiSatirlari
                .Include(cs => cs.Paketleyen)
                .Include(cs => cs.KontrolEden)
                .Include(cs => cs.SandikIcerikleri)
                .Include(cs => cs.FBTransferleri)
                .Include(cs => cs.StokHareketleri)
                .FirstOrDefaultAsync(cs => cs.Id == cekiSatiriId);
        }

        public async Task<bool> UrunAdetGuncelleAsync(int cekiSatiriId, int konulanAdet, int eksikAdet)
        {
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var urun = await repo.GetByIdAsync(cekiSatiriId);
            if (urun == null) return false;

            var siRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = await siRepo.FindAsync(si => si.CekiSatiriId == cekiSatiriId);
            var icerik = icerikler.FirstOrDefault();
            if (icerik != null)
            {
                icerik.KonulanAdet = konulanAdet;
                icerik.EksikAdet = eksikAdet;
                siRepo.Update(icerik);
            }

            if (konulanAdet >= urun.IstenenAdet)
                urun.Durum = "Tamamlandi";
            else if (konulanAdet > 0)
                urun.Durum = "KismiGeldi";
            else if (eksikAdet > 0)
                urun.Durum = "Eksik";

            repo.Update(urun);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PaketleyenAtaAsync(int cekiSatiriId, int paketleyenId)
        {
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var urun = await repo.GetByIdAsync(cekiSatiriId);
            if (urun == null) return false;

            urun.PaketleyenId = paketleyenId;
            repo.Update(urun);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> KontrolEdenAtaAsync(int cekiSatiriId, int kontrolEdenId)
        {
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var urun = await repo.GetByIdAsync(cekiSatiriId);
            if (urun == null) return false;

            urun.KontrolEdenId = kontrolEdenId;
            repo.Update(urun);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AciklamaGuncelleAsync(int cekiSatiriId, string aciklama)
        {
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var urun = await repo.GetByIdAsync(cekiSatiriId);
            if (urun == null) return false;

            urun.Remarks = aciklama;
            repo.Update(urun);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DurumGuncelleAsync(int cekiSatiriId, string yeniDurum)
        {
            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var urun = await repo.GetByIdAsync(cekiSatiriId);
            if (urun == null) return false;

            urun.Durum = yeniDurum;
            repo.Update(urun);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
