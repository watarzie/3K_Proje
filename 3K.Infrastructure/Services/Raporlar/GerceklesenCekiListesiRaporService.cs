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

/// <summary>Gerçekleşen çeki listesi raporu (PDF ve Excel); saha projelerinde sandık içeriği bazlıdır.</summary>
internal sealed class GerceklesenCekiListesiRaporService(AppDbContext context) : IGerceklesenCekiListesiRaporService
{
    public async Task<byte[]> GerceklesenCekiListesiRaporuPdfOlusturAsync(int projeId)
    {
        var projeTipi = await GetRaporProjeTipiAsync(projeId);
        return await GerceklesenCekiListesiRaporuPdfOlusturAsync(projeId, projeTipi);
    }

    public Task<byte[]> SahaGerceklesenCekiListesiRaporuPdfOlusturAsync(int projeId)
        => GerceklesenCekiListesiRaporuPdfOlusturAsync(projeId, ProjeTipi.Saha);

    private async Task<byte[]> GerceklesenCekiListesiRaporuPdfOlusturAsync(int projeId, ProjeTipi raporProjeTipi)
    {
        var proje = await context.Projeler
            .Include(p => p.DurumLookup)
            .FirstOrDefaultAsync(p => p.Id == projeId);

        if (proje == null)
            throw new KayitBulunamadiException($"Proje bulunamadi: {projeId}");

        var sahaYedekRaporuMu = raporProjeTipi is ProjeTipi.Saha or ProjeTipi.Yedek;
        if (proje.ProjeTipiId != (int)raporProjeTipi)
        {
            var raporAdi = GetGerceklesenRaporAdi(raporProjeTipi);
            var beklenenTip = raporProjeTipi switch
            {
                ProjeTipi.Saha => "saha",
                ProjeTipi.Yedek => "yedek",
                _ => "normal"
            };
            throw new IsKuraliException($"{raporAdi} raporu sadece {beklenenTip} projeleri için alınabilir.");
        }

        var sandikSevkTarihleri = await context.GetSandikSevkTarihleriAsync(projeId);
        var sevkiyatKaydiVar = sandikSevkTarihleri.Count > 0;

        if (raporProjeTipi == ProjeTipi.Normal && !ProjeSevkEdilmisSayilir(proje, sevkiyatKaydiVar))
            throw new IsKuraliException("Gerçekleşen çeki listesi raporu sadece sevk edilmiş projeler için alınabilir.");

        var satirlar = await LoadGerceklesenRaporSatirlariAsync(projeId, sahaYedekRaporuMu);
        // R-07: Durum iş tamamlanmasını, SEVK ürünün fiziksel sevkini (kendi ve saha tarafında sevkli sandığa fiilen
        // konan adet; trafoyla kapanan ayrı) gösterir.
        var sahaAktarimlari = await context.GetSahaAktarimParcalariAsync(satirlar);
        UrunSevkSonucu? GetSevkDurumu(CekiSatiri satir) =>
            UrunSevkDurumuKurali.Belirle(satir, projeId, sahaAktarimlari.GetValueOrDefault(satir.Id));

        // İlk sayfa için sandık bilgilerini çek
        var projeSandiklari = await context.Sandiklar
            .AsNoTracking()
            .Where(s => s.ProjeId == projeId)
            .ToListAsync();

        if (!satirlar.Any())
            throw new KayitBulunamadiException($"Projeye ait çeki satırı bulunamadı: {projeId}");

        var tamamlamaPlanMap = await context.GetTamamlamaPlanMapAsync(satirlar, sadeceSevkEdilenSandiklar: !sahaYedekRaporuMu);

        QuestPDF.Settings.License = LicenseType.Community;

        var headerBg = Colors.Blue.Darken3;
        var headerText = Colors.White;
        var tableBorderColor = Colors.Grey.Lighten2;
        var altRowBg = "#F8FAFE";
        var raporTarihi = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");
        var sevkTarihi = proje.GerceklesenSevkTarihi?.ToString("dd.MM.yyyy") ?? proje.PlanlananSevkTarihi?.ToString("dd.MM.yyyy") ?? "-";

        // Sandıkları sayısal sıraya göre sırala
        projeSandiklari = projeSandiklari
            .OrderBy(s => s.SandikNo, SandikNumarasiComparer.Instance)
            .ThenBy(s => s.Id)
            .ToList();

        string GetSandikNolari(CekiSatiri satir)
        {
            return GetRaporSandikNolari(satir);
        }

        decimal GetTrafodaSevkAdet(CekiSatiri satir)
        {
            return Math.Max(satir.TrafoSevkAdet, 0);
        }

        bool IsGridIptal(CekiSatiri satir)
        {
            return satir.GridDurumuId == (int)GridDurum.Iptal
                || satir.GridDurumuId == (int)GridDurum.IptalEdildi;
        }

        decimal GetGerceklesenAdet(CekiSatiri satir)
        {
            if (IsGridIptal(satir))
                return 0;

            var trafodaSevk = GetTrafodaSevkAdet(satir);
            var gerceklesen = satir.IstenenAdet - trafodaSevk - satir.KalanMiktar;
            var maxGerceklesen = Math.Max(satir.IstenenAdet - trafodaSevk, 0);

            if (gerceklesen < 0)
                return 0;

            return gerceklesen > maxGerceklesen ? maxGerceklesen : gerceklesen;
        }

        int GetDurumSortPriority(CekiSatiri satir)
        {
            if (GetTrafodaSevkAdet(satir) > 0)
                return 0;

            if (IsGridIptal(satir))
                return 1;

            if (satir.KalanMiktar > 0)
                return 2;

            return 3;
        }

        string GetRaporDurum(CekiSatiri satir)
        {
            if (GetTrafodaSevkAdet(satir) > 0)
                return "Trafoda Sevk";

            if (IsGridIptal(satir))
                return "İptal";

            return satir.KalanMiktar > 0 ? "Eksik" : "Tamamlandı";
        }

        string GetAciklama(CekiSatiri satir)
        {
            return RaporBicimleri.GetRaporAciklama(satir);
        }

        satirlar = satirlar
            .OrderBy(GetDurumSortPriority)
            .ThenBy(GetRaporPrimarySandikSortKey, SandikNumarasiComparer.Instance)
            .ThenBy(s => s.SiraNo)
            .ThenBy(s => s.Id)
            .ToList();

        var toplamSatir = satirlar.Count;
        var toplamAdet = satirlar.Sum(s => s.IstenenAdet);
        var toplamGerceklesen = satirlar.Sum(GetGerceklesenAdet);
        var toplamTrafodaSevk = satirlar.Sum(GetTrafodaSevkAdet);
        var toplamKalan = satirlar.Sum(s => RaporSorgulari.GetEksikRaporKalan(s, tamamlamaPlanMap));
        var sevkOzeti = UrunSevkDurumuKurali.OzetMetni(satirlar.Select(GetSevkDurumu));
        var sevkEdilenSandikSayisi = projeSandiklari.Count(s => s.DurumId == (int)SandikDurum.Sevkedildi);

        var document = Document.Create(container =>
        {
            // ===== SAYFA 1: SANDIK ÖZET TABLOSU =====
            if (projeSandiklari.Any())
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(18);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    // Header
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("3K").Bold().FontSize(24).FontColor(headerText);
                                col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                            });
                            row.RelativeItem(2).AlignCenter().Column(col =>
                            {
                                col.Item().Text("Koli / Sandık Bilgileri").Bold().FontSize(16).FontColor(headerText);
                                col.Item().Text("3K Sevkiyat Yönetim Sistemi – Gerçekleşen Çeki Listesi").FontSize(7).FontColor(Colors.Grey.Lighten3);
                            });
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                                col.Item().Text($"Sevk Tarihi: {sevkTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                            });
                        });

