using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Helpers;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Common;
using _3K.Core.Exceptions;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>Eksik ürünler raporu (PDF ve Excel): kalan miktar, proje transferleri ve saha aktarımlarıyla.</summary>
internal sealed class EksikUrunlerRaporService(AppDbContext context) : IEksikUrunlerRaporService
{
    /// <summary>
    /// Normal projelerde kalan ürünlerin, saha projelerinde sevk sonrası tüm satırların PDF raporu.
    /// </summary>
    public async Task<byte[]> EksikUrunlerRaporuPdfOlusturAsync(int projeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var proje = await context.Projeler
            .AsNoTracking()
            .Include(p => p.ProjeTipiLookup)
            .FirstOrDefaultAsync(p => p.Id == projeId, cancellationToken);

        if (proje == null)
            throw new KayitBulunamadiException($"Proje bulunamadı: {projeId}");

        // Grid durumu EksikGeldi veya Gelmedi olan VE kalan > 0 olan ürünler
        var satirlar = await context.CekiSatirlari
            .AsNoTracking()
            .Include(cs => cs.BirimLookup)
            .Include(cs => cs.GridDurumLookup)
            .Include(cs => cs.UcKDurumLookup)
            .Include(cs => cs.DurumLookup)
            .Include(cs => cs.GeriGonderilmeSebebiLookup)
            .Include(cs => cs.SurecDurumLookup)
            .Include(cs => cs.SandikIcerikleri).ThenInclude(si => si.Sandik)
            .Where(cs => cs.Ceki.ProjeId == projeId)
            .OrderBy(cs => cs.SiraNo)
            .ToListAsync(cancellationToken);

        var tamamlamaPlanMap = await context.GetTamamlamaPlanMapAsync(satirlar, sadeceSevkEdilenSandiklar: false, cancellationToken);
        var sahaEksikRaporuMu = proje.ProjeTipiId == (int)ProjeTipi.Saha;
        var aktifSahaAktarimKaynakIds = sahaEksikRaporuMu
            ? new HashSet<int>()
            : await GetAktifSahaAktarimKaynakSatirIdsAsync(satirlar.Select(s => s.Id), cancellationToken);

        // Normal projelerde eksik filtresi korunur; saha sevk sonrası raporunda tamamlananlar da listelenir.
        // Sıralama: Sandık numarasına göre sayısal (1,2,3...10,11), sonra sıra numarasına göre
        var raporSatirlari = satirlar
            .Where(cs => sahaEksikRaporuMu ||
                RaporSorgulari.GetEksikRaporKalan(cs, tamamlamaPlanMap) > 0 ||
                aktifSahaAktarimKaynakIds.Contains(cs.Id))
            .OrderBy(cs => GetEksikRaporSandikSortKey(cs, projeId), SandikNumarasiComparer.Instance)
            .ThenBy(cs => cs.SiraNo)
            .ThenBy(cs => cs.Id)
            .ToList();

        QuestPDF.Settings.License = LicenseType.Community;

        // Renk paleti
        var headerBg = Colors.Blue.Darken3;
        var headerText = Colors.White;
        var dangerColor = "#D32F2F";
        var warningColor = "#F57C00";
        var tableBorderColor = Colors.Grey.Lighten2;
        var altRowBg = "#F8FAFE";

        var raporBasligi = sahaEksikRaporuMu ? "Sevk Sonrası Eksik Raporu" : "Eksik Ürünler Raporu";
        var raporAltBasligi = sahaEksikRaporuMu
            ? "3K Sevkiyat Yönetim Sistemi - Saha Sevk Sonrası Eksik Çıktısı"
            : "3K Sevkiyat Yönetim Sistemi - Proje Eksik Ürün Çıktısı";
        var raporTarihi = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");
        var toplamRaporUrun = raporSatirlari.Count;
        var toplamEksikUrun = raporSatirlari.Count(s => RaporSorgulari.GetEksikRaporKalan(s, tamamlamaPlanMap) > 0);
        var toplamTamamlananUrun = toplamRaporUrun - toplamEksikUrun;
        var toplamKalanAdet = raporSatirlari.Sum(s => RaporSorgulari.GetEksikRaporKalan(s, tamamlamaPlanMap));
        var toplamIstenenAdet = raporSatirlari.Sum(s => s.IstenenAdet);
        var toplamGelenAdet = raporSatirlari.Sum(s => s.GelenMiktar + s.StokKarsilanan + s.ProjeKarsilanan + s.TedarikciKarsilanan);
        var summaryAccentColor = sahaEksikRaporuMu ? "#1565C0" : dangerColor;
        var (projedenAlinanMap, projeyeVerilenMap) = await GetProjeTransferRaporMapleriAsync(raporSatirlari, cancellationToken);
        var (kaynakProjeMap, hedefSahaMap) = await GetSahaAktarimRaporMapleriAsync(raporSatirlari, cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                // ===== HEADER =====
                page.Header().Column(headerCol =>
                {
                    // Üst çubuk
                    headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("3K").Bold().FontSize(22).FontColor(headerText);
                            col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                        });
                        row.RelativeItem(2).AlignCenter().Column(col =>
                        {
                            col.Item().Text(raporBasligi).Bold().FontSize(16).FontColor(headerText);
                            col.Item().Text(raporAltBasligi).FontSize(7).FontColor(Colors.Grey.Lighten3);
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                        });
                    });

