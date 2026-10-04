using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Common;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>
/// Toplu talep formu Excel'i (G4, G-110). Grid'de eksiği olan ve 3K'nın son durumunda hâlâ kalanı olan satırlar
/// listelenir; istenen miktar 3K kalanıdır. 3K'nın stoktan, tedarikçiden ya da başka projeden tamamen karşıladığı
/// satır Grid'de eksik görünse de forma girmez. Aktif saha aktarımına bağlanmış normal kaynak satır da girmez (S-01):
/// işi saha projesinde sürer, ihtiyaç saha projesinin talep formunda çıkar; iki kez talep edilmez. Düzen istemcideki
/// Talep Formu PDF'inin yapısını izler (başlık, tarih/saat, toplam kalem, sıra numarası, alt not); renkler ortak
/// Excel rapor stilidir.
/// </summary>
internal sealed class TalepFormuRaporService(AppDbContext context, ISahaTamamlamaService sahaTamamlama) : ITalepFormuRaporService
{
    private static readonly string[] Basliklar =
        ["#", "Proje No", "Barkod", "Ürün Adı", "İstenen Miktar", "Ölçü Birimi"];

    public async Task<byte[]> TopluTalepFormuExcelAsync(IReadOnlyList<int> projeIds, CancellationToken cancellationToken = default)
    {
        var projeler = await context.Projeler.AsNoTracking()
            .Where(p => projeIds.Contains(p.Id))
            .Select(p => new { p.Id, p.ProjeNo })
            .ToDictionaryAsync(p => p.Id, p => p.ProjeNo, cancellationToken);
        // GridEksikMiktar ve KalanMiktar veritabanı kolonu değildir; formülleri entity'de tek yerdedir, filtre bellekte uygulanır.
        var adaylar = (await context.CekiSatirlari.AsNoTracking()
                .Include(cs => cs.BirimLookup)
                .Where(cs => projeIds.Contains(cs.Ceki.ProjeId))
                .Select(cs => new { Satir = cs, cs.Ceki.ProjeId })
                .ToListAsync(cancellationToken))
            .Where(x => x.Satir.GridEksikMiktar > 0 && x.Satir.KalanMiktar > 0)
            .ToList();
        // Yalnız kaynak satırlar sahaya aktarılabilir; saha projesindeki aktarılmış satır (KaynakCekiSatiriId dolu) formda kalır.
        var sahayaAktarilanlar = (await sahaTamamlama.GetAktifTamamlamaMapAsync(
                adaylar.Where(x => !x.Satir.KaynakCekiSatiriId.HasValue).Select(x => x.Satir.Id), cancellationToken))
            .Keys.ToHashSet();
        var satirlar = adaylar
            .Where(x => !sahayaAktarilanlar.Contains(x.Satir.Id))
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
            RaporExcelStilleri.SetDecimalCell(sayfa.Cell(satirNo, 5), satir.KalanMiktar);
            sayfa.Cell(satirNo, 6).Value = satir.BirimLookup?.Deger ?? string.Empty;
            if (sira % 2 == 0)
                sayfa.Range(satirNo, 1, satirNo, sonKolon).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFE");
        }
        sayfa.Range(baslikSatiri + 1, 4, Math.Max(baslikSatiri + 1, satirNo), 4).Style.Alignment.WrapText = true;
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
