using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    public class FBTransferService : IFBTransferService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public FBTransferService(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<FBTransfer> TransferOlusturAsync(FBTransfer transfer)
        {
            var repo = _unitOfWork.GetRepository<FBTransfer>();
            await repo.AddAsync(transfer);
            await _unitOfWork.SaveChangesAsync();
            return transfer;
        }

        public async Task<IEnumerable<FBTransfer>> GetUrunTransferleriAsync(int cekiSatiriId)
        {
            return await _context.FBTransferleri
                .Include(fb => fb.Kullanici)
                .Where(fb => fb.CekiSatiriId == cekiSatiriId)
                .OrderByDescending(fb => fb.Tarih)
                .ToListAsync();
        }
    }
}
