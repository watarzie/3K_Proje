namespace _3K.Core.Interfaces
{
    public interface ICekiRevizyonDosyaTemizlemeService
    {
        Task<int> BugunYuklenenUygulanmisDosyaIcerikleriniTemizleAsync(
            CancellationToken cancellationToken = default);
    }
}
