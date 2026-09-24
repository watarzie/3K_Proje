namespace _3K.Core.Interfaces;

/// <summary>Çoklu instance form sürümü ve idempotency kontrolünü aynı transaction içinde sıralar.</summary>
public interface IAmbalajFormIslemKilidi
{
    Task KilitleAsync(CancellationToken cancellationToken);
}
