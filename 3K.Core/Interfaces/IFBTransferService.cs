using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// İş akışı 5: FB arası malzeme transfer
    /// </summary>
    public interface IFBTransferService
    {
        Task<FBTransfer> TransferOlusturAsync(FBTransfer transfer);
        Task<IEnumerable<FBTransfer>> GetUrunTransferleriAsync(int cekiSatiriId);
    }
}
