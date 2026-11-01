using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Helpers;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
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
    public async Task<byte[]> EksikUrunlerRaporuPdfOlusturAsync(int projeId, CancellationToken cancellationToken = default) =>
        PdfOlustur(await EksikRaporVerisiOlusturAsync(projeId, cancellationToken), cancellationToken);

    /// <summary>PDF ile aynı veri ve aynı bölümlerle (saha raporunda Saha İzi dahil) Excel raporu.</summary>
    public async Task<byte[]> EksikUrunlerRaporuExcelOlusturAsync(int projeId, CancellationToken cancellationToken = default) =>
        ExcelOlustur(await EksikRaporVerisiOlusturAsync(projeId, cancellationToken), cancellationToken);

    /// <summary>
    /// PDF ve Excel'in tek veri kaynağı: satır seçimi, sıralama, hücre metinleri, özet ve saha izi burada bir kez
    /// hesaplanır; iki çıktı yalnız biçimlendirir, içerik ayrı ayrı yazılmaz.
    /// </summary>
    internal async Task<EksikRaporVerisi> EksikRaporVerisiOlusturAsync(int projeId, CancellationToken cancellationToken)
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

        var (projedenAlinanMap, projeyeVerilenMap) = await GetProjeTransferRaporMapleriAsync(raporSatirlari, cancellationToken);
        var (kaynakProjeMap, hedefSahaMap) = await GetSahaAktarimRaporMapleriAsync(raporSatirlari, cancellationToken);
        // R-07: SEVK ürünün bu projenin kendi sevkiyatıyla gidip gitmediğidir; sahaya aktarılan kısım Hedef Saha
        // sütunundadır, sahadaki sevki saha projesinin raporundadır. Kalan/3K durumu iş tamamlanmasını gösterir.
        var tahsissizSandiklar = await GetTahsissizSatirSandiklariAsync(proje, raporSatirlari, cancellationToken);

        var modelSatirlari = raporSatirlari
            .Select((cs, index) => new EksikRaporSatiri(
                index + 1,
                cs.SiraNo,
                cs.BarkodNo,
                string.IsNullOrWhiteSpace(cs.OlcuResmiPozNo) ? "-" : cs.OlcuResmiPozNo,
                cs.Aciklama,
                GetEksikRaporSandikMetni(cs, proje),
                cs.IstenenAdet,
                cs.GelenMiktar,
                cs.StokKarsilanan + cs.ProjeKarsilanan + cs.TedarikciKarsilanan,
                cs.GeriGonderilenMiktar,
                GetKaynakProjeRaporMetni(cs, kaynakProjeMap),
                GetHedefSahaRaporMetni(cs, hedefSahaMap),
                GetProjedenAlinanRaporMetni(cs, projedenAlinanMap),
                GetProjeyeVerilenRaporMetni(cs, projeyeVerilenMap),
                RaporSorgulari.GetEksikRaporKalan(cs, tamamlamaPlanMap),
                UrunSevkDurumuKurali.Belirle(cs, proje.Id, tahsissizSandiklar.GetValueOrDefault(cs.Id)),
                cs.UcKDurumLookup?.Deger ?? "-",
                cs.UcKKarsilamaTipiId == (int)_3K.Core.Enums.UcKDurum.Bekliyor,
                cs.SurecDurumLookup?.Deger ?? "-",
                RaporBicimleri.GetRaporAciklama(cs)))
            .ToList();

        var sahaIzi = sahaEksikRaporuMu ? await GetSahaIziSatirlariAsync(proje.Id, cancellationToken) : [];
        return new EksikRaporVerisi(proje.ProjeNo, proje.Musteri, proje.FBNo, sahaEksikRaporuMu,
            TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm"), modelSatirlari, sahaIzi);
    }

    internal static byte[] PdfOlustur(EksikRaporVerisi veri, CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Renk paleti
        var headerBg = Colors.Blue.Darken3;
        var headerText = Colors.White;
        var dangerColor = "#D32F2F";
        var warningColor = "#F57C00";
        var tableBorderColor = Colors.Grey.Lighten2;
        var altRowBg = "#F8FAFE";

        var raporBasligi = veri.Baslik;
        var raporAltBasligi = veri.SahaRaporuMu
            ? "3K Sevkiyat Yönetim Sistemi - Saha Sevk Sonrası Eksik Çıktısı"
            : "3K Sevkiyat Yönetim Sistemi - Proje Eksik Ürün Çıktısı";
        var summaryAccentColor = veri.SahaRaporuMu ? "#1565C0" : dangerColor;

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
                            col.Item().Text($"Rapor Tarihi: {veri.RaporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                        });
                    });

                    // Proje bilgisi kartları
                    headerCol.Item().PaddingTop(10).PaddingBottom(5).Row(row =>
                    {
                        // Proje Bilgisi
                        row.RelativeItem().Border(1).BorderColor(tableBorderColor).Padding(8).Column(col =>
                        {
                            col.Item().Text("PROJE BİLGİSİ").Bold().FontSize(7).FontColor(Colors.Blue.Darken1);
                            col.Item().PaddingTop(3).Text(veri.ProjeNo).Bold().FontSize(13);
                            col.Item().Text($"Müşteri: {veri.Musteri}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(veri.FbNo))
                                col.Item().Text($"FB No: {veri.FbNo}").FontSize(8);
                        });

                        row.ConstantItem(10); // Spacer

                        // Özet
                        row.RelativeItem().Border(1).BorderColor(summaryAccentColor).Padding(8).Column(col =>
                        {
                            col.Item().Text(veri.SahaRaporuMu ? "DURUM ÖZET" : "EKSİK ÖZET").Bold().FontSize(7).FontColor(summaryAccentColor);
                            col.Item().PaddingTop(3).Text(veri.SahaRaporuMu ? $"{veri.ToplamUrun} ürün listeleniyor" : $"{veri.EksikUrun} ürün eksik").Bold().FontSize(13).FontColor(summaryAccentColor);
                            if (veri.SahaRaporuMu)
                                col.Item().Text($"Eksik: {veri.EksikUrun} / Tamamlanan: {veri.TamamlananUrun}").FontSize(8);
                            col.Item().Text($"Toplam Kalan: {RaporBicimleri.FormatAdet(veri.ToplamKalan)} adet").FontSize(8);
                            col.Item().Text($"Karşılama Oranı: %{RaporBicimleri.FormatAdet(veri.KarsilamaOrani)}").FontSize(8);
                            col.Item().Text($"Sevk (satır): {veri.SevkOzeti}").FontSize(8);
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
                        // SEVK sütunu için sayı/proje sütunları daraltıldı; metin sütunları önceki genişliğini korur.
                        columns.ConstantColumn(42);   // İstenen
                        columns.ConstantColumn(42);   // 3K Gelen
                        columns.ConstantColumn(40);   // Karşılanan
                        columns.ConstantColumn(42);   // Geri Gönd.
                        columns.ConstantColumn(45);   // Kaynak Proje
                        columns.ConstantColumn(45);   // Hedef Saha
                        columns.ConstantColumn(46);   // Prj. Alındı
                        columns.ConstantColumn(46);   // Prj. Verildi
                        columns.ConstantColumn(42);   // Kalan
                        columns.ConstantColumn(44);   // Sevk
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
                        BiHeader(header.Cell(), "SEVK", "Shipment");
                        BiHeader(header.Cell(), "3K DURUM", "3K Status");
                        BiHeader(header.Cell(), "SÜREÇ", "Process");
                        BiHeader(header.Cell(), "AÇIKLAMALAR", "Grid / 3K Notes");
                    });

                    // Data satırları: hücre metinleri Excel ile aynı kaynaktan (EksikRaporSatiri.Hucreler).
                    foreach (var s in veri.Satirlar)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var bg = s.No % 2 == 0 ? altRowBg : "#FFFFFF";
                        var hucreler = s.Hucreler();

                        void DataCell(int index, bool bold = false, string? fontColor = null)
                        {
                            var cell = table.Cell().Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(2);
                            if (bold)
                                cell.Text(hucreler[index]).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                            else
                                cell.Text(hucreler[index]).FontSize(7).FontColor(fontColor ?? Colors.Black);
                        }

                        for (var i = 0; i <= 9; i++)
                            DataCell(i);
                        DataCell(10, fontColor: s.KaynakProje != "-" ? "#1565C0" : null);
                        DataCell(11, fontColor: s.HedefSaha != "-" ? "#00897B" : null);
                        DataCell(12, fontColor: s.ProjedenAlindi != "-" ? "#2E7D32" : null);
                        DataCell(13, fontColor: s.ProjeyeVerildi != "-" ? "#1565C0" : null);
                        DataCell(14, bold: true, fontColor: s.Kalan > 0 ? dangerColor : "#2E7D32");
                        DataCell(15, bold: true, fontColor: RaporBicimleri.SevkRengi(s.Sevk?.Durum));
                        DataCell(16, fontColor: s.UcKBekliyor ? dangerColor : warningColor);
                        DataCell(17);
                        DataCell(18);
                    }
                });

                // ===== FOOTER =====
                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().Text($"3K Ambalaj – {raporBasligi} | Proje: {veri.ProjeNo}").FontSize(7).FontColor(Colors.Grey.Medium);
                    footer.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                        x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            // Saha izi: kaynak projelerin sahaya giden her ürünü hangi saha ve sandık üzerinden gitti
            // (Excel'de aynı sütunlarla ana tablonun altında).
            if (veri.SahaIzi.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Background(headerBg).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(SahaIziBasligi).Bold().FontSize(14).FontColor(headerText);
                            col.Item().Text(SahaIziAciklamasi).FontSize(7).FontColor(Colors.Grey.Lighten3);
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text(veri.ProjeNo).Bold().FontSize(11).FontColor(headerText);
                            col.Item().Text(veri.SahaIziOzeti).FontSize(7).FontColor(Colors.Grey.Lighten3);
                        });
                    });

                    page.Content().PaddingVertical(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);    // #
                            columns.ConstantColumn(28);    // Sıra
                            columns.RelativeColumn(1.1f);  // Barkod No
                            columns.RelativeColumn(2.2f);  // Ürün Açıklaması
                            columns.ConstantColumn(44);    // Sandık
                            columns.ConstantColumn(42);    // Miktar
                            columns.ConstantColumn(60);    // Kaynak Proje
                            columns.ConstantColumn(70);    // Gittiği Saha
                            columns.RelativeColumn(2.2f);  // Saha İzi
                            columns.RelativeColumn(1.1f);  // Durum
                        });
                        table.Header(header =>
                        {
                            foreach (var baslik in SahaIziBasliklari)
                                header.Cell().Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(3)
                                    .Text(baslik.ToUpper(new System.Globalization.CultureInfo("tr-TR"))).Bold().FontSize(7).FontColor(headerText);
                        });

                        var no = 1;
                        foreach (var s in veri.SahaIzi)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var bg = no % 2 == 0 ? altRowBg : "#FFFFFF";
                            var hucreler = s.Hucreler(no);
                            for (var i = 0; i < hucreler.Length; i++)
                            {
                                var (renk, kalin) = SahaIziHucreStili(s, i);
                                var text = table.Cell().Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(2)
                                    .Text(hucreler[i]).FontSize(7).FontColor(renk ?? Colors.Black);
                                if (kalin) text.Bold();
                            }
                            no++;
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                        x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            }
        });

        using var pdfStream = new MemoryStream();
        document.GeneratePdf(pdfStream);
        return pdfStream.ToArray();
    }

    internal static byte[] ExcelOlustur(EksikRaporVerisi veri, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(veri.SahaRaporuMu ? "Sevk Sonrasi Eksik" : "Eksik Raporu");
        const int lastColumn = 19;
        const int headerRow = 6;

        worksheet.Cell(1, 1).Value = veri.Baslik;
        RaporExcelStilleri.StyleExcelTitle(worksheet, lastColumn);

        worksheet.Cell(2, 1).Value = "Proje No";
        worksheet.Cell(2, 2).Value = veri.ProjeNo;
        worksheet.Cell(2, 4).Value = "Müşteri";
        worksheet.Cell(2, 5).Value = string.IsNullOrWhiteSpace(veri.Musteri) ? "-" : veri.Musteri;
        worksheet.Cell(2, 8).Value = "Rapor Tarihi";
        worksheet.Cell(2, 9).Value = veri.RaporTarihi;
        if (!string.IsNullOrWhiteSpace(veri.FbNo))
        {
            worksheet.Cell(2, 11).Value = "FB No";
            worksheet.Cell(2, 12).Value = veri.FbNo;
        }

        if (veri.SahaRaporuMu)
        {
            worksheet.Cell(3, 1).Value = "Toplam Ürün";
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(3, 2), veri.ToplamUrun);
            worksheet.Cell(3, 4).Value = "Durum";
            worksheet.Cell(3, 5).Value = $"Eksik: {veri.EksikUrun} / Tamamlanan: {veri.TamamlananUrun}";
            worksheet.Cell(3, 8).Value = "Toplam Kalan";
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(3, 9), veri.ToplamKalan);
            worksheet.Cell(3, 11).Value = "Karşılama Oranı";
            worksheet.Cell(3, 12).Value = $"%{RaporBicimleri.FormatAdet(veri.KarsilamaOrani)}";
        }
        else
        {
            worksheet.Cell(3, 1).Value = "Eksik Ürün";
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(3, 2), veri.EksikUrun);
            worksheet.Cell(3, 4).Value = "Toplam Kalan";
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(3, 5), veri.ToplamKalan);
            worksheet.Cell(3, 8).Value = "Karşılama Oranı";
            worksheet.Cell(3, 9).Value = $"%{RaporBicimleri.FormatAdet(veri.KarsilamaOrani)}";
        }
        worksheet.Cell(3, 14).Value = "Sevk (satır)";
        worksheet.Cell(3, 15).Value = veri.SevkOzeti;
        RaporExcelStilleri.StyleExcelInfoRows(worksheet, lastColumn);

        for (var i = 0; i < AnaTabloBasliklari.Length; i++)
            worksheet.Cell(headerRow, i + 1).Value = AnaTabloBasliklari[i];

        RaporExcelStilleri.StyleExcelHeader(worksheet, headerRow, lastColumn);

        var row = headerRow + 1;
        foreach (var s in veri.Satirlar)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hucreler = s.Hucreler();
            // Sayılar Excel'de sayı olarak kalır; metin hücreleri PDF'teki metnin aynısıdır.
            worksheet.Cell(row, 1).SetValue(s.No);
            worksheet.Cell(row, 2).SetValue(s.SiraNo);
            for (var i = 2; i <= 5; i++)
                worksheet.Cell(row, i + 1).Value = hucreler[i];
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(row, 7), s.Istenen);
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(row, 8), s.Gelen);
            if (s.Karsilanan > 0) RaporExcelStilleri.SetDecimalCell(worksheet.Cell(row, 9), s.Karsilanan); else worksheet.Cell(row, 9).Value = hucreler[8];
            if (s.GeriGonderilen > 0) RaporExcelStilleri.SetDecimalCell(worksheet.Cell(row, 10), s.GeriGonderilen); else worksheet.Cell(row, 10).Value = hucreler[9];
            for (var i = 10; i <= 13; i++)
                worksheet.Cell(row, i + 1).Value = hucreler[i];
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(row, 15), s.Kalan);
            worksheet.Cell(row, 15).Style.Font.Bold = true;
            worksheet.Cell(row, 15).Style.Font.FontColor = XLColor.FromHtml(s.Kalan > 0 ? "#D32F2F" : "#2E7D32");
            RaporExcelStilleri.SetSevkCell(worksheet.Cell(row, 16), hucreler[15], s.Sevk?.Durum);
            for (var i = 16; i <= 18; i++)
                worksheet.Cell(row, i + 1).Value = hucreler[i];

            if (s.No % 2 == 0)
                worksheet.Range(row, 1, row, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFE");

            row++;
        }

        worksheet.Column(5).Style.Alignment.WrapText = true;
        worksheet.Column(6).Style.Alignment.WrapText = true;
        worksheet.Column(19).Style.Alignment.WrapText = true;
        var anaTabloSonSatiri = Math.Max(row - 1, headerRow);

        // Saha izi: PDF'teki ayrı sayfanın aynı sütun ve metinleri, ayrı çalışma sayfası yerine ana tablonun hemen
        // altında başlıklı bölüm (ayrı sekme gözden kaçıyordu).
        var izSatirlari = veri.SahaIzi.Count > 0
            ? SahaIziBolumunuYaz(worksheet, veri, anaTabloSonSatiri + 2, lastColumn, cancellationToken)
            : [];

        RaporExcelStilleri.FinalizeExcelWorksheet(worksheet, headerRow, anaTabloSonSatiri, lastColumn);
        // Uzun sevk özeti (3,15) "Kalan" sayı sütununu genişletmesin; metin boş komşu hücrelere taşar.
        worksheet.Column(15).AdjustToContents(headerRow);
        foreach (var izSatiri in izSatirlari)
            RaporExcelStilleri.BirlesikSatirYuksekliginiAyarla(worksheet, izSatiri,
                SahaIziExcelSutunlari.Select(s => (s.Ilk, s.Son)));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Saha İzi bölümünü <paramref name="baslangicSatiri"/>'ndan başlayarak yazar; sütunlar ana tablonun aynı anlamlı
    /// sütunlarının altına (Sıra, Barkod, Sandık, Kaynak Proje, Hedef Saha) gelecek şekilde birleştirilir. Veri
    /// satırlarının numaralarını döndürür.
    /// </summary>
    private static List<int> SahaIziBolumunuYaz(
        IXLWorksheet worksheet, EksikRaporVerisi veri, int baslangicSatiri, int lastColumn, CancellationToken cancellationToken)
    {
        var baslikSatiri = baslangicSatiri;
        worksheet.Cell(baslikSatiri, 1).Value = $"{SahaIziBasligi} — {veri.ProjeNo}";
        RaporExcelStilleri.StyleExcelBolumBasligi(worksheet, baslikSatiri, lastColumn);
        RaporExcelStilleri.SetNotSatiri(worksheet, baslikSatiri + 1, lastColumn, $"{SahaIziAciklamasi} · {veri.SahaIziOzeti}");
        worksheet.Row(baslikSatiri + 1).Height = 16;

        var tabloBasligi = baslikSatiri + 2;
        for (var i = 0; i < SahaIziBasliklari.Length; i++)
            HucreYaz(worksheet, tabloBasligi, i, SahaIziBasliklari[i]);
        RaporExcelStilleri.StyleExcelHeader(worksheet, tabloBasligi, lastColumn);

        var satirlar = new List<int>();
        var satir = tabloBasligi + 1;
        var no = 1;
        foreach (var s in veri.SahaIzi)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hucreler = s.Hucreler(no);
            for (var i = 0; i < hucreler.Length; i++)
            {
                var hucre = i switch
                {
                    0 => worksheet.Cell(satir, 1).SetValue(no),
                    1 => worksheet.Cell(satir, SahaIziExcelSutunlari[1].Ilk).SetValue(s.SiraNo),
                    5 => SayiYaz(worksheet.Cell(satir, SahaIziExcelSutunlari[5].Ilk), s.Miktar),
                    _ => HucreYaz(worksheet, satir, i, hucreler[i])
                };
                var (renk, kalin) = SahaIziHucreStili(s, i);
                if (renk != null) hucre.Style.Font.FontColor = XLColor.FromHtml(renk);
                if (kalin) hucre.Style.Font.Bold = true;
            }

            if (no % 2 == 0)
                worksheet.Range(satir, 1, satir, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFE");
            satirlar.Add(satir);
            satir++;
            no++;
        }

        foreach (var (ilk, son) in SahaIziExcelSutunlari.Where(s => s.Son > s.Ilk))
            for (var r = tabloBasligi; r < satir; r++)
                worksheet.Range(r, ilk, r, son).Merge();

        var veriAraligi = worksheet.Range(tabloBasligi + 1, 1, Math.Max(satir - 1, tabloBasligi + 1), lastColumn);
        veriAraligi.Style.Alignment.WrapText = true;
        veriAraligi.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        RaporExcelStilleri.TabloKenarlari(worksheet, tabloBasligi, Math.Max(satir - 1, tabloBasligi), lastColumn);
        return satirlar;

        static IXLCell SayiYaz(IXLCell hucre, decimal deger)
        {
            RaporExcelStilleri.SetDecimalCell(hucre, deger);
            return hucre;
        }
    }

    private static IXLCell HucreYaz(IXLWorksheet worksheet, int satir, int sutunIndex, string metin)
    {
        var hucre = worksheet.Cell(satir, SahaIziExcelSutunlari[sutunIndex].Ilk);
        hucre.Value = metin;
        return hucre;
    }

    /// <summary>Saha İzi hücre rengi/kalınlığı (PDF ve Excel ortak): kaynak proje mavi, saha yeşil-mavi, durum sevke göre.</summary>
    private static (string? Renk, bool Kalin) SahaIziHucreStili(SahaIziSatiri satir, int sutunIndex) => sutunIndex switch
    {
        6 => ("#1565C0", false),
        7 => ("#00897B", true),
        9 => (satir.SevkEdildi ? "#2E7D32" : "#F57C00", true),
        _ => (null, false)
    };

    /// <summary>Eksik raporunun Excel ana tablo başlıkları (PDF'te aynı sırada TR/EN başlıklar).</summary>
    internal static readonly string[] AnaTabloBasliklari =
    [
        "#", "Sıra", "Barkod No", "Poz No", "Ürün Açıklaması", "Sandık",
        "İstenen", "3K Gelen", "Karşılanan", "Geri Gön.", "Kaynak Proje",
        "Hedef Saha", "Prj. Alındı", "Prj. Verildi", "Kalan", "Sevk", "3K Durum",
        "Süreç", "Grid / 3K Açıklamaları"
    ];

    /// <summary>PDF ve Excel'in ortak eksik raporu verisi; hesaplar burada, biçim çıktılarda.</summary>
    internal sealed record EksikRaporVerisi(
        string ProjeNo,
        string? Musteri,
        string? FbNo,
        bool SahaRaporuMu,
        string RaporTarihi,
        IReadOnlyList<EksikRaporSatiri> Satirlar,
        IReadOnlyList<SahaIziSatiri> SahaIzi)
    {
        public string Baslik => SahaRaporuMu ? "Sevk Sonrası Eksik Raporu" : "Eksik Ürünler Raporu";
        public int ToplamUrun => Satirlar.Count;
        public int EksikUrun => Satirlar.Count(s => s.Kalan > 0);
        public int TamamlananUrun => ToplamUrun - EksikUrun;
        public decimal ToplamKalan => Satirlar.Sum(s => s.Kalan);

        public decimal KarsilamaOrani
        {
            get
            {
                var istenen = Satirlar.Sum(s => s.Istenen);
                return istenen > 0 ? Satirlar.Sum(s => s.Gelen + s.Karsilanan) * 100 / istenen : 0;
            }
        }

        public string SevkOzeti => UrunSevkDurumuKurali.OzetMetni(Satirlar.Select(s => s.Sevk));
        public string SahaIziOzeti => $"{SahaIzi.Count} ürün · sevk edilen {SahaIzi.Count(s => s.SevkEdildi)}";
    }

    /// <summary>Eksik raporunun bir ürün satırı; <see cref="Hucreler"/> iki çıktının da yazdığı metinlerdir.</summary>
    internal sealed record EksikRaporSatiri(
        int No,
        int SiraNo,
        string BarkodNo,
        string PozNo,
        string Aciklama,
        string Sandik,
        decimal Istenen,
        decimal Gelen,
        decimal Karsilanan,
        decimal GeriGonderilen,
        string KaynakProje,
        string HedefSaha,
        string ProjedenAlindi,
        string ProjeyeVerildi,
        decimal Kalan,
        UrunSevkSonucu? Sevk,
        string UcKDurum,
        bool UcKBekliyor,
        string Surec,
        string Aciklamalar)
    {
        internal string[] Hucreler() =>
        [
            No.ToString(), SiraNo.ToString(), BarkodNo, PozNo, Aciklama, Sandik,
            RaporBicimleri.FormatAdet(Istenen), RaporBicimleri.FormatAdet(Gelen),
            Karsilanan > 0 ? RaporBicimleri.FormatAdet(Karsilanan) : "-",
            GeriGonderilen > 0 ? RaporBicimleri.FormatAdet(GeriGonderilen) : "-",
            KaynakProje, HedefSaha, ProjedenAlindi, ProjeyeVerildi,
            RaporBicimleri.FormatAdet(Kalan), RaporBicimleri.SevkMetni(Sevk), UcKDurum, Surec, Aciklamalar
        ];
    }

    /// <summary>Saha izi satırı: kaynak normal projenin sahaya gitmiş ürünü, şu anki saha/sandığı, taşıma zinciri ve sevki.</summary>
    internal sealed record SahaIziSatiri(
        string KaynakProjeNo,
        int SiraNo,
        string BarkodNo,
        string Aciklama,
        decimal Miktar,
        string SahaProjeNo,
        string SahaSandikNo,
        string Iz,
        string Durum,
        bool SevkEdildi)
    {
        /// <summary>PDF ve Excel'in yazdığı hücre metinleri, <see cref="SahaIziBasliklari"/> sırasıyla.</summary>
        internal string[] Hucreler(int no) =>
        [
            no.ToString(), SiraNo.ToString(), BarkodNo, Aciklama, SahaSandikNo, RaporBicimleri.FormatAdet(Miktar),
            KaynakProjeNo, SahaProjeNo, Iz, Durum
        ];
    }

    internal const string SahaIziBasligi = "Saha İzi";
    internal const string SahaIziAciklamasi = "Kaynak proje ürünlerinin gittiği saha, sandık ve sevk durumu";

    /// <summary>
    /// Saha İzi sütunları; sıra ana tablonunkiyle aynıdır (Sıra, Barkod, Ürün, Sandık, miktar, Kaynak Proje, saha)
    /// ki Excel'de bölüm ana tablonun aynı anlamlı sütunlarının altına otursun.
    /// </summary>
    internal static readonly string[] SahaIziBasliklari =
        ["#", "Sıra", "Barkod No", "Ürün Açıklaması", "Sandık", "Miktar", "Kaynak Proje", "Gittiği Saha", "Saha İzi", "Durum"];

    /// <summary>Excel'de her Saha İzi sütununun ana tablodaki sütun aralığı (birleştirilir).</summary>
    internal static readonly (int Ilk, int Son)[] SahaIziExcelSutunlari =
        [(1, 1), (2, 2), (3, 3), (4, 5), (6, 6), (7, 7), (8, 11), (12, 12), (13, 18), (19, 19)];

    /// <summary>
    /// Saha projesinin kaynak normal projelerinde sahaya aktarılmış her ürünün hangi saha ve sandık üzerinden gittiği:
    /// aktif aktarım kaydının bugünkü sahası ve sahadan sahaya taşıma izleri (<see cref="SahaTasimaKalemi"/>). Bu
    /// sahadan başka sahaya taşınan ürünler de görünür; kaynaklar bu saha projesine aktarılmış ya da taşınmış
    /// satırların kök projeleridir.
    /// </summary>
    internal async Task<List<SahaIziSatiri>> GetSahaIziSatirlariAsync(int sahaProjeId, CancellationToken cancellationToken)
    {
        var kokSatirIds = await context.SahaAktarimKalemleri.AsNoTracking()
            .Where(k => k.SahaProjeId == sahaProjeId)
            .Select(k => k.KaynakCekiSatiriId)
            .Union(context.SahaTasimaKalemleri.AsNoTracking()
                .Where(t => t.KaynakSahaProjeId == sahaProjeId || t.HedefSahaProjeId == sahaProjeId)
                .Select(t => t.KaynakCekiSatiriId))
            .Distinct()
            .ToListAsync(cancellationToken);
        if (kokSatirIds.Count == 0)
            return [];

        var kaynakProjeIds = await context.CekiSatirlari.AsNoTracking()
            .Where(cs => kokSatirIds.Contains(cs.Id))
            .Select(cs => cs.Ceki.ProjeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var kalemler = await context.SahaAktarimKalemleri.AsNoTracking()
            .Where(k => kaynakProjeIds.Contains(k.KaynakProjeId) &&
                k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                k.DurumId != (int)SahaAktarimDurum.Iptal)
            .Select(k => new
            {
                k.Id,
                KaynakProjeNo = k.KaynakProje.ProjeNo,
                k.KaynakCekiSatiri.SiraNo,
                k.KaynakCekiSatiri.BarkodNo,
                k.KaynakCekiSatiri.Aciklama,
                k.Miktar,
                SahaProjeNo = k.SahaProje.ProjeNo,
                SahaSandikNo = k.SahaSandik != null ? k.SahaSandik.SandikNo : null,
                k.DurumId,
                k.SevkTarihi
            })
            .ToListAsync(cancellationToken);

        var kalemIds = kalemler.Select(k => k.Id).ToList();
        var izler = (await context.SahaTasimaKalemleri.AsNoTracking()
                .Where(t => t.SahaAktarimKalemiId.HasValue && kalemIds.Contains(t.SahaAktarimKalemiId.Value))
                .ToListAsync(cancellationToken))
            .GroupBy(t => t.SahaAktarimKalemiId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Tarih).ThenBy(t => t.Id).ToList());

        return kalemler
            .Select(k =>
            {
                var sandikNo = RaporBicimleri.RaporMetni(k.SahaSandikNo);
                var iz = izler.TryGetValue(k.Id, out var tasimalar)
                    ? string.Join(" → ", tasimalar
                        .Select(t => $"{t.KaynakSahaProjeNo} ({RaporBicimleri.RaporMetni(t.KaynakSahaSandikNo)})")
                        .Append($"{k.SahaProjeNo} ({sandikNo})"))
                    : $"{k.SahaProjeNo} ({sandikNo})";
                var sevkEdildi = k.DurumId == (int)SahaAktarimDurum.SevkEdildi;
                var durum = sevkEdildi
                    ? k.SevkTarihi.HasValue ? $"Sevk edildi {k.SevkTarihi.Value:dd.MM.yyyy}" : "Sevk edildi"
                    : k.DurumId == (int)SahaAktarimDurum.SevkiyatDuzeltmede ? "Sevkiyat düzeltmede" : "Bekliyor";
                return new SahaIziSatiri(k.KaynakProjeNo, k.SiraNo, k.BarkodNo, k.Aciklama, k.Miktar,
                    k.SahaProjeNo, sandikNo, iz, durum, sevkEdildi);
            })
            .OrderBy(s => s.KaynakProjeNo, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.SiraNo)
            .ThenBy(s => s.SahaProjeNo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Sandık içeriği olmayan eski normal proje satırlarının sandığı (etkin içerik kuralı). Sahaya aktarım ve bu eski
    /// kayıtlar yalnız normal projede olduğundan saha/yedek raporunda boş döner.
    /// </summary>
    private async Task<IReadOnlyDictionary<int, Sandik>> GetTahsissizSatirSandiklariAsync(
        Proje proje,
        IEnumerable<CekiSatiri> satirlar,
        CancellationToken cancellationToken) =>
        proje.ProjeTipiId == (int)ProjeTipi.Normal
            ? await context.GetTahsissizSatirSandiklariAsync(proje.Id, satirlar, cancellationToken)
            : new Dictionary<int, Sandik>();

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
