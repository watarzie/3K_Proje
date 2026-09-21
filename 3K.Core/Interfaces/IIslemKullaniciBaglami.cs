namespace _3K.Core.Interfaces;

/// <summary>Onaylı üretim/finans yürütmesinin doğrulanmış başlatanı; HTTP'den doldurulamaz.</summary>
public interface IIslemKullaniciBaglami
{
    int? IslemKullaniciId { get; }
}
