using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// UML Sequence Diagram: RevizyonService
    /// </summary>
    public interface IRevizyonService
    {
        Task RevizyonKaydetAsync(Revizyon revizyon);
        Task<IEnumerable<Revizyon>> GetProjeRevizyonlariAsync(int projeId);
    }
}
