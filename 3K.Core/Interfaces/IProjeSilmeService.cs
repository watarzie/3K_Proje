namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Bir projeyi, sahip olduğu ve ona referans veren operasyonel kayıtlarla
    /// birlikte atomik olarak siler.
    /// </summary>
    public interface IProjeSilmeService
    {
        /// <returns>Proje bulunup silindiyse <see langword="true"/>; bulunamadıysa <see langword="false"/>.</returns>
        Task<bool> SilAsync(int projeId, CancellationToken cancellationToken = default);
    }
}
