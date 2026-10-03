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

/// <summary>Depoda aktif görünen sandıkların raporları: genel (proje tipine göre) ve proje bazlı.</summary>
internal sealed class DepoSandikRaporService(AppDbContext context) : IDepoSandikRaporService
{
    public async Task<byte[]> DepoSandikRaporuPdfOlusturAsync(int? projeTipiId = null)
    {
        if (projeTipiId.HasValue && !Enum.IsDefined(typeof(ProjeTipi), projeTipiId.Value))
            throw new IsKuraliException("Geçersiz proje tipi filtresi.");

        IQueryable<Sandik> sandikQuery = context.Sandiklar
            .AsNoTracking()
            .Include(s => s.Proje)
                .ThenInclude(p => p.ProjeTipiLookup)
            .Include(s => s.DurumLookup)
            .Include(s => s.TipLookup)
            .Include(s => s.DepoLokasyonLookup)
            .Include(s => s.SandikIcerikleri)
                .ThenInclude(si => si.CekiSatiri)
            .Where(s => s.DurumId != (int)SandikDurum.Sevkedildi);

        if (projeTipiId.HasValue)
            sandikQuery = sandikQuery.Where(s => s.Proje.ProjeTipiId == projeTipiId.Value);

        var sandiklar = await sandikQuery.ToListAsync();
        var depoLokasyonlari = await context.LookupDepoLokasyonlari
            .AsNoTracking()
            .OrderBy(l => l.Anahtar)
            .ToListAsync();
        var lokasyonAdlari = depoLokasyonlari.ToDictionary(l => l.Id, l => l.Deger);

        bool BelirsizLokasyonMu(LookupDepoLokasyon lokasyon)
        {
            return lokasyon.Id == (int)DepoLokasyon.Belirsiz
                || lokasyon.Deger.Equals("Belirsiz", StringComparison.OrdinalIgnoreCase);
        }

        string EtkinDepoLokasyonMetni(Sandik sandik)
        {
            return lokasyonAdlari.GetValueOrDefault(sandik.DepoLokasyonId, "Belirsiz");
        }

        string LokasyonRengi(LookupDepoLokasyon lokasyon)
        {
            return lokasyon.Deger.Trim().ToUpperInvariant() switch
            {
                "3K" => "#0EA5E9",
                "SEYMEN" => "#078A55",
                "GRID" => "#F59E0B",
                "BELİRSİZ" or "BELIRSIZ" => "#64748B",
                _ => "#2563EB"
            };
        }

        var siraliSandiklar = sandiklar
            .Where(s => s.DepoLokasyonId != (int)DepoLokasyon.Belirsiz)
            .OrderBy(EtkinDepoLokasyonMetni)
            .ThenBy(s => s.Proje.ProjeNo)
            .ThenBy(s => s.SandikNo, SandikNumarasiComparer.Instance)
            .ThenBy(s => s.Id)
            .ToList();

        QuestPDF.Settings.License = LicenseType.Community;

        var headerBg = Colors.Blue.Darken3;
        var headerText = Colors.White;
        var tableBorderColor = Colors.Grey.Lighten2;
        var altRowBg = "#F8FAFE";
        var raporTarihi = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");
        var raporKapsami = projeTipiId switch
        {
            (int)ProjeTipi.Normal => "Normal Projeler",
            (int)ProjeTipi.Saha => "Saha Projeleri",
            (int)ProjeTipi.Yedek => "Yedek Projeleri",
            _ => "Tum Projeler"
        };

        var projeBazliDagilim = siraliSandiklar
            .GroupBy(s => new
            {
                s.ProjeId,
                s.Proje.ProjeNo,
                s.Proje.ProjeTipiId,
                ProjeTipi = s.Proje.ProjeTipiLookup != null ? s.Proje.ProjeTipiLookup.Deger : null
            })
            .Select(g => new
            {
                g.Key.ProjeId,
                g.Key.ProjeNo,
                ProjeTipiId = g.Key.ProjeTipiId,
                ProjeTipi = g.Key.ProjeTipi ?? (g.Key.ProjeTipiId switch
                {
                    (int)ProjeTipi.Normal => "Normal",
                    (int)ProjeTipi.Saha => "Saha",
                    (int)ProjeTipi.Yedek => "Yedek",
                    _ => "-"
                }),
                ToplamSandik = g.Count(),
                LokasyonSayilari = depoLokasyonlari.ToDictionary(
                    lokasyon => lokasyon.Id,
                    lokasyon => g.Count(s => s.DepoLokasyonId == lokasyon.Id))
            })
            .OrderBy(p => p.ProjeNo)
            .ToList();

        var raporLokasyonlari = depoLokasyonlari
            .Where(l => !BelirsizLokasyonMu(l))
            .OrderBy(l => l.Anahtar)
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("3K").Bold().FontSize(22).FontColor(headerText);
                            col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                        });
                        row.RelativeItem(2).AlignCenter().Column(col =>
                        {
                            col.Item().Text("Depo Sandık Raporu").Bold().FontSize(16).FontColor(headerText);
                            col.Item().Text("3K Sevkiyat Yönetim Sistemi - Depo Yönetimi Çıktısı").FontSize(7).FontColor(Colors.Grey.Lighten3);
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                            col.Item().Text($"Kapsam: {raporKapsami}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                        });
                    });

                    /*
                        row.RelativeItem().Element(c => GenelDurumKarti(c, "SAHA PROJELERİ", sahaDurum, Colors.Green.Darken1));
                        row.ConstantItem(8);
                        row.RelativeItem().Element(c => GenelDurumKarti(c, "YEDEK PROJELERİ", yedekDurum, Colors.Orange.Darken1));
                    });

                    headerCol.Item().PaddingTop(5).Text("Not: Sevk Edildi durumundaki sandıklar depo envanterine dahil edilmez. Proje kilidi açıldığında eski durumuna dönen sandıklar tekrar rapora girer.")
                        .FontSize(7).FontColor(Colors.Grey.Darken1);
                    */
                    headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(tableBorderColor);
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(24);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.0f);
                        columns.RelativeColumn(0.9f);
                        foreach (var _ in raporLokasyonlari)
                            columns.RelativeColumn(0.9f);
                    });

                    table.Header(header =>
                    {
                        void HeaderCell(IContainer c, string text) =>
                            c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(3).Text(text).Bold().FontSize(6.5f).FontColor(headerText);

                        HeaderCell(header.Cell(), "#");
                        HeaderCell(header.Cell(), "PROJE NO");
                        HeaderCell(header.Cell(), "PROJE TİPİ");
                        HeaderCell(header.Cell(), "TOPLAM SANDIK");
                        foreach (var lokasyon in raporLokasyonlari)
                            HeaderCell(header.Cell(), lokasyon.Deger.ToUpperInvariant());
                    });

                    int sira = 1;
                    foreach (var proje in projeBazliDagilim)
                    {
                        var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";

                        void DataCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                        {
                            var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(2);
                            if (bold)
                                cell.Text(text).FontSize(6.5f).FontColor(fontColor ?? Colors.Black).Bold();
                            else
                                cell.Text(text).FontSize(6.5f).FontColor(fontColor ?? Colors.Black);
                        }

                        string CountText(int value) => value > 0 ? value.ToString() : "-";

                        DataCell(table.Cell(), sira.ToString());
                        DataCell(table.Cell(), proje.ProjeNo, bold: true);
                        DataCell(table.Cell(), proje.ProjeTipi, bold: true, fontColor: proje.ProjeTipiId switch
                        {
                            (int)ProjeTipi.Saha => "#078A55",
                            (int)ProjeTipi.Yedek => "#B45309",
                            _ => "#0EA5E9"
                        });
                        DataCell(table.Cell(), proje.ToplamSandik.ToString(), bold: true);
                        foreach (var lokasyon in raporLokasyonlari)
                        {
                            var count = proje.LokasyonSayilari.GetValueOrDefault(lokasyon.Id);
                            DataCell(table.Cell(), CountText(count), bold: count > 0, fontColor: LokasyonRengi(lokasyon));
                        }

                        sira++;
                    }

                    // ===== TOPLAM SATIRI =====
                    var genelToplamSandik = projeBazliDagilim.Sum(p => p.ToplamSandik);

                    void ToplamCell(IContainer c, string text, string? fontColor = null)
                    {
                        var cell = c.Background("#E3F2FD").Border(0.5f).BorderColor(tableBorderColor).Padding(3);
                        cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                    }

                    ToplamCell(table.Cell(), "");
                    ToplamCell(table.Cell(), "TOPLAM");
                    ToplamCell(table.Cell(), $"{projeBazliDagilim.Count} proje");
                    ToplamCell(table.Cell(), genelToplamSandik.ToString());
                    foreach (var lokasyon in raporLokasyonlari)
                    {
                        var toplam = projeBazliDagilim.Sum(p => p.LokasyonSayilari.GetValueOrDefault(lokasyon.Id));
                        ToplamCell(table.Cell(), toplam > 0 ? toplam.ToString() : "-", LokasyonRengi(lokasyon));
                    }
                });

                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().Text("3K Ambalaj - Depo Sandık Raporu").FontSize(7).FontColor(Colors.Grey.Medium);
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

    public async Task<byte[]> ProjeDepoSandikRaporuPdfOlusturAsync(int projeId)
    {
        var proje = await context.Projeler
            .AsNoTracking()
            .Include(p => p.ProjeTipiLookup)
            .Include(p => p.DurumLookup)
            .FirstOrDefaultAsync(p => p.Id == projeId);

        if (proje == null)
            throw new KayitBulunamadiException($"Proje bulunamadi: {projeId}");

        var sandiklar = await context.Sandiklar
            .AsNoTracking()
            .Include(s => s.DurumLookup)
            .Include(s => s.TipLookup)
            .Include(s => s.DepoLokasyonLookup)
            .Include(s => s.SandikIcerikleri)
                .ThenInclude(si => si.CekiSatiri)
            .Where(s => s.ProjeId == projeId)
            .ToListAsync();

        bool DepodaSayilacakSandik(Sandik sandik)
        {
            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                return false;

            if (SandiktaGridKapandiUrunVar(sandik))
                return true;

            return sandik.SandikIcerikleri.Any(i =>
            {
                var satir = i.CekiSatiri;
                if (satir == null)
                    return i.Miktar > 0 || i.KonulanAdet > 0 || i.StokKarsilanan > 0 || i.ProjeKarsilanan > 0 || i.TedarikciKarsilanan > 0;

                return satir.GelenMiktar > 0
                    || satir.ProjeKarsilanan > 0
                    || satir.StokKarsilanan > 0
                    || satir.TedarikciKarsilanan > 0;
            });
        }

        bool SandiktaGridKapandiUrunVar(Sandik sandik)
        {
            return sandik.SandikIcerikleri.Any(i => i.CekiSatiri?.GridDurumuId == (int)GridDurum.GridKapandi);
        }

        var raporSandiklari = sandiklar
            .Where(s => s.DepoLokasyonId != (int)DepoLokasyon.Belirsiz)
            .Where(DepodaSayilacakSandik)
            .OrderBy(s => s.DepoLokasyonLookup?.Anahtar ?? s.DepoLokasyonId)
            .ThenBy(s => s.DepoLokasyonLookup?.Deger)
            .ThenBy(s => s.SandikNo, SandikNumarasiComparer.Instance)
            .ThenBy(s => s.Id)
            .ToList();

        var lokasyonOzetleri = raporSandiklari
            .GroupBy(s => new
            {
                s.DepoLokasyonId,
                Lokasyon = s.DepoLokasyonLookup?.Deger ?? "Belirsiz",
                Anahtar = s.DepoLokasyonLookup?.Anahtar ?? s.DepoLokasyonId
            })
            .Select(g => new
            {
                g.Key.DepoLokasyonId,
                g.Key.Lokasyon,
                g.Key.Anahtar,
                SandikSayisi = g.Count()
            })
            .OrderBy(x => x.Anahtar)
            .ThenBy(x => x.Lokasyon)
            .ToList();

        string LokasyonRengi(string lokasyon)
        {
            return lokasyon.Trim().ToUpperInvariant() switch
            {
                "3K" => "#0EA5E9",
                "SEYMEN" => "#078A55",
                "GRID" => "#F59E0B",
                _ => "#2563EB"
            };
        }

        string BoyutMetni(Sandik sandik)
        {
            var boyutlar = new[]
            {
                sandik.Boy.HasValue ? RaporBicimleri.FormatAdet(sandik.Boy.Value) : null,
                sandik.En.HasValue ? RaporBicimleri.FormatAdet(sandik.En.Value) : null,
                sandik.Yukseklik.HasValue ? RaporBicimleri.FormatAdet(sandik.Yukseklik.Value) : null
            }.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            return boyutlar.Count == 3 ? string.Join(" x ", boyutlar) : "-";
        }

        string AgirlikMetni(Sandik sandik)
        {
            if (!sandik.NetKg.HasValue && !sandik.GrossKg.HasValue)
                return "-";

            var net = sandik.NetKg.HasValue ? $"Net {RaporBicimleri.FormatAdet(sandik.NetKg.Value)}" : null;
            var brut = sandik.GrossKg.HasValue ? $"Brüt {RaporBicimleri.FormatAdet(sandik.GrossKg.Value)}" : null;
            return string.Join(" / ", new[] { net, brut }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        QuestPDF.Settings.License = LicenseType.Community;

        var headerBg = Colors.Blue.Darken3;
        var headerText = Colors.White;
        var tableBorderColor = Colors.Grey.Lighten2;
        var altRowBg = "#F8FAFE";
        var raporTarihi = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");
        var projeTipi = proje.ProjeTipiLookup?.Deger ?? (proje.ProjeTipiId switch
        {
            (int)ProjeTipi.Saha => "Saha",
            (int)ProjeTipi.Yedek => "Yedek",
            _ => "Normal"
        });
        var projeDurumu = proje.DurumLookup?.Deger ?? "-";
        var toplamSandik = raporSandiklari.Count;
        var toplamUrun = raporSandiklari.Sum(s => s.SandikIcerikleri.Count);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("3K").Bold().FontSize(22).FontColor(headerText);
                            col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                        });
                        row.RelativeItem(2).AlignCenter().Column(col =>
                        {
                            col.Item().Text("Proje Depo Sandık Raporu").Bold().FontSize(16).FontColor(headerText);
                            col.Item().Text("3K Sevkiyat Yönetim Sistemi - Depo Durumu").FontSize(7).FontColor(Colors.Grey.Lighten3);
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                            col.Item().Text($"Proje: {proje.ProjeNo}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                        });
                    });

                    headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(tableBorderColor);
                });

                page.Content().PaddingTop(10).Column(content =>
                {
                    content.Item().Border(1).BorderColor(tableBorderColor).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(t =>
                            {
                                t.Span("Proje No: ").FontSize(9);
                                t.Span(proje.ProjeNo).Bold().FontSize(10).FontColor(headerBg);
                            });
                            col.Item().PaddingTop(3).Text(t =>
                            {
                                t.Span("Müşteri: ").FontSize(8).FontColor(Colors.Grey.Darken2);
                                t.Span(string.IsNullOrWhiteSpace(proje.Musteri) ? "-" : proje.Musteri).Bold().FontSize(8);
                            });
                        });
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(t =>
                            {
                                t.Span("Proje Tipi: ").FontSize(8).FontColor(Colors.Grey.Darken2);
                                t.Span(projeTipi).Bold().FontSize(8);
                            });
                            col.Item().PaddingTop(3).Text(t =>
                            {
                                t.Span("Durum: ").FontSize(8).FontColor(Colors.Grey.Darken2);
                                t.Span(projeDurumu).Bold().FontSize(8);
                            });
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text(t =>
                            {
                                t.Span("Depodaki Sandık: ").FontSize(8).FontColor(Colors.Grey.Darken2);
                                t.Span(toplamSandik.ToString()).Bold().FontSize(10).FontColor(headerBg);
                            });
                            col.Item().PaddingTop(3).Text(t =>
                            {
                                t.Span("Ürün Satırı: ").FontSize(8).FontColor(Colors.Grey.Darken2);
                                t.Span(toplamUrun.ToString()).Bold().FontSize(8);
                            });
                        });
                    });

                    content.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.4f);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(70);
                        });

                        table.Header(header =>
                        {
                            void HeaderCell(IContainer c, string text) =>
                                c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(4).Text(text).Bold().FontSize(7).FontColor(headerText);

                            HeaderCell(header.Cell(), "DEPO LOKASYONU");
                            HeaderCell(header.Cell(), "SANDIK");
                            HeaderCell(header.Cell(), "ORAN");
                        });

                        if (lokasyonOzetleri.Count == 0)
                        {
                            void EmptyCell(IContainer c, string text) =>
                                c.Border(0.5f).BorderColor(tableBorderColor).Padding(4).Text(text).FontSize(7).FontColor(Colors.Grey.Darken1);

                            EmptyCell(table.Cell().ColumnSpan(3), "Bu projeye ait depoda görünen sandık bulunmuyor.");
                        }
                        else
                        {
                            foreach (var lokasyon in lokasyonOzetleri)
                            {
                                var oran = toplamSandik > 0
                                    ? $"{Math.Round(lokasyon.SandikSayisi * 100m / toplamSandik, 1):0.#}%"
                                    : "0%";
                                var color = LokasyonRengi(lokasyon.Lokasyon);

                                void SummaryCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                                {
                                    var cell = c.Border(0.5f).BorderColor(tableBorderColor).Padding(4);
                                    if (bold)
                                        cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                                    else
                                        cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black);
                                }

                                SummaryCell(table.Cell(), lokasyon.Lokasyon, bold: true, fontColor: color);
                                SummaryCell(table.Cell(), lokasyon.SandikSayisi.ToString(), bold: true);
                                SummaryCell(table.Cell(), oran);
                            }
                        }
                    });

                    content.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(28);
                            columns.ConstantColumn(58);
                            columns.RelativeColumn(1.6f);
                            columns.ConstantColumn(64);
                            columns.ConstantColumn(64);
                            columns.ConstantColumn(76);
                            columns.ConstantColumn(42);
                            columns.ConstantColumn(88);
                            columns.ConstantColumn(68);
                        });

                        table.Header(header =>
                        {
                            void HeaderCell(IContainer c, string text) =>
                                c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(4).Text(text).Bold().FontSize(7).FontColor(headerText);

                            HeaderCell(header.Cell(), "#");
                            HeaderCell(header.Cell(), "SANDIK NO");
                            HeaderCell(header.Cell(), "SANDIK ADI");
                            HeaderCell(header.Cell(), "TİP");
                            HeaderCell(header.Cell(), "DURUM");
                            HeaderCell(header.Cell(), "DEPO");
                            HeaderCell(header.Cell(), "ÜRÜN");
                            HeaderCell(header.Cell(), "ÖLÇÜ");
                            HeaderCell(header.Cell(), "KG");
                        });

                        if (raporSandiklari.Count == 0)
                        {
                            table.Cell().ColumnSpan(9)
                                .Border(0.5f)
                                .BorderColor(tableBorderColor)
                                .Padding(8)
                                .AlignCenter()
                                .Text("Bu projeye ait depoda görünen sandık bulunmuyor.")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);
                        }

                        var sira = 1;
                        foreach (var sandik in raporSandiklari)
                        {
                            var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";
                            var lokasyon = sandik.DepoLokasyonLookup?.Deger ?? "Belirsiz";
                            var lokasyonRengi = LokasyonRengi(lokasyon);

                            void DataCell(IContainer c, string text, bool bold = false, string? fontColor = null, bool center = false)
                            {
                                var cell = c.Background(bg)
                                    .Border(0.5f)
                                    .BorderColor(tableBorderColor)
                                    .Padding(4);

                                if (center)
                                    cell = cell.AlignCenter();

                                if (bold)
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                                else
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black);
                            }

                            DataCell(table.Cell(), sira.ToString(), bold: true, center: true);
                            DataCell(table.Cell(), sandik.SandikNo, bold: true, fontColor: headerBg);
                            DataCell(table.Cell(), string.IsNullOrWhiteSpace(sandik.Ad) ? "-" : sandik.Ad);
                            DataCell(table.Cell(), sandik.TipLookup?.Deger ?? "-", center: true);
                            DataCell(table.Cell(), sandik.DurumLookup?.Deger ?? "-", bold: true, center: true);
                            DataCell(table.Cell(), lokasyon, bold: true, fontColor: lokasyonRengi, center: true);
                            DataCell(table.Cell(), sandik.SandikIcerikleri.Count.ToString(), bold: true, center: true);
                            DataCell(table.Cell(), BoyutMetni(sandik), center: true);
                            DataCell(table.Cell(), AgirlikMetni(sandik), center: true);

                            sira++;
                        }
                    });
                });

                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().Text($"3K Ambalaj - Proje Depo Sandık Raporu | Proje: {proje.ProjeNo}").FontSize(7).FontColor(Colors.Grey.Medium);
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
}
