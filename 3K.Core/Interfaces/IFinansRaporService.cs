namespace _3K.Core.Interfaces
{
    public interface IFinansRaporService
    {
        Task<byte[]> IsRaporuPdfAsync(DateTime? baslangic, DateTime? bitis, string? projeNo, string? musteri, int[]? isTurleri, string kullanici, CancellationToken cancellationToken = default);
        Task<byte[]> IsRaporuExcelAsync(DateTime? baslangic, DateTime? bitis, string? projeNo, string? musteri, int[]? isTurleri, string kullanici, CancellationToken cancellationToken = default);
        Task<byte[]> AylikRaporPdfAsync(int yil, int ay, string[]? gruplar, string kullanici, CancellationToken cancellationToken = default);
        Task<byte[]> AylikRaporExcelAsync(int yil, int ay, string[]? gruplar, string kullanici, CancellationToken cancellationToken = default);
        Task<byte[]> AylikRaporZipAsync(int yil, int ay, string[]? gruplar, string kullanici, CancellationToken cancellationToken = default);
        Task<byte[]> GiderRaporuPdfAsync(string kullanici, CancellationToken cancellationToken = default);
        Task<byte[]> GiderRaporuExcelAsync(string kullanici, CancellationToken cancellationToken = default);
        Task<byte[]> SiparisDurumRaporuPdfAsync(DateTime? baslangic, DateTime? bitis, string? projeNo, string? isGrubu, string? durum, string kullanici, CancellationToken cancellationToken = default);
        Task<byte[]> SiparisDurumRaporuExcelAsync(DateTime? baslangic, DateTime? bitis, string? projeNo, string? isGrubu, string? durum, string kullanici, CancellationToken cancellationToken = default);
    }
}