                    // Proje bilgisi kartları
                    headerCol.Item().PaddingTop(10).PaddingBottom(5).Row(row =>
                    {
                        // Proje Bilgisi
                        row.RelativeItem().Border(1).BorderColor(tableBorderColor).Padding(8).Column(col =>
                        {
                            col.Item().Text("PROJE BİLGİSİ").Bold().FontSize(7).FontColor(Colors.Blue.Darken1);
                            col.Item().PaddingTop(3).Text(proje.ProjeNo).Bold().FontSize(13);
                            col.Item().Text($"Müşteri: {proje.Musteri}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(proje.FBNo))
                                col.Item().Text($"FB No: {proje.FBNo}").FontSize(8);
                        });

                        row.ConstantItem(10); // Spacer

                        // Özet
                        row.RelativeItem().Border(1).BorderColor(summaryAccentColor).Padding(8).Column(col =>
                        {
                            col.Item().Text(sahaEksikRaporuMu ? "DURUM ÖZET" : "EKSİK ÖZET").Bold().FontSize(7).FontColor(summaryAccentColor);
                            col.Item().PaddingTop(3).Text(sahaEksikRaporuMu ? $"{toplamRaporUrun} ürün listeleniyor" : $"{toplamEksikUrun} ürün eksik").Bold().FontSize(13).FontColor(summaryAccentColor);
                            if (sahaEksikRaporuMu)
                                col.Item().Text($"Eksik: {toplamEksikUrun} / Tamamlanan: {toplamTamamlananUrun}").FontSize(8);
                            col.Item().Text($"Toplam Kalan: {RaporBicimleri.FormatAdet(toplamKalanAdet)} adet").FontSize(8);
                            col.Item().Text($"Karşılama Oranı: %{RaporBicimleri.FormatAdet(toplamIstenenAdet > 0 ? (toplamGelenAdet * 100 / toplamIstenenAdet) : 0)}").FontSize(8);
                        });
                    });

                    headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(tableBorderColor);
                });

                // ===== CONTENT - TABLE =====
                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(24);   // #
                        columns.ConstantColumn(28);   // Sıra
                        columns.RelativeColumn(1.25f); // Barkod
                        columns.ConstantColumn(42);   // Poz No
                        columns.RelativeColumn(2.1f); // Açıklama
                        columns.RelativeColumn(1.45f); // Sandık no ve adı
                        columns.ConstantColumn(42);   // İstenen
                        columns.ConstantColumn(46);   // 3K Gelen
                        columns.ConstantColumn(46);   // Karşılanan
                        columns.ConstantColumn(46);   // Geri Gönd.
                        columns.ConstantColumn(55);   // Kaynak Proje
                        columns.ConstantColumn(55);   // Hedef Saha
                        columns.ConstantColumn(52);   // Prj. Alındı
                        columns.ConstantColumn(52);   // Prj. Verildi
                        columns.ConstantColumn(42);   // Kalan
                        columns.RelativeColumn(1.35f); // 3K Durum
                        columns.RelativeColumn(1);    // Sürec
                        columns.RelativeColumn(1.7f); // Grid / 3K açıklamaları
                    });

                    // Başlık satırı (TR + EN)
                    table.Header(header =>
                    {
                        void BiHeader(IContainer c, string tr, string en = "")
                        {
                            c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(3).Column(col =>
                            {
                                col.Item().Text(tr).Bold().FontSize(7).FontColor(headerText);
                                if (!string.IsNullOrEmpty(en))
                                    col.Item().Text(en).FontSize(5).FontColor(Colors.Grey.Lighten3).Italic();
                            });
                        }

                        BiHeader(header.Cell(), "#");
                        BiHeader(header.Cell(), "SIRA", "Seq.");
                        BiHeader(header.Cell(), "BARKOD NO", "Barcode No");
                        BiHeader(header.Cell(), "POZ NO", "Pos. No");
                        BiHeader(header.Cell(), "ÜRÜN AÇIKLAMASI", "Product Desc.");
                        BiHeader(header.Cell(), "SANDIK", "Crate");
                        BiHeader(header.Cell(), "İSTENEN", "Requested");
                        BiHeader(header.Cell(), "3K GELEN", "3K Received");
                        BiHeader(header.Cell(), "KARŞI.", "Fulfilled");
                        BiHeader(header.Cell(), "GERİ GÖN.", "Returned");
                        BiHeader(header.Cell(), "KAYNAK PROJE", "Source Prj.");
                        BiHeader(header.Cell(), "HEDEF SAHA", "Target Site");
                        BiHeader(header.Cell(), "PRJ. ALINDI", "From Prj.");
                        BiHeader(header.Cell(), "PRJ. VERİLDİ", "Given to Prj.");
                        BiHeader(header.Cell(), "KALAN", "Remaining");
                        BiHeader(header.Cell(), "3K DURUM", "3K Status");
                        BiHeader(header.Cell(), "SÜREÇ", "Process");
                        BiHeader(header.Cell(), "AÇIKLAMALAR", "Grid / 3K Notes");
                    });

                    // Data satırları
                    int sira = 1;
                    foreach (var cs in raporSatirlari)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";
                        var karsilanan = cs.StokKarsilanan + cs.ProjeKarsilanan + cs.TedarikciKarsilanan;
                        var ucKDurum = cs.UcKDurumLookup?.Deger ?? "-";
                        var sandikNo = GetEksikRaporSandikMetni(cs, proje);
                        var kalan = RaporSorgulari.GetEksikRaporKalan(cs, tamamlamaPlanMap);

                        void DataCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                        {
                            var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(2);
                            if (bold)
                                cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                            else
                                cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black);
                        }

                        DataCell(table.Cell(), sira.ToString());
                        DataCell(table.Cell(), cs.SiraNo.ToString());
                        DataCell(table.Cell(), cs.BarkodNo);
                        DataCell(table.Cell(), string.IsNullOrWhiteSpace(cs.OlcuResmiPozNo) ? "-" : cs.OlcuResmiPozNo);
                        DataCell(table.Cell(), cs.Aciklama);
                        DataCell(table.Cell(), sandikNo);
                        DataCell(table.Cell(), RaporBicimleri.FormatAdet(cs.IstenenAdet));
                        DataCell(table.Cell(), RaporBicimleri.FormatAdet(cs.GelenMiktar));
                        DataCell(table.Cell(), karsilanan > 0 ? RaporBicimleri.FormatAdet(karsilanan) : "-");
                        DataCell(table.Cell(), cs.GeriGonderilenMiktar > 0 ? RaporBicimleri.FormatAdet(cs.GeriGonderilenMiktar) : "-");
                        var kaynakProje = GetKaynakProjeRaporMetni(cs, kaynakProjeMap);
                        var hedefSaha = GetHedefSahaRaporMetni(cs, hedefSahaMap);
                        var projedenAlindi = GetProjedenAlinanRaporMetni(cs, projedenAlinanMap);
                        var projeyeVerildi = GetProjeyeVerilenRaporMetni(cs, projeyeVerilenMap);
                        DataCell(table.Cell(), kaynakProje, fontColor: kaynakProje != "-" ? "#1565C0" : null);
                        DataCell(table.Cell(), hedefSaha, fontColor: hedefSaha != "-" ? "#00897B" : null);
                        DataCell(table.Cell(), projedenAlindi, fontColor: projedenAlindi != "-" ? "#2E7D32" : null);
                        DataCell(table.Cell(), projeyeVerildi, fontColor: projeyeVerildi != "-" ? "#1565C0" : null);
                        DataCell(table.Cell(), RaporBicimleri.FormatAdet(kalan), bold: true, fontColor: kalan > 0 ? dangerColor : "#2E7D32");
                        DataCell(table.Cell(), ucKDurum, fontColor: cs.UcKKarsilamaTipiId == (int)_3K.Core.Enums.UcKDurum.Bekliyor ? dangerColor : warningColor);
                        DataCell(table.Cell(), cs.SurecDurumLookup?.Deger ?? "-");
                        DataCell(table.Cell(), RaporBicimleri.GetRaporAciklama(cs));

                        sira++;
                    }
                });

                // ===== FOOTER =====
                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().Text($"3K Ambalaj – {raporBasligi} | Proje: {proje.ProjeNo}").FontSize(7).FontColor(Colors.Grey.Medium);
                    footer.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                        x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        });

        using var pdfStream = new MemoryStream();
        document.GeneratePdf(pdfStream);
        return pdfStream.ToArray();
    }

    public async Task<byte[]> EksikUrunlerRaporuExcelOlusturAsync(int projeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var proje = await context.Projeler
            .AsNoTracking()
            .Include(p => p.ProjeTipiLookup)
            .FirstOrDefaultAsync(p => p.Id == projeId, cancellationToken);

        if (proje == null)
            throw new KayitBulunamadiException($"Proje bulunamadı: {projeId}");

        var satirlar = await context.CekiSatirlari
            .AsNoTracking()
            .Include(cs => cs.BirimLookup)
            .Include(cs => cs.GridDurumLookup)
            .Include(cs => cs.UcKDurumLookup)
            .Include(cs => cs.DurumLookup)
            .Include(cs => cs.GeriGonderilmeSebebiLookup)
            .Include(cs => cs.SurecDurumLookup)
            .Include(cs => cs.SandikIcerikleri).ThenInclude(si => si.Sandik)
            .Where(cs => cs.Ceki.ProjeId == projeId)
            .ToListAsync(cancellationToken);

        var tamamlamaPlanMap = await context.GetTamamlamaPlanMapAsync(satirlar, sadeceSevkEdilenSandiklar: false, cancellationToken);
        var sahaEksikRaporuMu = proje.ProjeTipiId == (int)ProjeTipi.Saha;
        var aktifSahaAktarimKaynakIds = sahaEksikRaporuMu
            ? new HashSet<int>()
            : await GetAktifSahaAktarimKaynakSatirIdsAsync(satirlar.Select(s => s.Id), cancellationToken);

        var raporSatirlari = satirlar
            .Where(cs => sahaEksikRaporuMu ||
                RaporSorgulari.GetEksikRaporKalan(cs, tamamlamaPlanMap) > 0 ||
                aktifSahaAktarimKaynakIds.Contains(cs.Id))
            .OrderBy(cs => GetEksikRaporSandikSortKey(cs, projeId), SandikNumarasiComparer.Instance)
            .ThenBy(cs => cs.SiraNo)
            .ThenBy(cs => cs.Id)
            .ToList();

        var toplamRaporUrun = raporSatirlari.Count;
        var toplamEksikUrun = raporSatirlari.Count(s => RaporSorgulari.GetEksikRaporKalan(s, tamamlamaPlanMap) > 0);
        var toplamTamamlananUrun = toplamRaporUrun - toplamEksikUrun;
        var toplamKalanAdet = raporSatirlari.Sum(s => RaporSorgulari.GetEksikRaporKalan(s, tamamlamaPlanMap));
        var toplamIstenenAdet = raporSatirlari.Sum(s => s.IstenenAdet);
        var toplamGelenAdet = raporSatirlari.Sum(s => s.GelenMiktar + s.StokKarsilanan + s.ProjeKarsilanan + s.TedarikciKarsilanan);
        var karsilamaOrani = toplamIstenenAdet > 0 ? toplamGelenAdet * 100 / toplamIstenenAdet : 0;
        var (projedenAlinanMap, projeyeVerilenMap) = await GetProjeTransferRaporMapleriAsync(raporSatirlari, cancellationToken);
        var (kaynakProjeMap, hedefSahaMap) = await GetSahaAktarimRaporMapleriAsync(raporSatirlari, cancellationToken);
        var raporBasligi = sahaEksikRaporuMu ? "Sevk Sonrası Eksik Raporu" : "Eksik Ürünler Raporu";

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sahaEksikRaporuMu ? "Sevk Sonrasi Eksik" : "Eksik Raporu");
        const int lastColumn = 18;
        const int headerRow = 6;

        worksheet.Cell(1, 1).Value = raporBasligi;
        RaporExcelStilleri.StyleExcelTitle(worksheet, lastColumn);

        worksheet.Cell(2, 1).Value = "Proje No";
        worksheet.Cell(2, 2).Value = proje.ProjeNo;
        worksheet.Cell(2, 4).Value = "Müşteri";
        worksheet.Cell(2, 5).Value = proje.Musteri ?? "-";
        worksheet.Cell(2, 8).Value = "Rapor Tarihi";
        worksheet.Cell(2, 9).Value = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");

        if (sahaEksikRaporuMu)
        {
            worksheet.Cell(3, 1).Value = "Toplam Ürün";
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(3, 2), toplamRaporUrun);
            worksheet.Cell(3, 4).Value = "Durum";
            worksheet.Cell(3, 5).Value = $"Eksik: {toplamEksikUrun} / Tamamlanan: {toplamTamamlananUrun}";
            worksheet.Cell(3, 8).Value = "Toplam Kalan";
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(3, 9), toplamKalanAdet);
            worksheet.Cell(3, 11).Value = "Karşılama Oranı";
            worksheet.Cell(3, 12).Value = $"%{RaporBicimleri.FormatAdet(karsilamaOrani)}";
        }
        else
        {
            worksheet.Cell(3, 1).Value = "Eksik Ürün";
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(3, 2), toplamEksikUrun);
            worksheet.Cell(3, 4).Value = "Toplam Kalan";
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(3, 5), toplamKalanAdet);
            worksheet.Cell(3, 8).Value = "Karşılama Oranı";
            worksheet.Cell(3, 9).Value = $"%{RaporBicimleri.FormatAdet(karsilamaOrani)}";
        }
        RaporExcelStilleri.StyleExcelInfoRows(worksheet, lastColumn);

        var headers = new[]
        {
            "#", "Sıra", "Barkod No", "Poz No", "Ürün Açıklaması", "Sandık",
            "İstenen", "3K Gelen", "Karşılanan", "Geri Gön.", "Kaynak Proje",
            "Hedef Saha", "Prj. Alındı", "Prj. Verildi", "Kalan", "3K Durum",
            "Süreç", "Grid / 3K Açıklamaları"
        };

        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(headerRow, i + 1).Value = headers[i];

        RaporExcelStilleri.StyleExcelHeader(worksheet, headerRow, lastColumn);

        var row = headerRow + 1;
        var sira = 1;
        foreach (var cs in raporSatirlari)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var karsilanan = cs.StokKarsilanan + cs.ProjeKarsilanan + cs.TedarikciKarsilanan;
            var sandikNo = GetEksikRaporSandikMetni(cs, proje);
            var kaynakProje = GetKaynakProjeRaporMetni(cs, kaynakProjeMap);
            var hedefSaha = GetHedefSahaRaporMetni(cs, hedefSahaMap);
            var projedenAlindi = GetProjedenAlinanRaporMetni(cs, projedenAlinanMap);
            var projeyeVerildi = GetProjeyeVerilenRaporMetni(cs, projeyeVerilenMap);
            var kalan = RaporSorgulari.GetEksikRaporKalan(cs, tamamlamaPlanMap);

            worksheet.Cell(row, 1).SetValue(sira);
            worksheet.Cell(row, 2).SetValue(cs.SiraNo);
            worksheet.Cell(row, 3).Value = cs.BarkodNo;
            worksheet.Cell(row, 4).Value = string.IsNullOrWhiteSpace(cs.OlcuResmiPozNo) ? "-" : cs.OlcuResmiPozNo;
            worksheet.Cell(row, 5).Value = cs.Aciklama;
            worksheet.Cell(row, 6).Value = sandikNo;
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(row, 7), cs.IstenenAdet);
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(row, 8), cs.GelenMiktar);
            if (karsilanan > 0) RaporExcelStilleri.SetDecimalCell(worksheet.Cell(row, 9), karsilanan); else worksheet.Cell(row, 9).Value = "-";
            if (cs.GeriGonderilenMiktar > 0) RaporExcelStilleri.SetDecimalCell(worksheet.Cell(row, 10), cs.GeriGonderilenMiktar); else worksheet.Cell(row, 10).Value = "-";
            worksheet.Cell(row, 11).Value = kaynakProje;
            worksheet.Cell(row, 12).Value = hedefSaha;
            worksheet.Cell(row, 13).Value = projedenAlindi;
            worksheet.Cell(row, 14).Value = projeyeVerildi;
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(row, 15), kalan);
            worksheet.Cell(row, 15).Style.Font.Bold = true;
            worksheet.Cell(row, 15).Style.Font.FontColor = XLColor.FromHtml(kalan > 0 ? "#D32F2F" : "#2E7D32");
            worksheet.Cell(row, 16).Value = cs.UcKDurumLookup?.Deger ?? "-";
            worksheet.Cell(row, 17).Value = cs.SurecDurumLookup?.Deger ?? "-";
            worksheet.Cell(row, 18).Value = RaporBicimleri.GetRaporAciklama(cs);

            if (sira % 2 == 0)
                worksheet.Range(row, 1, row, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFE");

            row++;
            sira++;
        }

        worksheet.Column(5).Style.Alignment.WrapText = true;
        worksheet.Column(6).Style.Alignment.WrapText = true;
        worksheet.Column(18).Style.Alignment.WrapText = true;
        RaporExcelStilleri.FinalizeExcelWorksheet(worksheet, headerRow, Math.Max(row - 1, headerRow), lastColumn);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static List<Sandik> GetEksikRaporSandiklari(CekiSatiri satir, int projeId) =>
        satir.SandikIcerikleri
            .Where(si => si.Sandik != null && si.Sandik.ProjeId == projeId)
            .Select(si => si.Sandik)
            .DistinctBy(s => s.Id)
            .OrderBy(s => s.SandikNo, SandikNumarasiComparer.Instance)
            .ThenBy(s => s.Id)
            .ToList();

    private static string? GetEksikRaporSandikSortKey(CekiSatiri satir, int projeId) =>
        GetEksikRaporSandiklari(satir, projeId).FirstOrDefault()?.SandikNo
            ?? satir.FiiliSandikNo ?? satir.CekideGecenSandikNo;

    internal static string GetEksikRaporSandikMetni(CekiSatiri satir, Proje proje)
    {
        var sandiklar = GetEksikRaporSandiklari(satir, proje.Id);
        var adGoster = proje.ProjeTipiId is (int)ProjeTipi.Saha or (int)ProjeTipi.Yedek;
        if (sandiklar.Count > 0)
            // Her ilişki kendi numara/adıyla gösterilir; ürün satırı ve miktarı çoğaltılmaz.
            return string.Join("\n", sandiklar.Select(s => adGoster
                ? $"{RaporBicimleri.RaporMetni(s.SandikNo)}: {RaporBicimleri.GetRaporSandikAdi(s)}"
                : RaporBicimleri.RaporMetni(s.SandikNo)));

        var eskiSandikNo = RaporBicimleri.RaporMetni(satir.FiiliSandikNo ?? satir.CekideGecenSandikNo);
        return adGoster ? $"{eskiSandikNo}: -" : eskiSandikNo;
    }

    private async Task<HashSet<int>> GetAktifSahaAktarimKaynakSatirIdsAsync(IEnumerable<int> cekiSatiriIds, CancellationToken cancellationToken = default)
    {
        var satirIds = cekiSatiriIds.Where(id => id > 0).Distinct().ToList();
        if (satirIds.Count == 0)
            return new HashSet<int>();

        var kaynakSatirIds = await context.SahaAktarimKalemleri
            .AsNoTracking()
            .Where(k => satirIds.Contains(k.KaynakCekiSatiriId) &&
                k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                k.DurumId != (int)SahaAktarimDurum.Iptal)
            .Select(k => k.KaynakCekiSatiriId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var eskiKaynakSatirIds = await context.CekiSatirlari
            .AsNoTracking()
            .Where(cs => cs.KaynakCekiSatiriId.HasValue &&
                satirIds.Contains(cs.KaynakCekiSatiriId.Value) &&
                cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha)
            .Select(cs => cs.KaynakCekiSatiriId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return kaynakSatirIds.Concat(eskiKaynakSatirIds).ToHashSet();
    }

    private async Task<(Dictionary<int, string> ProjedenAlinan, Dictionary<int, string> ProjeyeVerilen)> GetProjeTransferRaporMapleriAsync(
        IReadOnlyCollection<CekiSatiri> satirlar,
        CancellationToken cancellationToken = default)
    {
        var satirIds = satirlar.Select(s => s.Id).Distinct().ToList();
        if (!satirIds.Any())
            return (new Dictionary<int, string>(), new Dictionary<int, string>());

        var transferler = await context.ProjeTransferleri
            .AsNoTracking()
            .Include(t => t.KaynakProje)
            .Include(t => t.HedefProje)
            .Where(t => t.DurumId == (int)ProjeTransferDurum.Aktif &&
                (satirIds.Contains(t.KaynakCekiSatiriId) ||
                 (t.HedefCekiSatiriId.HasValue && satirIds.Contains(t.HedefCekiSatiriId.Value))))
            .ToListAsync(cancellationToken);

        var projedenAlinan = transferler
            .Where(t => t.HedefCekiSatiriId.HasValue && satirIds.Contains(t.HedefCekiSatiriId.Value))
            .GroupBy(t => t.HedefCekiSatiriId!.Value)
            .ToDictionary(
                g => g.Key,
                g => BuildProjeTransferRaporMetni(g, t => t.KaynakProje?.ProjeNo, "Alınan"));

        var projeyeVerilen = transferler
            .Where(t => satirIds.Contains(t.KaynakCekiSatiriId))
            .GroupBy(t => t.KaynakCekiSatiriId)
            .ToDictionary(
                g => g.Key,
                g => BuildProjeTransferRaporMetni(g, t => t.HedefProje?.ProjeNo, "Verilen"));

        return (projedenAlinan, projeyeVerilen);
    }

    private static string BuildProjeTransferRaporMetni(
        IEnumerable<ProjeTransfer> transferler,
        Func<ProjeTransfer, string?> projeNoSelector,
        string hareketEtiketi)
    {
        return string.Join(", ", transferler
            .GroupBy(t => string.IsNullOrWhiteSpace(projeNoSelector(t)) ? "?" : projeNoSelector(t)!.Trim())
            .OrderBy(g => g.Key)
            .Select(g => $"{hareketEtiketi}: {RaporBicimleri.FormatAdet(g.Sum(t => t.Miktar))} ({g.Key})"));
    }

    private async Task<(Dictionary<int, string> KaynakProje, Dictionary<int, string> HedefSaha)> GetSahaAktarimRaporMapleriAsync(
        IReadOnlyCollection<CekiSatiri> satirlar,
        CancellationToken cancellationToken = default)
    {
        var satirIds = satirlar
            .Where(s => s.Id > 0)
            .Select(s => s.Id)
            .Distinct()
            .ToList();

        if (!satirIds.Any())
            return (new Dictionary<int, string>(), new Dictionary<int, string>());

        var sahaAktarimlari = await context.SahaAktarimKalemleri
            .AsNoTracking()
            .Include(k => k.KaynakProje)
            .Include(k => k.SahaProje)
            .Where(k => k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                k.DurumId != (int)SahaAktarimDurum.Iptal &&
                (satirIds.Contains(k.KaynakCekiSatiriId) ||
                 (k.SahaCekiSatiriId.HasValue && satirIds.Contains(k.SahaCekiSatiriId.Value))))
            .ToListAsync(cancellationToken);

        var kaynakProje = sahaAktarimlari
            .Where(k => k.SahaCekiSatiriId.HasValue && satirIds.Contains(k.SahaCekiSatiriId.Value))
            .GroupBy(k => k.SahaCekiSatiriId!.Value)
            .ToDictionary(
                g => g.Key,
                g => BuildSahaAktarimRaporMetni(g, k => k.KaynakProje?.ProjeNo));

        var hedefSaha = sahaAktarimlari
            .Where(k => satirIds.Contains(k.KaynakCekiSatiriId))
            .GroupBy(k => k.KaynakCekiSatiriId)
            .ToDictionary(
                g => g.Key,
                g => BuildSahaAktarimRaporMetni(g, k => k.SahaProje?.ProjeNo));

        // Eski saha kayıtlarında hareket defteri bulunmasa da hedef satırın kaynak bağlantısı korunur.
        var eskiSahaKaynaklari = await context.CekiSatirlari
            .AsNoTracking()
            .Where(cs => satirIds.Contains(cs.Id) &&
                cs.KaynakCekiSatiriId.HasValue &&
                cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha)
            .Select(cs => new
            {
                SahaCekiSatiriId = cs.Id,
                KaynakProjeNo = cs.KaynakCekiSatiri!.Ceki.Proje.ProjeNo,
                cs.IstenenAdet
            })
            .ToListAsync(cancellationToken);

        foreach (var grup in eskiSahaKaynaklari.GroupBy(a => a.SahaCekiSatiriId))
        {
            if (kaynakProje.ContainsKey(grup.Key))
                continue;

            kaynakProje[grup.Key] = string.Join(", ", grup
                .GroupBy(a => string.IsNullOrWhiteSpace(a.KaynakProjeNo) ? "?" : a.KaynakProjeNo.Trim())
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key} ({RaporBicimleri.FormatAdet(g.Sum(a => a.IstenenAdet))})"));
        }

        // Kaynak proje raporunda da eski hedef saha satırları geriye dönük iz olarak kullanılır.
        var eskiSahaAktarimlari = await context.CekiSatirlari
            .AsNoTracking()
            .Where(cs => cs.KaynakCekiSatiriId.HasValue &&
                satirIds.Contains(cs.KaynakCekiSatiriId.Value) &&
                cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha)
            .Select(cs => new
            {
                KaynakCekiSatiriId = cs.KaynakCekiSatiriId!.Value,
                SahaProjeNo = cs.Ceki.Proje.ProjeNo,
                cs.IstenenAdet
            })
            .ToListAsync(cancellationToken);

        foreach (var grup in eskiSahaAktarimlari.GroupBy(a => a.KaynakCekiSatiriId))
        {
            if (hedefSaha.ContainsKey(grup.Key))
                continue;

            hedefSaha[grup.Key] = string.Join(", ", grup
                .GroupBy(a => string.IsNullOrWhiteSpace(a.SahaProjeNo) ? "?" : a.SahaProjeNo.Trim())
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key} ({RaporBicimleri.FormatAdet(g.Sum(a => a.IstenenAdet))})"));
        }

        return (kaynakProje, hedefSaha);
    }

    private static string BuildSahaAktarimRaporMetni(
        IEnumerable<SahaAktarimKalemi> aktarimlar,
        Func<SahaAktarimKalemi, string?> projeNoSelector)
    {
        return string.Join(", ", aktarimlar
            .GroupBy(k => string.IsNullOrWhiteSpace(projeNoSelector(k)) ? "?" : projeNoSelector(k)!.Trim())
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Key} ({RaporBicimleri.FormatAdet(g.Sum(k => k.Miktar))})"));
    }

    private static string GetProjedenAlinanRaporMetni(CekiSatiri satir, IReadOnlyDictionary<int, string> transferMap)
    {
        if (transferMap.TryGetValue(satir.Id, out var transferMetni) && !string.IsNullOrWhiteSpace(transferMetni))
            return transferMetni;

        if (satir.ProjeKarsilanan <= 0)
            return "-";

        var projeNo = string.IsNullOrWhiteSpace(satir.KaynakHedefProjeNo) ? "?" : satir.KaynakHedefProjeNo;
        return $"Alınan: {RaporBicimleri.FormatAdet(satir.ProjeKarsilanan)} ({projeNo})";
    }

    private static string GetProjeyeVerilenRaporMetni(CekiSatiri satir, IReadOnlyDictionary<int, string> transferMap)
    {
        if (transferMap.TryGetValue(satir.Id, out var transferMetni) && !string.IsNullOrWhiteSpace(transferMetni))
            return transferMetni;

        if (satir.ProjeGonderilen <= 0)
            return "-";

        var projeNo = string.IsNullOrWhiteSpace(satir.KaynakHedefProjeNo) ? "?" : satir.KaynakHedefProjeNo;
        return $"Verilen: {RaporBicimleri.FormatAdet(satir.ProjeGonderilen)} ({projeNo})";
    }

    private static string GetKaynakProjeRaporMetni(CekiSatiri satir, IReadOnlyDictionary<int, string> sahaAktarimMap)
    {
        if (sahaAktarimMap.TryGetValue(satir.Id, out var aktarimMetni) && !string.IsNullOrWhiteSpace(aktarimMetni))
            return aktarimMetni;

        // Hareket defteri oluşturulmadan önce üretilmiş saha satırlarında kaynak proje bilgisi satır üzerinde tutulur.
        if (satir.KaynakCekiSatiriId.HasValue && !string.IsNullOrWhiteSpace(satir.KaynakHedefProjeNo))
            return $"{satir.KaynakHedefProjeNo.Trim()} ({RaporBicimleri.FormatAdet(satir.IstenenAdet)})";

        return "-";
    }

    private static string GetHedefSahaRaporMetni(CekiSatiri satir, IReadOnlyDictionary<int, string> sahaAktarimMap)
    {
        return sahaAktarimMap.TryGetValue(satir.Id, out var aktarimMetni) && !string.IsNullOrWhiteSpace(aktarimMetni)
            ? aktarimMetni
            : "-";
    }
}
