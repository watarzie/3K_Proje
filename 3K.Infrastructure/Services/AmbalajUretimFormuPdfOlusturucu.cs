using System.Collections.Concurrent;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// Ambalaj üretim formunun görsel şablonunu üretir. Veri seçimi Application
    /// katmanında yapılır; bu sınıf yalnız seçilmiş form modelini gruplar ve çizer.
    /// </summary>
    internal static class AmbalajUretimFormuPdfOlusturucu
    {
        private static readonly ConcurrentDictionary<string, Lazy<byte[]>> VarlikOnbellegi =
            new(StringComparer.OrdinalIgnoreCase);

        public static byte[] Olustur(AmbalajUretimFormuModel form)
        {
            ArgumentNullException.ThrowIfNull(form);
            if (form.Kalemler.Count == 0)
                throw new InvalidOperationException("Üretim formunda çizilecek sandık bulunmuyor.");

            QuestPDF.Settings.License = LicenseType.Community;
            var gruplar = AmbalajUretimFormuGruplayici.Grupla(form.Kalemler);
            var raporTarihi = TurkeyTime.Now;
            var document = Document.Create(container =>
            {
                container.Page(page => ListeSayfasi(page, form, gruplar, raporTarihi));
                foreach (var grup in gruplar)
                {
                    container.Page(page => DetaySayfasi(page, form, grup, raporTarihi));
                    container.Page(page => CizimSayfasi(page, form, grup, raporTarihi));
                }
            });

            return document.GeneratePdf();
        }

        private static void ListeSayfasi(
            PageDescriptor page,
            AmbalajUretimFormuModel form,
            IReadOnlyList<AmbalajUretimGrubu> gruplar,
            DateTime raporTarihi)
        {
            ListeSayfaAyarlari(page);
            page.Header().Element(header => RaporBasligi(
                header,
                "AMBALAJ ÜRETİM LİSTESİ",
                form,
                raporTarihi,
                firinPartiNo: FirinPartiNoOzetle(gruplar),
                genisFirinPartiAlani: true));

            page.Content().Column(column =>
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(32);
                        columns.ConstantColumn(48);
                        columns.ConstantColumn(38);
                        columns.ConstantColumn(84);
                        columns.RelativeColumn(2f);
                        columns.ConstantColumn(56);
                        columns.ConstantColumn(52);
                        columns.RelativeColumn(2.2f);
                    });

                    table.Header(header =>
                    {
                        ListeBaslikHucre(header.Cell(), "SIRA\nNO");
                        ListeBaslikHucre(header.Cell(), "KOLİ\nNO");
                        ListeBaslikHucre(header.Cell(), "ADET");
                        ListeBaslikHucre(header.Cell(), "AMBALAJ\nCİNSİ");
                        ListeBaslikHucre(header.Cell(), "EBATLAR");
                        ListeBaslikHucre(header.Cell(), "ÇAM\nMİKTARI m³");
                        ListeBaslikHucre(header.Cell(), "AĞIRLIK\nBRÜT KG");
                        ListeBaslikHucre(header.Cell(), "KULLANIM YERİ");
                    });

                    for (var index = 0; index < gruplar.Count; index++)
                    {
                        var grup = gruplar[index];
                        ListeVeriHucre(table.Cell(), (index + 1).ToString(CultureInfo.InvariantCulture), true);
                        ListeVeriHucre(table.Cell(), grup.SandikNo, true);
                        ListeVeriHucre(table.Cell(), grup.Adet.ToString(CultureInfo.InvariantCulture), true);
                        ListeVeriHucre(table.Cell(), grup.Temsilci.SandikCinsi);
                        ListeOlcuHucre(table.Cell(), OlcuMetni(grup.Temsilci.DisOlculer));
                        ListeVeriHucre(table.Cell(), FormatM3(grup.NetM3), true);
                        ListeVeriHucre(table.Cell(), grup.Temsilci.BrutKg.HasValue
                            ? FormatAdet(grup.Temsilci.BrutKg.Value)
                            : string.Empty);
                        ListeVeriHucre(table.Cell(), grup.Temsilci.KullanimAmaci ?? grup.Temsilci.SandikAdi ?? "-");
                    }
                });

                column.Item().PaddingTop(6).ShowEntire().Background("#DCE8F8")
                    .Border(1).BorderColor(Colors.Blue.Darken3).Column(ozet =>
                    {
                        ozet.Item().Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text($"TOPLAM SANDIK: {gruplar.Sum(g => g.Adet)} Ad.")
                                .ExtraBold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            row.RelativeItem().AlignRight().Text($"TOPLAM ÇAM: {FormatM3(gruplar.Sum(g => g.NetM3))} m³")
                                .ExtraBold().FontSize(11).FontColor(Colors.Blue.Darken3);
                        });
                        ozet.Item().BorderTop(1).BorderColor(Colors.Blue.Darken3).Row(row =>
                        {
                            row.RelativeItem(0.9f).BorderRight(1).BorderColor(Colors.Blue.Darken3)
                                .PaddingVertical(4).PaddingHorizontal(6)
                                .Text("KOD NO: FC500208").ExtraBold().FontSize(9).FontColor(Colors.Blue.Darken3);
                            row.RelativeItem(1.25f).BorderRight(1).BorderColor(Colors.Blue.Darken3)
                                .PaddingVertical(4).PaddingHorizontal(6)
                                .Text($"SARF KERESTE: {FormatM3(gruplar.Sum(g => g.SarfM3))} m³")
                                .ExtraBold().FontSize(9).FontColor(Colors.Blue.Darken3);
                            row.RelativeItem(1.15f).PaddingVertical(4).PaddingHorizontal(6)
                                .Text($"TOPLAM: {FormatM3(gruplar.Sum(g => g.ToplamM3))} m³")
                                .ExtraBold().FontSize(9).FontColor(Colors.Blue.Darken3);
                        });
                    });
            });

            page.Footer().Element(container => RaporAltbilgisi(container, form));
        }

        private static void DetaySayfasi(
            PageDescriptor page,
            AmbalajUretimFormuModel form,
            AmbalajUretimGrubu grup,
            DateTime raporTarihi)
        {
            SayfaAyarlari(page);
            page.Header().Element(header => RaporBasligi(
                header,
                "AMBALAJ ÜRETİM DETAYI",
                form,
                raporTarihi,
                grup.SandikNo,
                grup.Temsilci.FirinPartiNo));

            page.Content().Column(column =>
            {
                column.Spacing(8);
                column.Item().PaddingBottom(8).Background("#EAF0F8").Padding(8).Column(info =>
                {
                    info.Item().Text($"SANDIK NO: {grup.SandikNo}   |   ADET: {grup.Adet}   |   TİP: {grup.Temsilci.SandikTuru}")
                        .Bold().FontSize(10).FontColor(Colors.Blue.Darken3);
                    info.Item().PaddingTop(2).Text($"KULLANIM AMACI: {grup.Temsilci.KullanimAmaci ?? grup.Temsilci.SandikAdi ?? "-"}")
                        .FontSize(9);
                    if (!string.IsNullOrWhiteSpace(grup.Temsilci.TalimatVeren) || !string.IsNullOrWhiteSpace(grup.Temsilci.Aciklama))
                    {
                        info.Item().Text($"TALİMAT VEREN: {grup.Temsilci.TalimatVeren ?? "-"}   |   AÇIKLAMA: {grup.Temsilci.Aciklama ?? "-"}")
                            .FontSize(8);
                    }
                });

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.25f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.35f);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(75);
                    });
                    BaslikHucre(table.Cell(), string.Empty);
                    BaslikHucre(table.Cell(), "ÖLÇÜ");
                    BaslikHucre(table.Cell(), "AÇIKLAMA");
                    BaslikHucre(table.Cell(), "ADET");
                    BaslikHucre(table.Cell(), "MALZEME");

                    VeriHucre(table.Cell(), "SANDIK İÇ EBATLARI", true);
                    VeriHucre(table.Cell(), OlcuMetni(grup.Temsilci.IcOlculer), true);
                    VeriHucre(table.Cell(), "BOY × EN × YÜK.");
                    VeriHucre(table.Cell(), grup.Adet.ToString(CultureInfo.InvariantCulture), true);
                    VeriHucre(table.Cell(), "ÇAM");

                    VeriHucre(table.Cell(), "SANDIK DIŞ EBATLARI", true);
                    VeriHucre(table.Cell(), OlcuMetni(grup.Temsilci.DisOlculer), true);
                    VeriHucre(table.Cell(), "TOPLAM ÇAM HACMİ");
                    VeriHucre(table.Cell(), FormatM3(grup.NetM3), true);
                    VeriHucre(table.Cell(), "m³");

                    foreach (var parcaGrubu in new[] { "AP", "OD", "UT", "YD" })
                    {
                        if (parcaGrubu == "OD")
                        {
                            VeriHucre(table.Cell(), "ON_H1", true);
                            VeriHucre(table.Cell(), $"{FormatMm(grup.Temsilci.OnDuvarYuksekligi)} mm", true);
                            VeriHucre(table.Cell(), "ÖN DUVAR YÜKSEKLİĞİ");
                            VeriHucre(table.Cell(), string.Empty);
                            VeriHucre(table.Cell(), string.Empty);
                        }

                        foreach (var parca in grup.Parcalar.Where(p => p.Grup == parcaGrubu))
                        {
                            VeriHucre(table.Cell(), parca.Kod, true);
                            VeriHucre(table.Cell(), $"{FormatMm(parca.KesitEn)}×{FormatMm(parca.KesitYukseklik)}×{FormatMm(parca.Uzunluk)} mm", true);
                            VeriHucre(table.Cell(), parca.Aciklama);
                            VeriHucre(table.Cell(), FormatAdet(parca.TeorikAdet));
                            VeriHucre(table.Cell(), parca.Malzeme);
                        }

                        VeriHucre(table.Cell(), $"{parcaGrubu}_HACİM", true);
                        VeriHucre(table.Cell(), FormatM3(grup.Parcalar.Where(p => p.Grup == parcaGrubu).Sum(p => p.HacimM3)), true);
                        VeriHucre(table.Cell(), "m³");
                        VeriHucre(table.Cell(), string.Empty);
                        VeriHucre(table.Cell(), string.Empty);
                    }
                });

                column.Item().PaddingTop(7).ShowEntire().Element(container => UcBoyutluGorsel(container, grup));
            });

            page.Footer().Element(container => RaporAltbilgisi(container, form));
        }

        private static void CizimSayfasi(
            PageDescriptor page,
            AmbalajUretimFormuModel form,
            AmbalajUretimGrubu grup,
            DateTime raporTarihi)
        {
            SayfaAyarlari(page);
            page.Header().Element(header => SandikRaporBasligi(header, "TEKNİK ÜRETİM ÇİZİMLERİ", form, grup, raporTarihi));

            var ap1 = Parca(grup, "AP_1");
            var ap2 = Parca(grup, "AP_2");
            var ap3 = Parca(grup, "AP_3");
            var od4 = Parca(grup, "OD_4");
            var od5 = Parca(grup, "OD_5");
            var od10 = Parca(grup, "OD_10");
            var ut6 = Parca(grup, "UT_6");
            var ut7 = Parca(grup, "UT_7");
            var ut11 = Parca(grup, "UT_11");
            var yd8 = Parca(grup, "YD_8");
            var yd9 = Parca(grup, "YD_9");
            var yd13 = Parca(grup, "YD_13");

            page.Content().Column(column =>
            {
                column.Spacing(5);
                column.Item().Element(container => TeknikGorsel(
                    container,
                    "ÜST TAVAN KUŞAK GÖRÜNÜMÜ",
                    Varlik(AyakGorseliDosyaAdi("ust-tavan", grup.Temsilci.AyakAdedi)),
                    190,
                    new(
                        Alt: FormatMm(ut7.Uzunluk),
                        Sol: FormatMm(ut6.Uzunluk),
                        Sag: FormatMm(ap1.Uzunluk),
                        AltSag: "325",
                        Capraz: FormatMm(ut11.Uzunluk)),
                    2.11f));

                column.Item().Row(row =>
                {
                    row.RelativeItem(2).PaddingRight(4).Element(container => TeknikGorsel(
                        container,
                        "ÖN DUVAR KUŞAK GÖRÜNÜMÜ",
                        Varlik(AyakGorseliDosyaAdi("on-duvar", grup.Temsilci.AyakAdedi)),
                        142,
                        new(
                            Alt: FormatMm(od4.Uzunluk),
                            Sol: FormatMm(od5.Uzunluk),
                            Sag: FormatMm(grup.Temsilci.OnDuvarYuksekligi),
                            AltSag: "346",
                            Capraz: FormatMm(od10.Uzunluk))));
                    row.RelativeItem().Element(container => TeknikGorsel(
                        container,
                        "YAN DUVAR GÖRÜNÜMÜ",
                        Varlik($"yan-duvar-{Varyant(grup.Temsilci.YanKusakAdedi, 2, 4)}.jpg"),
                        142,
                        new(
                            Alt: FormatMm(yd9.Uzunluk),
                            Sol: FormatMm(grup.Temsilci.OnDuvarYuksekligi),
                            Sag: FormatMm(yd8.Uzunluk),
                            Capraz: FormatMm(yd13.Uzunluk))));
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().PaddingRight(4).Element(container => TeknikGorsel(
                        container,
                        "ALT PALET ÖN GÖRÜNÜMÜ",
                        Varlik(AyakGorseliDosyaAdi("alt-ayak", grup.Temsilci.AyakAdedi)),
                        82,
                        new(Ust: FormatMm(ap2.Uzunluk), AltSol: "300")));
                    row.RelativeItem().Element(container => TeknikGorsel(
                        container,
                        "ALT PALET YAN GÖRÜNÜMÜ",
                        Varlik($"ust-kizak-{Varyant(grup.Temsilci.UstKizakAdedi, 2, 6)}.jpg"),
                        82,
                        new(
                            Ust: FormatMm(ap3.Uzunluk),
                            Alt: FormatMm(ap1.Uzunluk),
                            Sol: "209",
                            UstSag: "23")));
                });

                column.Item().Element(container => TeknikGorsel(
                    container,
                    "ALT PALET ÜST GÖRÜNÜMÜ",
                    Varlik(AyakGorseliDosyaAdi("palet-ust", grup.Temsilci.AyakAdedi)),
                    155,
                    new(Ust: FormatMm(ap2.Uzunluk), Sol: FormatMm(ap1.Uzunluk), Sag: FormatMm(ap3.Uzunluk)),
                    2.21f));
            });

            page.Footer().Element(container => SandikAltbilgisi(container, grup));
        }

        private static void RaporBasligi(
            IContainer container,
            string baslik,
            AmbalajUretimFormuModel form,
            DateTime raporTarihi,
            string? sandikNo = null,
            string? firinPartiNo = null,
            bool genisFirinPartiAlani = false)
        {
            container.Column(column =>
            {
                column.Item().Background(Colors.Blue.Darken3).Padding(8).Row(row =>
                {
                    row.ConstantItem(145).Element(MarkaAmblemi);
                    row.RelativeItem().PaddingLeft(12).AlignMiddle().Column(title =>
                    {
                        title.Item().Text(baslik).Bold().FontSize(genisFirinPartiAlani ? 15 : 14).FontColor(Colors.White);
                        title.Item().PaddingTop(2).Text($"PROJE NO: {form.ProjeNo}")
                            .Bold().FontSize(10).FontColor(Colors.White);
                        title.Item().Text($"FB: {form.FBNo ?? "-"}")
                            .FontSize(genisFirinPartiAlani ? 9 : 8).FontColor("#DCE8F8");
                    });
                });

                column.Item().PaddingTop(6).PaddingBottom(8).Row(row =>
                {
                    row.RelativeItem().Text($"FİRMA: {form.ProjeAdi ?? "-"}").Bold().FontSize(9);
                    if (!string.IsNullOrWhiteSpace(sandikNo))
                        row.AutoItem().PaddingRight(18).Text($"SANDIK: {sandikNo}").Bold().FontSize(9);
                    var firinPartiAlani = genisFirinPartiAlani ? row.ConstantItem(190) : row.AutoItem();
                    firinPartiAlani.PaddingRight(18).Text($"FIRIN PARTİ NO: {firinPartiNo ?? "-"}")
                        .Bold().FontSize(9).FontColor(Colors.Blue.Darken2);
                    row.AutoItem().Text($"RAPOR TARİHİ: {raporTarihi:dd.MM.yyyy HH:mm}")
                        .FontSize(genisFirinPartiAlani ? 9 : 8).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private static void SandikRaporBasligi(
            IContainer container,
            string baslik,
            AmbalajUretimFormuModel form,
            AmbalajUretimGrubu grup,
            DateTime raporTarihi)
        {
            container.Column(column =>
            {
                column.Item().Background(Colors.Blue.Darken3).Padding(8).Row(row =>
                {
                    row.ConstantItem(145).Element(MarkaAmblemi);
                    row.RelativeItem().PaddingLeft(12).AlignMiddle().Column(title =>
                    {
                        title.Item().Text(baslik).Bold().FontSize(14).FontColor(Colors.White);
                        title.Item().PaddingTop(2).Text($"PROJE NO: {form.ProjeNo}  |  SANDIK: {grup.SandikNo}")
                            .Bold().FontSize(10).FontColor(Colors.White);
                        title.Item().Text($"{grup.Temsilci.SandikCinsi}  |  {grup.Adet} Ad.  |  Fırın Parti No: {grup.Temsilci.FirinPartiNo ?? "-"}  |  {grup.Temsilci.SandikTuru}")
                            .FontSize(7.5f).FontColor("#DCE8F8");
                    });
                    row.AutoItem().AlignBottom().Text(raporTarihi.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture))
                        .FontSize(7).FontColor(Colors.Grey.Lighten3);
                });

                column.Item().PaddingTop(6).PaddingBottom(6).Background("#EAF0F8").Padding(6).Row(row =>
                {
                    row.RelativeItem().Text($"İÇ ÖLÇÜ: {OlcuMetni(grup.Temsilci.IcOlculer)}").Bold().FontSize(9);
                    row.RelativeItem().AlignRight().Text($"DIŞ ÖLÇÜ: {OlcuMetni(grup.Temsilci.DisOlculer)}").Bold().FontSize(9);
                });
            });
        }

        private static void MarkaAmblemi(IContainer container)
        {
            container.Height(50).PaddingLeft(4).PaddingVertical(4).Column(logo =>
            {
                logo.Item().Text("3K")
                    .Bold().FontSize(22).FontColor(Colors.White);
                logo.Item().Text("All Processes. One Flow.")
                    .Italic().FontSize(6.5f).FontColor(Colors.White);
            });
        }

        private static void TeknikGorsel(
            IContainer container,
            string baslik,
            byte[] gorsel,
            float yukseklik,
            AmbalajGorselOlculeri olculer,
            float? enBoyOrani = null)
        {
            container.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(column =>
            {
                column.Item().Background("#EAF0F8").PaddingVertical(2.5f).PaddingHorizontal(5).Row(header =>
                {
                    header.RelativeItem().AlignMiddle().Text(baslik)
                        .Bold().FontSize(8).FontColor(Colors.Blue.Darken3);
                    if (!string.IsNullOrWhiteSpace(olculer.Capraz))
                    {
                        header.AutoItem().PaddingLeft(5).AlignMiddle().Text($"ÇAPRAZ {olculer.Capraz} mm")
                            .ExtraBold().FontSize(7).FontColor("#145487");
                    }
                });
                var gorselAlani = column.Item().Height(yukseklik).Padding(3);
                if (enBoyOrani.HasValue)
                    gorselAlani = gorselAlani.AlignCenter().Width(yukseklik * enBoyOrani.Value);

                gorselAlani.Layers(layers =>
                {
                    layers.PrimaryLayer().Image(gorsel).FitArea();
                    if (!string.IsNullOrWhiteSpace(olculer.Ust))
                        layers.Layer().AlignTop().AlignCenter().Element(c => OlcuEtiketi(c, olculer.Ust));
                    if (!string.IsNullOrWhiteSpace(olculer.Alt))
                        layers.Layer().AlignBottom().AlignCenter().Element(c => OlcuEtiketi(c, olculer.Alt));
                    if (!string.IsNullOrWhiteSpace(olculer.Sol))
                        layers.Layer().AlignLeft().AlignMiddle().RotateLeft().Element(c => OlcuEtiketi(c, olculer.Sol));
                    if (!string.IsNullOrWhiteSpace(olculer.Sag))
                        layers.Layer().AlignRight().AlignMiddle().RotateLeft().Element(c => OlcuEtiketi(c, olculer.Sag));
                    if (!string.IsNullOrWhiteSpace(olculer.UstSag))
                        layers.Layer().AlignTop().AlignRight().PaddingTop(2).Element(c => OlcuEtiketi(c, olculer.UstSag));
                    if (!string.IsNullOrWhiteSpace(olculer.AltSol))
                        layers.Layer().AlignBottom().AlignLeft().PaddingBottom(2).Element(c => OlcuEtiketi(c, olculer.AltSol));
                    if (!string.IsNullOrWhiteSpace(olculer.AltSag))
                        layers.Layer().AlignBottom().AlignRight().PaddingBottom(2).Element(c => OlcuEtiketi(c, olculer.AltSag));
                });
            });
        }

        private static void UcBoyutluGorsel(IContainer container, AmbalajUretimGrubu grup)
        {
            container.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(column =>
            {
                column.Item().Background("#EAF0F8").PaddingVertical(2.5f).AlignCenter()
                    .Text("NUMARALI 3B SANDIK MONTAJ GÖRÜNÜMÜ").Bold().FontSize(8).FontColor(Colors.Blue.Darken3);
                column.Item().Height(185).AlignCenter().Width(396).Layers(layers =>
                {
                    var dosyaAdi = UcBoyutluGorselDosyaAdi(grup.Temsilci.AyakAdedi);
                    layers.PrimaryLayer().Image(Varlik(dosyaAdi)).FitArea();
                    layers.Layer().AlignTop().AlignLeft().PaddingLeft(72).PaddingTop(3)
                        .Element(c => OlcuEtiketi(c, FormatMm(grup.Temsilci.DisOlculer.En)));
                    layers.Layer().AlignLeft().AlignMiddle().RotateLeft()
                        .Element(c => OlcuEtiketi(c, FormatMm(grup.Temsilci.DisOlculer.Yukseklik)));
                    layers.Layer().AlignBottom().AlignCenter().PaddingBottom(1)
                        .Element(c => OlcuEtiketi(c, FormatMm(grup.Temsilci.DisOlculer.Boy)));
                    layers.Layer().AlignRight().AlignMiddle().PaddingRight(2)
                        .Element(c => OlcuEtiketi(c, FormatMm(grup.Temsilci.OnDuvarYuksekligi)));
                });
            });
        }

        private static void OlcuEtiketi(IContainer container, string deger) =>
            container.Background(Colors.White).Border(0.7f).BorderColor("#1E6EAF")
                .PaddingHorizontal(4).PaddingVertical(2)
                .Text($"{deger} mm").ExtraBold().FontSize(9.5f).FontColor("#145487");

        private static void BaslikHucre(IContainer container, string text) =>
            container.Border(0.5f).BorderColor(Colors.Blue.Darken3).Background(Colors.Blue.Darken3)
                .Padding(4).AlignCenter().AlignMiddle().Text(text).Bold().FontColor(Colors.White).FontSize(8.5f);

        private static void ListeBaslikHucre(IContainer container, string text) =>
            container.Border(0.5f).BorderColor(Colors.Blue.Darken3).Background(Colors.Blue.Darken3)
                .Padding(3).AlignCenter().AlignMiddle().Text(text).Bold().FontColor(Colors.White).FontSize(9);

        private static void VeriHucre(IContainer container, string text, bool bold = false)
        {
            var cell = container.Border(0.5f).Padding(3).AlignMiddle();
            if (bold)
                cell.Text(text).Bold().FontSize(8.5f);
            else
                cell.Text(text).FontSize(8.5f);
        }

        private static void ListeVeriHucre(IContainer container, string text, bool bold = false)
        {
            var cell = container.Border(0.5f).Padding(3).AlignMiddle();
            if (bold)
                cell.Text(text).Bold().FontSize(9);
            else
                cell.Text(text).FontSize(9);
        }

        private static void ListeOlcuHucre(IContainer container, string text) =>
            container.Border(0.5f).PaddingHorizontal(3).PaddingVertical(2).AlignMiddle()
                .Text(text).FontSize(9);

        private static void SayfaAyarlari(PageDescriptor page)
        {
            page.Size(PageSizes.A4);
            page.Margin(24);
            page.DefaultTextStyle(style => style.FontSize(8).FontColor("#172033"));
        }

        private static void ListeSayfaAyarlari(PageDescriptor page)
        {
            page.Size(PageSizes.A4);
            page.Margin(18);
            page.DefaultTextStyle(style => style.FontSize(8).FontColor("#172033"));
        }

        private static void RaporAltbilgisi(IContainer container, AmbalajUretimFormuModel form)
        {
            container.BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"3K Ambalaj | {form.ProjeNo} | {form.ProjeAdi ?? "-"}")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
                SayfaNumarasi(row.AutoItem());
            });
        }

        private static void SandikAltbilgisi(IContainer container, AmbalajUretimGrubu grup)
        {
            container.BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"3K Ambalaj | Teknik Üretim Çizimleri | Sandık {grup.SandikNo}")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
                SayfaNumarasi(row.AutoItem());
            });
        }

        private static void SayfaNumarasi(IContainer container) => container.Text(text =>
        {
            text.Span("Sayfa ").FontSize(7);
            text.CurrentPageNumber().FontSize(7);
            text.Span(" / ").FontSize(7);
            text.TotalPages().FontSize(7);
        });

        private static byte[] Varlik(string dosyaAdi)
        {
            if (!string.Equals(Path.GetFileName(dosyaAdi), dosyaAdi, StringComparison.Ordinal))
                throw new ArgumentException("Geçersiz ambalaj rapor varlığı adı.", nameof(dosyaAdi));

            return VarlikOnbellegi.GetOrAdd(
                dosyaAdi,
                static ad => new Lazy<byte[]>(() =>
                {
                    var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Ambalaj", ad);
                    if (!File.Exists(path))
                        throw new FileNotFoundException($"Ambalaj rapor varlığı bulunamadı: {ad}", path);
                    return File.ReadAllBytes(path);
                }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;
        }

        private static AmbalajUretimFormuParcasiModel Parca(AmbalajUretimGrubu grup, string kod) =>
            grup.Parcalar.Single(parca => string.Equals(parca.Kod, kod, StringComparison.Ordinal));

        private static string FirinPartiNoOzetle(IEnumerable<AmbalajUretimGrubu> gruplar)
        {
            var degerler = gruplar.Select(g => g.Temsilci.FirinPartiNo)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return degerler.Count == 0 ? "-" : string.Join(" / ", degerler);
        }

        private static int Varyant(int deger, int minimum, int maksimum) => Math.Clamp(deger, minimum, maksimum);

        private static string AyakGorseliDosyaAdi(string onEk, int ayakAdedi) =>
            ayakAdedi >= 7
                ? $"{onEk}-7.png"
                : $"{onEk}-{Varyant(ayakAdedi, 2, 6)}.jpg";

        private static string UcBoyutluGorselDosyaAdi(int ayakAdedi) => ayakAdedi switch
        {
            <= 2 => "3d-2.png",
            3 => "3d-3.png",
            4 => "3d-4.png",
            5 => "3boyut52.jpg",
            6 => "3d-6.png",
            _ => "3d-7.png"
        };

        private static string OlcuMetni(AmbalajOlculeri olculer) =>
            $"{FormatMm(olculer.Boy)} × {FormatMm(olculer.En)} × {FormatMm(olculer.Yukseklik)}\u00A0mm";
        private static string FormatMm(decimal value) => Math.Round(value, 0).ToString("0", CultureInfo.InvariantCulture);
        private static string FormatM3(decimal value) => value.ToString("0.000", CultureInfo.GetCultureInfo("tr-TR"));
        private static string FormatAdet(decimal value) => decimal.Truncate(value) == value
            ? decimal.Truncate(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.InvariantCulture);

        private sealed record AmbalajGorselOlculeri(
            string? Ust = null,
            string? Alt = null,
            string? Sol = null,
            string? Sag = null,
            string? UstSag = null,
            string? AltSol = null,
            string? AltSag = null,
            string? Capraz = null);
    }
}
