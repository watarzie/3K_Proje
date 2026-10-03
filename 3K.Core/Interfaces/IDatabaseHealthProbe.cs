namespace _3K.Core.Interfaces;

public enum DatabaseHealth
{
    Connected,
    Unreachable,
    Error
}

/// <summary>Veritabanı bağlantısını denetler; hata ayrıntısı loglanır, çağırana taşınmaz.</summary>
public interface IDatabaseHealthProbe
{
    Task<DatabaseHealth> CheckAsync(CancellationToken cancellationToken);
}
