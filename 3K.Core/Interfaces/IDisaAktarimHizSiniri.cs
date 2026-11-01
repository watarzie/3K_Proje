namespace _3K.Core.Interfaces;

/// <summary>
/// PDF/Excel üretimi ve dosya indirme sınırı (kullanıcı başına). MediatR hattındaki dışa aktarım davranışı kullanır;
/// uygulaması ve eşiği diğer hız sınırlarıyla birlikte API katmanındadır.
/// </summary>
public interface IDisaAktarimHizSiniri
{
    /// <summary>Kullanıcının kotasından bir istek düşer; kota bittiyse <c>false</c>.</summary>
    bool IzinVer(int kullaniciId);
}
