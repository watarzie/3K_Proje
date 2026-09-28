namespace _3K.Core.Interfaces;

/// <summary>3K sandık durum raporu.</summary>
public interface IUcKSandikDurumRaporService
{
    Task<byte[]> UcKSandikDurumRaporuPdfOlusturAsync(int projeId);
}
