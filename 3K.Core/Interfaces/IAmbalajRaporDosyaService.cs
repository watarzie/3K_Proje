using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    public interface IAmbalajRaporDosyaService
    {
        byte[] ExcelOlustur(IReadOnlyList<AmbalajRaporSatiri> satirlar, AmbalajRaporOzeti ozet);
        byte[] PdfOlustur(IReadOnlyList<AmbalajRaporSatiri> satirlar, AmbalajRaporOzeti ozet);
        byte[] UretimFormuExcelOlustur(AmbalajUretimFormuModel form);
        byte[] UretimFormuPdfOlustur(AmbalajUretimFormuModel form);
    }
}
