namespace _3K.Core.Interfaces
{
    /// <summary>
    /// İş akışı 10: Mail akışı
    /// Günlük güncelleme, haftalık eksik listesi, proje bazlı eksik listeleri
    /// </summary>
    public interface IMailService
    {
        Task GunlukGuncellemeMailiGonderAsync();
        Task HaftalikEksikListesiGonderAsync();
        Task ProjeBazliEksikListesiGonderAsync(int projeId);
    }
}
