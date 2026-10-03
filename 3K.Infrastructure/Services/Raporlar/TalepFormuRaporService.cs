using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Common;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>
/// Toplu talep formu Excel'i (G4). Düzen istemcideki Talep Formu PDF'inin yapısını izler (başlık, tarih/saat,
/// toplam kalem, sıra numarası, alt not); renkler ortak Excel rapor stilidir.
/// </summary>
internal sealed class TalepFormuRaporService(AppDbContext context) : ITalepFormuRaporService
{
    private static readonly string[] Basliklar =
        ["#", "Proje No", "Barkod", "Ürün Adı", "Ürün Açıklaması", "İstenen Miktar", "Ölçü Birimi"];

    public async Task<byte[]> TopluTalepFormuExcelAsync(IReadOnlyList<int> projeIds, CancellationToken cancellationToken = default)
    {
        var projeler = await context.Projeler.AsNoTracking()
            .Where(p => projeIds.Contains(p.Id))
            .Select(p => new { p.Id, p.ProjeNo })
            .ToDictionaryAsync(p => p.Id, p => p.ProjeNo, cancellationToken);
        // GridEksikMiktar veritabanı kolonu değildir; formülü entity'de tek yerdedir, filtre bellekte uygulanır.
        var satirlar = (await context.CekiSatirlari.AsNoTracking()
                .Include(cs => cs.BirimLookup)
                .Where(cs => projeIds.Contains(cs.Ceki.ProjeId))
                .Select(cs => new { Satir = cs, cs.Ceki.ProjeId })
                .ToListAsync(cancellationToken))
            .Where(x => x.Satir.GridEksikMiktar > 0)
            .OrderBy(x => IndexOf(projeIds, x.ProjeId))
            .ThenBy(x => x.Satir.FiiliSandikNo ?? x.Satir.CekideGecenSandikNo, SandikNumarasiComparer.Instance)
            .ThenBy(x => x.Satir.SiraNo)
            .ThenBy(x => x.Satir.Id)
            .ToList();

        using var workbook = new XLWorkbook();
        var sayfa = workbook.Worksheets.Add("Talep Formu");
        var sonKolon = Basliklar.Length;
        const int baslikSatiri = 5;
        var simdi = TurkeyTime.Now;

        sayfa.Cell(1, 1).Value = "TALEP FORMU";
        RaporExcelStilleri.StyleExcelTitle(sayfa, sonKolon);
        sayfa.Cell(2, 1).Value = "Tarih";
        sayfa.Cell(2, 2).Value = simdi.ToString("dd.MM.yyyy");
        sayfa.Cell(2, 3).Value = "Saat";
        sayfa.Cell(2, 4).Value = simdi.ToString("HH:mm");
        sayfa.Cell(3, 1).Value = "Projeler";
        sayfa.Cell(3, 2).Value = string.Join(", ", projeIds.Where(projeler.ContainsKey).Select(id => projeler[id]));
        sayfa.Cell(3, 3).Value = "Toplam";
        sayfa.Cell(3, 4).Value = $"{satirlar.Count} kalem";
        RaporExcelStilleri.StyleExcelInfoRows(sayfa, sonKolon);
        sayfa.Range(2, 1, 3, 1).Style.Font.Bold = true;
        sayfa.Range(2, 3, 3, 3).Style.Font.Bold = true;

        for (var kolon = 0; kolon < sonKolon; kolon++)
            sayfa.Cell(baslikSatiri, kolon + 1).Value = Basliklar[kolon];
        RaporExcelStilleri.StyleExcelHeader(sayfa, baslikSatiri, sonKolon);

        var satirNo = baslikSatiri;
        foreach (var (kayit, sira) in satirlar.Select((x, i) => (x, i + 1)))
        {
            satirNo++;
            var satir = kayit.Satir;
            sayfa.Cell(satirNo, 1).Value = sira;
            sayfa.Cell(satirNo, 2).Value = projeler.GetValueOrDefault(kayit.ProjeId, string.Empty);
            sayfa.Cell(satirNo, 3).Value = RaporBicimleri.RaporMetni(satir.BarkodNo);
            sayfa.Cell(satirNo, 4).Value = RaporBicimleri.RaporMetni(satir.Aciklama);
            sayfa.Cell(satirNo, 5).Value = RaporBicimleri.RaporMetni(satir.Remarks);
            RaporExcelStilleri.SetDecimalCell(sayfa.Cell(satirNo, 6), satir.GridEksikMiktar);
            sayfa.Cell(satirNo, 7).Value = satir.BirimLookup?.Deger ?? string.Empty;
            if (sira % 2 == 0)
                sayfa.Range(satirNo, 1, satirNo, sonKolon).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFE");
        }
        sayfa.Range(baslikSatiri + 1, 4, Math.Max(baslikSatiri + 1, satirNo), 5).Style.Alignment.WrapText = true;
        RaporExcelStilleri.FinalizeExcelWorksheet(sayfa, baslikSatiri, satirNo, sonKolon);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static int IndexOf(IReadOnlyList<int> ids, int id)
    {
        for (var i = 0; i < ids.Count; i++)
            if (ids[i] == id) return i;
        return int.MaxValue;
    }
}