                        headerCol.Item().PaddingTop(10).Border(1).BorderColor(tableBorderColor).Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("Proje No: ").FontSize(10);
                                t.Span(proje.ProjeNo).Bold().FontSize(11).FontColor(headerBg);
                            });
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("Müşteri: ").FontSize(10);
                                t.Span(proje.Musteri ?? "-").Bold().FontSize(11).FontColor(headerBg);
                            });
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Toplam Sandık: ").FontSize(10);
                                t.Span($"{projeSandiklari.Count} adet / {sevkEdilenSandikSayisi} sevk edildi").Bold().FontSize(11).FontColor(headerBg);
                            });
                        });

                        headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(tableBorderColor);
                    });

                    // Content - Sandık Tablosu
                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(28);    // #
                            columns.ConstantColumn(55);    // Koli No
                            columns.RelativeColumn(1.8f);  // Sandık Adı
                            columns.RelativeColumn(1.8f);  // Sandık Adı (EN)
                            columns.ConstantColumn(58);    // Net (kg)
                            columns.ConstantColumn(58);    // Brüt (kg)
                            columns.ConstantColumn(64);    // Boy (mm)
                            columns.ConstantColumn(64);    // En (mm)
                            columns.ConstantColumn(72);    // Yükseklik (mm)
                            columns.ConstantColumn(66);    // Sevk
                            columns.ConstantColumn(62);    // Sevk Tarihi
                        });

                        table.Header(header =>
                        {
                            void BiHeader(IContainer c, string tr, string en = "")
                            {
                                c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(5).Column(col =>
                                {
                                    col.Item().Text(tr).Bold().FontSize(8).FontColor(headerText);
                                    if (!string.IsNullOrEmpty(en))
                                        col.Item().Text(en).FontSize(5.5f).FontColor(Colors.Grey.Lighten3).Italic();
                                });
                            }

                            BiHeader(header.Cell(), "#");
                            BiHeader(header.Cell(), "KOLİ NO", "Case No");
                            BiHeader(header.Cell(), "SANDIK ADI", "Crate Name");
                            BiHeader(header.Cell(), "SANDIK ADI (EN)", "Case Name");
                            BiHeader(header.Cell(), "NET (kg)");
                            BiHeader(header.Cell(), "BRÜT (kg)", "Gross (kg)");
                            BiHeader(header.Cell(), "BOY (mm)", "Length (mm)");
                            BiHeader(header.Cell(), "EN (mm)", "Width (mm)");
                            BiHeader(header.Cell(), "YÜKSEKLİK (mm)", "Height (mm)");
                            BiHeader(header.Cell(), "SEVK", "Shipment");
                            BiHeader(header.Cell(), "SEVK TARİHİ", "Ship Date");
                        });

                        int sandikSira = 1;
                        decimal toplamNet = 0;
                        decimal toplamBrut = 0;

                        foreach (var sandik in projeSandiklari)
                        {
                            var bg = sandikSira % 2 == 0 ? altRowBg : "#FFFFFF";
                            var netKg = sandik.NetKg ?? 0;
                            var brutKg = sandik.GrossKg ?? 0;
                            toplamNet += netKg;
                            toplamBrut += brutKg;

                            void SandikDataCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                            {
                                var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(5);
                                if (bold)
                                    cell.Text(text).FontSize(8).FontColor(fontColor ?? Colors.Black).Bold();
                                else
                                    cell.Text(text).FontSize(8).FontColor(fontColor ?? Colors.Black);
                            }

                            SandikDataCell(table.Cell(), sandikSira.ToString(), bold: true);
                            SandikDataCell(table.Cell(), sandik.SandikNo, bold: true, fontColor: headerBg);
                            SandikDataCell(table.Cell(), RaporBicimleri.RaporMetni(sandik.Ad));
                            SandikDataCell(table.Cell(), RaporBicimleri.RaporMetni(sandik.AdIngilizce));
                            SandikDataCell(table.Cell(), RaporBicimleri.FormatAdet(netKg), bold: true);
                            SandikDataCell(table.Cell(), RaporBicimleri.FormatAdet(brutKg), bold: true);
                            SandikDataCell(table.Cell(), RaporBicimleri.FormatAdet(sandik.Boy ?? 0));
                            SandikDataCell(table.Cell(), RaporBicimleri.FormatAdet(sandik.En ?? 0));
                            SandikDataCell(table.Cell(), RaporBicimleri.FormatAdet(sandik.Yukseklik ?? 0));
                            var sandikSevk = UrunSevkDurumuKurali.SandikDurumu(sandik);
                            SandikDataCell(table.Cell(), UrunSevkDurumuKurali.Etiket(sandikSevk), bold: true,
                                fontColor: RaporBicimleri.SevkRengi(sandikSevk));
                            SandikDataCell(table.Cell(), RaporBicimleri.SevkTarihiMetni(sandikSevkTarihleri, sandik.Id));

                            sandikSira++;
                        }

                        // Toplam satırı
                        void ToplamCell(IContainer c, string text, bool bold = true)
                        {
                            var cell = c.Background("#E3F2FD").Border(0.5f).BorderColor(tableBorderColor).Padding(5);
                            cell.Text(text).FontSize(8).FontColor(Colors.Black).Bold();
                        }

                        ToplamCell(table.Cell(), "");
                        ToplamCell(table.Cell(), "");
                        ToplamCell(table.Cell(), "TOPLAM");
                        ToplamCell(table.Cell(), "");
                        ToplamCell(table.Cell(), RaporBicimleri.FormatAdet(toplamNet));
                        ToplamCell(table.Cell(), RaporBicimleri.FormatAdet(toplamBrut));
                        ToplamCell(table.Cell(), "");
                        ToplamCell(table.Cell(), "");
                        ToplamCell(table.Cell(), "");
                        ToplamCell(table.Cell(), $"{sevkEdilenSandikSayisi} / {projeSandiklari.Count}");
                        ToplamCell(table.Cell(), "");
                    });

                    // Footer
                    page.Footer().Row(footer =>
                    {
                        footer.RelativeItem().Text($"3K Ambalaj - Koli/Sandık Bilgileri | Proje: {proje.ProjeNo}").FontSize(7).FontColor(Colors.Grey.Medium);
                        footer.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                            x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                            x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            }

            // ===== SAYFA 2+: GERÇEKLEŞEN ÇEKİ LİSTESİ DETAY =====
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("3K").Bold().FontSize(24).FontColor(headerText);
                            col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                        });
                        row.RelativeItem(2).AlignCenter().Column(col =>
                        {
                            col.Item().Text("Gerçekleşen Çeki Listesi Raporu").Bold().FontSize(16).FontColor(headerText);
                            col.Item().Text("3K Sevkiyat Yönetim Sistemi").FontSize(9).FontColor(Colors.Grey.Lighten3);
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                            col.Item().Text($"Sevk Tarihi: {sevkTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                        });
                    });

                    headerCol.Item().PaddingTop(10).Border(1).BorderColor(tableBorderColor).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Proje No: ").FontSize(10);
                            t.Span(proje.ProjeNo).Bold().FontSize(11).FontColor(headerBg);
                        });
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Müşteri: ").FontSize(10);
                            t.Span(proje.Musteri ?? "-").Bold().FontSize(11).FontColor(headerBg);
                        });
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Toplam: ").FontSize(10);
                            t.Span($"{toplamSatir} satır / {RaporBicimleri.FormatAdet(toplamAdet)} adet / {RaporBicimleri.FormatAdet(toplamGerceklesen)} gerçekleşen / {RaporBicimleri.FormatAdet(toplamTrafodaSevk)} trafoda / {RaporBicimleri.FormatAdet(toplamKalan)} kalan").Bold().FontSize(9);
                        });
                    });

                    headerCol.Item().PaddingTop(4).AlignRight().Text(t =>
                    {
                        t.Span("Sevk (satır): ").FontSize(9);
                        t.Span(sevkOzeti).Bold().FontSize(9);
                    });
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.ConstantColumn(40);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(36);
                        columns.RelativeColumn(1.8f);
                        columns.ConstantColumn(38);
                        columns.ConstantColumn(34);
                        columns.ConstantColumn(54);
                        columns.ConstantColumn(44);
                        columns.ConstantColumn(38);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(58);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header =>
                    {
                        void BiHeader(IContainer c, string tr, string en = "")
                        {
                            c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(4).Column(col =>
                            {
                                col.Item().Text(tr).Bold().FontSize(7).FontColor(headerText);
                                if (!string.IsNullOrEmpty(en))
                                    col.Item().Text(en).FontSize(5).FontColor(Colors.Grey.Lighten3).Italic();
                            });
                        }

                        BiHeader(header.Cell(), "#");
                        BiHeader(header.Cell(), "SANDIK", "Crate");
                        BiHeader(header.Cell(), "BARKOD", "Barcode");
                        BiHeader(header.Cell(), "POZ NO", "Pos. No");
                        BiHeader(header.Cell(), "İSİM", "Name");
                        BiHeader(header.Cell(), "MİKTAR", "Quantity");
                        BiHeader(header.Cell(), "BİRİM", "Unit");
                        BiHeader(header.Cell(), "GERÇEKLEŞEN", "Actual");
                        BiHeader(header.Cell(), "TRAFODA", "At Transformer");
                        BiHeader(header.Cell(), "KALAN", "Remaining");
                        BiHeader(header.Cell(), "DURUM", "Status");
                        BiHeader(header.Cell(), "SEVK", "Shipment");
                        BiHeader(header.Cell(), "AÇIKLAMA", "Remarks");
                    });

                    var sira = 1;
                    foreach (var satir in satirlar)
                    {
                        var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";
                        var gerceklesenAdet = GetGerceklesenAdet(satir);
                        var trafodaSevkAdet = GetTrafodaSevkAdet(satir);
                        var kalanMiktar = RaporSorgulari.GetEksikRaporKalan(satir, tamamlamaPlanMap);
                        var durumMetni = GetRaporDurum(satir);
                        var aciklama = GetAciklama(satir);

                        void DataCell(IContainer c, string text, bool bold = false, string? color = null)
                        {
                            var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(4);
                            if (bold)
                                cell.Text(text).FontSize(7).FontColor(color ?? Colors.Black).Bold();
                            else
                                cell.Text(text).FontSize(7).FontColor(color ?? Colors.Black);
                        }

                        void RowNumberCell(IContainer c, string text)
                        {
                            c.Background(bg)
                                .Border(0.5f)
                                .BorderColor(tableBorderColor)
                                .PaddingVertical(4)
                                .PaddingHorizontal(2)
                                .AlignCenter()
                                .Text(text)
                                .FontSize(7)
                                .FontColor(Colors.Black)
                                .Bold();
                        }

                        void DurumCell(IContainer c, string text)
                        {
                            var cell = c.Background(bg)
                                .Border(0.5f)
                                .BorderColor(tableBorderColor)
                                .Padding(4);

                            cell.Text(text)
                                .FontSize(7)
                                .Bold()
                                .FontColor(Colors.Black);
                        }

                        RowNumberCell(table.Cell(), sira.ToString());
                        DataCell(table.Cell(), GetSandikNolari(satir), bold: true, color: headerBg);
                        DataCell(table.Cell(), satir.BarkodNo);
                        DataCell(table.Cell(), string.IsNullOrWhiteSpace(satir.OlcuResmiPozNo) ? "-" : satir.OlcuResmiPozNo);
                        DataCell(table.Cell(), satir.Aciklama);
                        DataCell(table.Cell(), RaporBicimleri.FormatAdet(satir.IstenenAdet), bold: true);
                        DataCell(table.Cell(), satir.BirimLookup?.Deger ?? "Adet");
                        DataCell(table.Cell(), RaporBicimleri.FormatAdet(gerceklesenAdet), bold: true);
                        DataCell(table.Cell(), trafodaSevkAdet > 0 ? RaporBicimleri.FormatAdet(trafodaSevkAdet) : "-", bold: trafodaSevkAdet > 0);
                        DataCell(table.Cell(), RaporBicimleri.FormatAdet(kalanMiktar), bold: true);
                        DurumCell(table.Cell(), durumMetni);
                        var sevk = GetSevkDurumu(satir);
                        DataCell(table.Cell(), RaporBicimleri.SevkMetni(sevk), bold: true, color: RaporBicimleri.SevkRengi(sevk?.Durum));
                        DataCell(table.Cell(), aciklama);

                        sira++;
                    }
                });

                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().Text($"3K Ambalaj - Gerçekleşen Çeki Listesi | Proje: {proje.ProjeNo}").FontSize(7).FontColor(Colors.Grey.Medium);
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

    public async Task<byte[]> GerceklesenCekiListesiRaporuExcelOlusturAsync(int projeId)
    {
        var projeTipi = await GetRaporProjeTipiAsync(projeId);
        return await GerceklesenCekiListesiRaporuExcelOlusturAsync(projeId, projeTipi);
    }

    public Task<byte[]> SahaGerceklesenCekiListesiRaporuExcelOlusturAsync(int projeId)
        => GerceklesenCekiListesiRaporuExcelOlusturAsync(projeId, ProjeTipi.Saha);

    private async Task<byte[]> GerceklesenCekiListesiRaporuExcelOlusturAsync(int projeId, ProjeTipi raporProjeTipi)
    {
        var proje = await context.Projeler
            .Include(p => p.DurumLookup)
            .FirstOrDefaultAsync(p => p.Id == projeId);

        if (proje == null)
            throw new KayitBulunamadiException($"Proje bulunamadi: {projeId}");

        var sahaYedekRaporuMu = raporProjeTipi is ProjeTipi.Saha or ProjeTipi.Yedek;
        if (proje.ProjeTipiId != (int)raporProjeTipi)
        {
            var raporAdi = GetGerceklesenRaporAdi(raporProjeTipi);
            var beklenenTip = raporProjeTipi switch
            {
                ProjeTipi.Saha => "saha",
                ProjeTipi.Yedek => "yedek",
                _ => "normal"
            };
            throw new IsKuraliException($"{raporAdi} raporu sadece {beklenenTip} projeleri için alınabilir.");
        }

        var sandikSevkTarihleri = await context.GetSandikSevkTarihleriAsync(projeId);
        var sevkiyatKaydiVar = sandikSevkTarihleri.Count > 0;

        if (raporProjeTipi == ProjeTipi.Normal && !ProjeSevkEdilmisSayilir(proje, sevkiyatKaydiVar))
            throw new IsKuraliException("Gerçekleşen çeki listesi raporu sadece sevk edilmiş projeler için alınabilir.");

        var satirlar = await LoadGerceklesenRaporSatirlariAsync(projeId, sahaYedekRaporuMu);

        var projeSandiklari = await context.Sandiklar
            .AsNoTracking()
            .Where(s => s.ProjeId == projeId)
            .ToListAsync();

        if (!satirlar.Any())
            throw new KayitBulunamadiException($"Projeye ait çeki satırı bulunamadı: {projeId}");

        var tamamlamaPlanMap = await context.GetTamamlamaPlanMapAsync(satirlar, sadeceSevkEdilenSandiklar: !sahaYedekRaporuMu);

        projeSandiklari = projeSandiklari
            .OrderBy(s => s.SandikNo, SandikNumarasiComparer.Instance)
            .ThenBy(s => s.Id)
            .ToList();

        satirlar = satirlar
            .OrderBy(GetRaporDurumSortPriority)
            .ThenBy(GetRaporPrimarySandikSortKey, SandikNumarasiComparer.Instance)
            .ThenBy(s => s.SiraNo)
            .ThenBy(s => s.Id)
            .ToList();

        var toplamSatir = satirlar.Count;
        var toplamAdet = satirlar.Sum(s => s.IstenenAdet);
        var toplamGerceklesen = satirlar.Sum(GetRaporGerceklesenAdet);
        var toplamTrafodaSevk = satirlar.Sum(GetRaporTrafodaSevkAdet);
        var toplamKalan = satirlar.Sum(s => RaporSorgulari.GetEksikRaporKalan(s, tamamlamaPlanMap));
        var sevkTarihi = proje.GerceklesenSevkTarihi?.ToString("dd.MM.yyyy") ?? proje.PlanlananSevkTarihi?.ToString("dd.MM.yyyy") ?? "-";
        var sahaAktarimlari = await context.GetSahaAktarimParcalariAsync(satirlar);
        UrunSevkSonucu? GetSevkDurumu(CekiSatiri satir) =>
            UrunSevkDurumuKurali.Belirle(satir, projeId, sahaAktarimlari.GetValueOrDefault(satir.Id));
        var sevkOzeti = UrunSevkDurumuKurali.OzetMetni(satirlar.Select(GetSevkDurumu));
        var sevkEdilenSandikSayisi = projeSandiklari.Count(s => s.DurumId == (int)SandikDurum.Sevkedildi);

        using var workbook = new XLWorkbook();

        if (projeSandiklari.Any())
        {
            var sandikSheet = workbook.Worksheets.Add("Sandık Bilgileri");
            const int sandikLastColumn = 11;
            const int sandikHeaderRow = 6;

            sandikSheet.Cell(1, 1).Value = "Koli / Sandık Bilgileri";
            RaporExcelStilleri.StyleExcelTitle(sandikSheet, sandikLastColumn);
            sandikSheet.Cell(2, 1).Value = "Proje No";
            sandikSheet.Cell(2, 2).Value = proje.ProjeNo;
            sandikSheet.Cell(2, 4).Value = "Müşteri";
            sandikSheet.Cell(2, 5).Value = proje.Musteri ?? "-";
            sandikSheet.Cell(2, 8).Value = "Rapor Tarihi";
            sandikSheet.Cell(2, 9).Value = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");
            sandikSheet.Cell(3, 1).Value = "Toplam Sandık";
            RaporExcelStilleri.SetDecimalCell(sandikSheet.Cell(3, 2), projeSandiklari.Count);
            sandikSheet.Cell(3, 4).Value = "Sevk Tarihi";
            sandikSheet.Cell(3, 5).Value = sevkTarihi;
            sandikSheet.Cell(3, 8).Value = "Sevk Edilen Sandık";
            sandikSheet.Cell(3, 9).Value = $"{sevkEdilenSandikSayisi} / {projeSandiklari.Count}";
            RaporExcelStilleri.StyleExcelInfoRows(sandikSheet, sandikLastColumn);

            var sandikHeaders = new[] { "#", "Koli No", "Sandık Adı", "Sandık Adı (EN)", "Net (kg)", "Brüt (kg)", "Boy (mm)", "En (mm)", "Yükseklik (mm)", "Sevk", "Sevk Tarihi" };
            for (var i = 0; i < sandikHeaders.Length; i++)
                sandikSheet.Cell(sandikHeaderRow, i + 1).Value = sandikHeaders[i];

            RaporExcelStilleri.StyleExcelHeader(sandikSheet, sandikHeaderRow, sandikLastColumn);

            var row = sandikHeaderRow + 1;
            var sira = 1;
            foreach (var sandik in projeSandiklari)
            {
                sandikSheet.Cell(row, 1).SetValue(sira);
                sandikSheet.Cell(row, 2).Value = sandik.SandikNo;
                sandikSheet.Cell(row, 3).Value = RaporBicimleri.RaporMetni(sandik.Ad);
                sandikSheet.Cell(row, 4).Value = RaporBicimleri.RaporMetni(sandik.AdIngilizce);
                RaporExcelStilleri.SetDecimalCell(sandikSheet.Cell(row, 5), sandik.NetKg ?? 0);
                RaporExcelStilleri.SetDecimalCell(sandikSheet.Cell(row, 6), sandik.GrossKg ?? 0);
                RaporExcelStilleri.SetDecimalCell(sandikSheet.Cell(row, 7), sandik.Boy ?? 0);
                RaporExcelStilleri.SetDecimalCell(sandikSheet.Cell(row, 8), sandik.En ?? 0);
                RaporExcelStilleri.SetDecimalCell(sandikSheet.Cell(row, 9), sandik.Yukseklik ?? 0);
                var sandikSevk = UrunSevkDurumuKurali.SandikDurumu(sandik);
                RaporExcelStilleri.SetSevkCell(sandikSheet.Cell(row, 10), UrunSevkDurumuKurali.Etiket(sandikSevk), sandikSevk);
                sandikSheet.Cell(row, 11).Value = RaporBicimleri.SevkTarihiMetni(sandikSevkTarihleri, sandik.Id);

                if (sira % 2 == 0)
                    sandikSheet.Range(row, 1, row, sandikLastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFE");

                row++;
                sira++;
            }

            RaporExcelStilleri.FinalizeExcelWorksheet(sandikSheet, sandikHeaderRow, row - 1, sandikLastColumn);
        }

        var worksheet = workbook.Worksheets.Add("Gerçekleşen Çeki");
        const int lastColumn = 13;
        const int headerRow = 6;

        worksheet.Cell(1, 1).Value = "Gerçekleşen Çeki Listesi Raporu";
        RaporExcelStilleri.StyleExcelTitle(worksheet, lastColumn);
        worksheet.Cell(2, 1).Value = "Proje No";
        worksheet.Cell(2, 2).Value = proje.ProjeNo;
        worksheet.Cell(2, 4).Value = "Müşteri";
        worksheet.Cell(2, 5).Value = proje.Musteri ?? "-";
        worksheet.Cell(2, 8).Value = "Rapor Tarihi";
        worksheet.Cell(2, 9).Value = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");
        worksheet.Cell(3, 1).Value = "Sevk Tarihi";
        worksheet.Cell(3, 2).Value = sevkTarihi;
        worksheet.Cell(3, 4).Value = "Toplam";
        worksheet.Cell(3, 5).Value = $"{toplamSatir} satır / {RaporBicimleri.FormatAdet(toplamAdet)} adet / {RaporBicimleri.FormatAdet(toplamGerceklesen)} gerçekleşen / {RaporBicimleri.FormatAdet(toplamTrafodaSevk)} trafoda / {RaporBicimleri.FormatAdet(toplamKalan)} kalan";
        worksheet.Cell(3, 8).Value = "Sevk (satır)";
        worksheet.Cell(3, 9).Value = sevkOzeti;
        RaporExcelStilleri.StyleExcelInfoRows(worksheet, lastColumn);

        var headers = new[]
        {
            "#", "Sandık", "Barkod", "Poz No", "İsim", "Miktar", "Birim",
            "Gerçekleşen", "Trafoda", "Kalan", "Durum", "Sevk", "Açıklama"
        };

        for (var i = 0; i < headers.Length; i++)
            worksheet.Cell(headerRow, i + 1).Value = headers[i];

        RaporExcelStilleri.StyleExcelHeader(worksheet, headerRow, lastColumn);

        var dataRow = headerRow + 1;
        var dataSira = 1;
        foreach (var satir in satirlar)
        {
            var gerceklesenAdet = GetRaporGerceklesenAdet(satir);
            var trafodaSevkAdet = GetRaporTrafodaSevkAdet(satir);

            worksheet.Cell(dataRow, 1).SetValue(dataSira);
            worksheet.Cell(dataRow, 2).Value = GetRaporSandikNolari(satir);
            worksheet.Cell(dataRow, 3).Value = satir.BarkodNo;
            worksheet.Cell(dataRow, 4).Value = string.IsNullOrWhiteSpace(satir.OlcuResmiPozNo) ? "-" : satir.OlcuResmiPozNo;
            worksheet.Cell(dataRow, 5).Value = satir.Aciklama;
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(dataRow, 6), satir.IstenenAdet);
            worksheet.Cell(dataRow, 7).Value = satir.BirimLookup?.Deger ?? "Adet";
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(dataRow, 8), gerceklesenAdet);
            if (trafodaSevkAdet > 0) RaporExcelStilleri.SetDecimalCell(worksheet.Cell(dataRow, 9), trafodaSevkAdet); else worksheet.Cell(dataRow, 9).Value = "-";
            RaporExcelStilleri.SetDecimalCell(worksheet.Cell(dataRow, 10), RaporSorgulari.GetEksikRaporKalan(satir, tamamlamaPlanMap));
            worksheet.Cell(dataRow, 11).Value = GetGerceklesenRaporDurum(satir);
            var sevk = GetSevkDurumu(satir);
            RaporExcelStilleri.SetSevkCell(worksheet.Cell(dataRow, 12), RaporBicimleri.SevkMetni(sevk), sevk?.Durum);
            worksheet.Cell(dataRow, 13).Value = RaporBicimleri.GetRaporAciklama(satir);

            if (dataSira % 2 == 0)
                worksheet.Range(dataRow, 1, dataRow, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFE");

            dataRow++;
            dataSira++;
        }

        worksheet.Column(5).Style.Alignment.WrapText = true;
        worksheet.Column(13).Style.Alignment.WrapText = true;
        RaporExcelStilleri.FinalizeExcelWorksheet(worksheet, headerRow, dataRow - 1, lastColumn);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Stok modülündeki tüm ürünlerin PDF raporunu oluşturur.
    /// </summary>
    private async Task<ProjeTipi> GetRaporProjeTipiAsync(int projeId)
    {
        var projeTipiId = await context.Projeler
            .AsNoTracking()
            .Where(p => p.Id == projeId)
            .Select(p => (int?)p.ProjeTipiId)
            .SingleOrDefaultAsync();

        if (!projeTipiId.HasValue)
            throw new KayitBulunamadiException($"Proje bulunamadı: {projeId}");

        if (!Enum.IsDefined(typeof(ProjeTipi), projeTipiId.Value))
            throw new IsKuraliException($"Projenin tipi geçersiz: {projeTipiId.Value}");

        return (ProjeTipi)projeTipiId.Value;
    }

    private static string GetGerceklesenRaporAdi(ProjeTipi projeTipi) => projeTipi switch
    {
        ProjeTipi.Saha => "Saha gerçekleşen çeki listesi",
        ProjeTipi.Yedek => "Yedek gerçekleşen çeki listesi",
        _ => "Gerçekleşen çeki listesi"
    };

    private async Task<List<CekiSatiri>> LoadGerceklesenRaporSatirlariAsync(int projeId, bool sandikIcerikBazliRaporMu)
    {
        var satirlar = await context.CekiSatirlari
            .AsNoTracking()
            .Include(cs => cs.BirimLookup)
            .Include(cs => cs.SandikIcerikleri)
                .ThenInclude(si => si.Sandik)
            .Where(cs => cs.Ceki.ProjeId == projeId)
            .ToListAsync();

        if (!sandikIcerikBazliRaporMu)
            return satirlar;

        var sahaIcerikleri = await context.SandikIcerikleri
            .AsNoTracking()
            .Include(si => si.Sandik)
            .Include(si => si.BirimLookup)
            .Include(si => si.CekiSatiri)
                .ThenInclude(cs => cs!.BirimLookup)
            .Where(si => si.Sandik.ProjeId == projeId)
            .ToListAsync();
        sahaIcerikleri = sahaIcerikleri
            .OrderBy(si => si.Sandik.SandikNo, SandikNumarasiComparer.Instance)
            .ThenBy(si => si.Id)
            .ToList();

        if (!sahaIcerikleri.Any())
            return satirlar;

        var satirIds = satirlar.Select(s => s.Id).ToHashSet();
        var icerikMap = sahaIcerikleri
            .Where(si => si.CekiSatiriId.HasValue && satirIds.Contains(si.CekiSatiriId.Value))
            .GroupBy(si => si.CekiSatiriId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var satir in satirlar)
        {
            if (icerikMap.TryGetValue(satir.Id, out var projeIcerikleri))
            {
                satir.SandikIcerikleri = projeIcerikleri;
                continue;
            }

            satir.SandikIcerikleri = satir.SandikIcerikleri
                .Where(si => si.Sandik?.ProjeId == projeId)
                .ToList();
        }

        var ekSatirlar = new List<CekiSatiri>();
        var sentetikId = -1;
        var sentetikSiraNo = satirlar.Any() ? satirlar.Max(s => s.SiraNo) + 1 : 1;

        foreach (var icerik in sahaIcerikleri)
        {
            if (icerik.CekiSatiriId.HasValue && satirIds.Contains(icerik.CekiSatiriId.Value))
                continue;

            ekSatirlar.Add(BuildSahaIcerikRaporSatiri(icerik, sentetikId--, sentetikSiraNo++));
        }

        satirlar.AddRange(ekSatirlar);
        return satirlar;
    }

    private static CekiSatiri BuildSahaIcerikRaporSatiri(SandikIcerik icerik, int sentetikId, int siraNo)
    {
        var kaynakSatir = icerik.CekiSatiri;
        var miktar = GetSahaIcerikRaporMiktari(icerik, kaynakSatir);
        var eksikAdet = Math.Min(Math.Max(icerik.EksikAdet, 0), miktar);
        var tamamlananAdet = Math.Max(miktar - eksikAdet, 0);
        var sandikNo = icerik.Sandik?.SandikNo ?? "";
        var birimId = icerik.BirimId ?? kaynakSatir?.BirimId ?? (int)Birim.Adet;

        return new CekiSatiri
        {
            Id = sentetikId,
            CekiId = kaynakSatir?.CekiId ?? 0,
            KaynakCekiSatiriId = kaynakSatir?.Id,
            SiraNo = kaynakSatir?.SiraNo ?? siraNo,
            OlcuResmiPozNo = kaynakSatir?.OlcuResmiPozNo,
            BarkodNo = !string.IsNullOrWhiteSpace(icerik.BarkodNo) ? icerik.BarkodNo! : kaynakSatir?.BarkodNo ?? "",
            Aciklama = !string.IsNullOrWhiteSpace(icerik.Isim) ? icerik.Isim! : kaynakSatir?.Aciklama ?? icerik.Aciklama ?? "Saha malzemesi",
            IstenenAdet = miktar,
            BirimId = birimId,
            BirimLookup = icerik.BirimLookup ?? kaynakSatir?.BirimLookup,
            CekideGecenSandikNo = sandikNo,
            FiiliSandikNo = sandikNo,
            Remarks = kaynakSatir?.Remarks,
            DurumId = eksikAdet > 0 ? (int)UrunDurum.Eksik : (int)UrunDurum.Tamamlandi,
            GridDurumuId = eksikAdet > 0 ? (int)GridDurum.EksikGeldi : (int)GridDurum.TamGeldi,
            GridGelenAdet = tamamlananAdet,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            GridSevkMiktari = tamamlananAdet,
            UcKDurumuId = eksikAdet > 0 ? (int)UcKDurum.EksikGeldi : (int)UcKDurum.TamGeldi,
            UcKKarsilamaTipiId = eksikAdet > 0 ? (int)UcKDurum.EksikGeldi : (int)UcKDurum.TamGeldi,
            GelenMiktar = tamamlananAdet,
            KaynakHedefProjeNo = icerik.KaynakProjeNo,
            UcKAciklama = icerik.Aciklama,
            IsManuelEklenen = kaynakSatir == null,
            EklemeNedeni = kaynakSatir == null ? "Saha manuel malzeme" : "Saha kaynak satır bağlantısı",
            SandikIcerikleri = new List<SandikIcerik> { icerik }
        };
    }

    private static decimal GetSahaIcerikRaporMiktari(SandikIcerik icerik, CekiSatiri? kaynakSatir)
    {
        if (icerik.Miktar > 0)
            return icerik.Miktar;

        var icerikToplami = icerik.KonulanAdet
            + icerik.StokKarsilanan
            + icerik.ProjeKarsilanan
            + icerik.TedarikciKarsilanan
            + icerik.EksikAdet;

        if (icerikToplami > 0)
            return icerikToplami;

        return kaynakSatir?.IstenenAdet ?? 0;
    }

    private static List<string> GetRaporSandikNoList(CekiSatiri satir)
    {
        if (!string.IsNullOrWhiteSpace(satir.FiiliSandikNo))
            return new List<string> { satir.FiiliSandikNo.Trim() };

        var aktifSandiklar = satir.SandikIcerikleri
            .Where(si => si.KonulanAdet > 0 && si.Sandik != null && !string.IsNullOrWhiteSpace(si.Sandik.SandikNo))
            .Select(si => si.Sandik!.SandikNo.Trim())
            .Distinct()
            .OrderBy(x => x, SandikNumarasiComparer.Instance)
            .ToList();

        if (aktifSandiklar.Any())
            return aktifSandiklar;

        if (!string.IsNullOrWhiteSpace(satir.CekideGecenSandikNo))
            return new List<string> { satir.CekideGecenSandikNo.Trim() };

        var sandiklar = satir.SandikIcerikleri
            .Where(si => si.Sandik != null && !string.IsNullOrWhiteSpace(si.Sandik.SandikNo))
            .Select(si => si.Sandik!.SandikNo.Trim())
            .Distinct()
            .OrderBy(x => x, SandikNumarasiComparer.Instance)
            .ToList();

        return sandiklar;
    }

    private static string GetRaporSandikNolari(CekiSatiri satir)
    {
        return string.Join(", ", GetRaporSandikNoList(satir));
    }

    private static string? GetRaporPrimarySandikSortKey(CekiSatiri satir)
    {
        var firstSandikNo = GetRaporSandikNoList(satir).FirstOrDefault() ?? "";
        return firstSandikNo;
    }

    private static decimal GetRaporTrafodaSevkAdet(CekiSatiri satir)
    {
        return Math.Max(satir.TrafoSevkAdet, 0);
    }

    private static bool IsRaporGridIptal(CekiSatiri satir)
    {
        return satir.GridDurumuId == (int)GridDurum.Iptal
            || satir.GridDurumuId == (int)GridDurum.IptalEdildi;
    }

    private static decimal GetRaporGerceklesenAdet(CekiSatiri satir)
    {
        if (IsRaporGridIptal(satir))
            return 0;

        var trafodaSevk = GetRaporTrafodaSevkAdet(satir);
        var gerceklesen = satir.IstenenAdet - trafodaSevk - satir.KalanMiktar;
        var maxGerceklesen = Math.Max(satir.IstenenAdet - trafodaSevk, 0);

        if (gerceklesen < 0)
            return 0;

        return gerceklesen > maxGerceklesen ? maxGerceklesen : gerceklesen;
    }

    private static int GetRaporDurumSortPriority(CekiSatiri satir)
    {
        if (GetRaporTrafodaSevkAdet(satir) > 0)
            return 0;

        if (IsRaporGridIptal(satir))
            return 1;

        if (satir.KalanMiktar > 0)
            return 2;

        return 3;
    }

    private static string GetGerceklesenRaporDurum(CekiSatiri satir)
    {
        if (GetRaporTrafodaSevkAdet(satir) > 0)
            return "Trafoda Sevk";

        if (IsRaporGridIptal(satir))
            return "İptal";

        // Durum iş tamamlanmasıdır; sevk ayrı SEVK sütunundadır (R-07).
        return satir.KalanMiktar > 0 ? "Eksik" : "Tamamlandı";
    }

    private static bool ProjeSevkEdilmisSayilir(Proje proje, bool sevkiyatKaydiVar)
    {
        return proje.DurumId == (int)ProjeDurum.SevkEdildi
            || proje.DurumId == (int)ProjeDurum.EksikSevkEdildi
            || sevkiyatKaydiVar;
    }
}
