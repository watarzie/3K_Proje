using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    public sealed class GetAmbalajUretimFormuQueryHandler
        : IRequestHandler<GetAmbalajUretimFormuQuery, Result<AmbalajUretimFormuModel>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAmbalajUretimFormuQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<Result<AmbalajUretimFormuModel>> Handle(
            GetAmbalajUretimFormuQuery request,
            CancellationToken cancellationToken)
        {
            return await AmbalajUretimFormuOlusturucu.OlusturAsync(
                _unitOfWork, request.KayitId, request.ProjeId, request.ManuelProjeNo, cancellationToken);
        }
    }

    public sealed class GetAmbalajUretimFormuDosyasiQueryHandler
        : IRequestHandler<GetAmbalajUretimFormuDosyasiQuery, Result<AmbalajDosyaDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAmbalajRaporDosyaService _dosyaService;

        public GetAmbalajUretimFormuDosyasiQueryHandler(
            IUnitOfWork unitOfWork,
            IAmbalajRaporDosyaService dosyaService)
        {
            _unitOfWork = unitOfWork;
            _dosyaService = dosyaService;
        }

        public async Task<Result<AmbalajDosyaDto>> Handle(
            GetAmbalajUretimFormuDosyasiQuery request,
            CancellationToken cancellationToken)
        {
            var result = await AmbalajUretimFormuOlusturucu.OlusturAsync(
                _unitOfWork,
                request.KayitId,
                request.ProjeId,
                request.ManuelProjeNo,
                cancellationToken,
                request.KayitIdleri,
                request.Tur,
                request.BagimsizKayitMi);
            if (!result.IsSuccess)
                return Result<AmbalajDosyaDto>.Failure(result.Error!.Message, result.StatusCode);

            var form = result.Value!;
            var safeProject = string.Concat((form.ProjeNo.Length == 0 ? "bagimsiz" : form.ProjeNo)
                .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character));
            var zaman = TurkeyTime.Now.ToString("yyyyMMdd-HHmmss");
            if (string.Equals(request.Format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return Result<AmbalajDosyaDto>.Success(new AmbalajDosyaDto(
                    _dosyaService.UretimFormuExcelOlustur(form),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ambalaj-uretim-formu-{safeProject}-{zaman}.xlsx"));
            }

            return Result<AmbalajDosyaDto>.Success(new AmbalajDosyaDto(
                _dosyaService.UretimFormuPdfOlustur(form),
                "application/pdf",
                $"ambalaj-uretim-formu-{safeProject}-{zaman}.pdf"));
        }
    }

    internal static class AmbalajUretimFormuOlusturucu
    {
        public static async Task<Result<AmbalajUretimFormuModel>> OlusturAsync(
            IUnitOfWork unitOfWork,
            int? kayitId,
            int? projeId,
            string? manuelProjeNo = null,
            CancellationToken cancellationToken = default,
            IReadOnlyCollection<int>? kayitIdleri = null,
            AmbalajSandikTuru? tur = null,
            bool? bagimsizKayitMi = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (kayitIdleri?.Any(id => id <= 0) == true)
                return Result<AmbalajUretimFormuModel>.Failure(
                    "Seçili kayıt kimlikleri sıfırdan büyük olmalıdır.",
                    400);

            Proje? proje = null;
            List<AmbalajUretimKaydi> kayitlar;
            var tekilSecimIdleri = kayitIdleri?
                .Distinct()
                .ToArray() ?? [];
            if (tekilSecimIdleri.Length > GetAmbalajUretimFormuDosyasiQuery.EnFazlaSecilebilirKayit)
                return Result<AmbalajUretimFormuModel>.Failure(
                    $"Tek seferde en fazla {GetAmbalajUretimFormuDosyasiQuery.EnFazlaSecilebilirKayit} sandık seçilebilir.",
                    400);

            var seciciSayisi = new[]
            {
                tekilSecimIdleri.Length > 0,
                kayitId.HasValue,
                projeId.HasValue,
                !string.IsNullOrWhiteSpace(manuelProjeNo)
            }.Count(secili => secili);
            if (seciciSayisi != 1)
                return Result<AmbalajUretimFormuModel>.Failure(
                    "Üretim formu için kayıt, proje, manuel proje numarası veya seçili kayıt listesinden yalnız biri belirtilmelidir.",
                    400);

            if (tekilSecimIdleri.Length > 0)
            {
                kayitlar = (await unitOfWork.GetRepository<AmbalajUretimKaydi>().FindAsync(
                        k => tekilSecimIdleri.Contains(k.Id)))
                    .ToList();

                var bulunanIdler = kayitlar.Select(k => k.Id).ToHashSet();
                var bulunamayanIdler = tekilSecimIdleri.Where(id => !bulunanIdler.Contains(id)).ToArray();
                if (bulunamayanIdler.Length > 0)
                    return Result<AmbalajUretimFormuModel>.Failure(
                        $"Seçilen ambalaj üretim kayıtları bulunamadı: {KimlikleriYaz(bulunamayanIdler)}.",
                        404);

                var uygunOlmayanlar = kayitlar
                    .Where(k => k.IptalMi || !k.AmbalajaDahil || !k.UretimeAlindi)
                    .OrderBy(k => k.Id)
                    .ToArray();
                if (uygunOlmayanlar.Length > 0)
                    return Result<AmbalajUretimFormuModel>.Failure(
                        "Seçilen sandıkların tamamı aktif, ambalaja dahil ve üretime alınmış olmalıdır. " +
                        $"Uygun olmayan kayıtlar: {KayitlariYaz(uygunOlmayanlar)}.",
                        409);

                var sistemProjeIdleri = kayitlar
                    .Where(k => k.ProjeId.HasValue)
                    .Select(k => k.ProjeId!.Value)
                    .Distinct()
                    .ToArray();
                var manuelKayitVar = kayitlar.Any(k => !k.ProjeId.HasValue);
                if (sistemProjeIdleri.Length > 1 || (sistemProjeIdleri.Length == 1 && manuelKayitVar))
                    return Result<AmbalajUretimFormuModel>.Failure(
                        "Seçilen sandıklar farklı projelere ait. Toplu üretim formu yalnız aynı proje altındaki sandıklar için oluşturulabilir.",
                        409);

                if (sistemProjeIdleri.Length == 1)
                {
                    proje = await unitOfWork.GetRepository<Proje>().GetByIdAsync(sistemProjeIdleri[0]);
                    if (proje == null)
                        return Result<AmbalajUretimFormuModel>.Failure("Seçilen sandıkların projesi bulunamadı.", 404);
                }
                else
                {
                    var manuelProjeAnahtarlari = kayitlar
                        .Select(k => $"{k.ManuelProjeNo?.Trim()}\u001f{k.ManuelProjeAdi?.Trim()}")
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    if (manuelProjeAnahtarlari.Length > 1)
                        return Result<AmbalajUretimFormuModel>.Failure(
                            "Seçilen sandıklar farklı manuel projelere ait. Toplu üretim formu yalnız aynı manuel proje altındaki sandıklar için oluşturulabilir.",
                            409);
                }

                kayitlar = kayitlar
                    .OrderBy(k => k.Tur)
                    .ThenBy(k => k.SandikNo)
                    .ThenBy(k => k.Id)
                    .ToList();
            }
            else if (projeId.HasValue)
            {
                proje = await unitOfWork.GetRepository<Proje>().GetByIdAsync(projeId.Value);
                if (proje == null)
                    return Result<AmbalajUretimFormuModel>.Failure("Proje bulunamadı.", 404);
                kayitlar = (await unitOfWork.GetRepository<AmbalajUretimKaydi>().FindAsync(
                        k => k.ProjeId == projeId.Value && !k.IptalMi && k.AmbalajaDahil && k.UretimeAlindi &&
                             (!tur.HasValue || k.Tur == tur.Value) &&
                             (!bagimsizKayitMi.HasValue || k.BagimsizKayitMi == bagimsizKayitMi.Value)))
                    .OrderBy(k => k.Tur)
                    .ThenBy(k => k.SandikNo)
                    .ToList();
            }
            else if (!string.IsNullOrWhiteSpace(manuelProjeNo))
            {
                var normalizeNo = manuelProjeNo.Trim();
                kayitlar = (await unitOfWork.GetRepository<AmbalajUretimKaydi>().FindAsync(
                        k => !k.ProjeId.HasValue && k.ManuelProjeNo == normalizeNo &&
                             !k.IptalMi && k.AmbalajaDahil && k.UretimeAlindi))
                    .OrderBy(k => k.Tur).ThenBy(k => k.SandikNo).ThenBy(k => k.Id)
                    .ToList();
            }
            else
            {
                var kayit = await unitOfWork.GetRepository<AmbalajUretimKaydi>().GetByIdAsync(kayitId!.Value);
                if (kayit == null)
                    return Result<AmbalajUretimFormuModel>.Failure("Ambalaj üretim kaydı bulunamadı.", 404);
                if (kayit.IptalMi || !kayit.AmbalajaDahil || !kayit.UretimeAlindi)
                    return Result<AmbalajUretimFormuModel>.Failure(
                        "Üretim formu yalnız aktif, ambalaja dahil ve üretime alınmış kayıtlar için oluşturulabilir.",
                        409);
                kayitlar = [kayit];
                if (kayit.ProjeId.HasValue)
                    proje = await unitOfWork.GetRepository<Proje>().GetByIdAsync(kayit.ProjeId.Value);
            }

            if (kayitlar.Count == 0)
                return Result<AmbalajUretimFormuModel>.Failure("Üretime alınmış ambalaj kaydı bulunamadı.", 404);
            var uyumsuzFormul = kayitlar.FirstOrDefault(k => !string.Equals(
                k.M3HesaplamaVersiyonu,
                AmbalajHesaplayici.FormulVersiyonu,
                StringComparison.Ordinal));
            if (uyumsuzFormul != null)
                return Result<AmbalajUretimFormuModel>.Failure(
                    $"{uyumsuzFormul.SandikNo} numaralı sandığın {uyumsuzFormul.M3HesaplamaVersiyonu} formül sürümüne ait parça snapshotı bulunmuyor. " +
                    $"Güncel {AmbalajHesaplayici.FormulVersiyonu} formülü eski sürüm etiketiyle kullanılamaz.",
                    409);
            var eksik = kayitlar.FirstOrDefault(k => !AmbalajUretimYardimcilari.OlculerGecerli(k));
            if (eksik != null)
                return Result<AmbalajUretimFormuModel>.Failure(
                    $"{eksik.SandikNo} numaralı sandığın ölçüleri eksik olduğu için üretim formu oluşturulamadı.",
                    409);

            var kaynakSandikIds = kayitlar
                .Where(k => k.KaynakModul == AmbalajKaynakModulu.Sandik && k.KaynakKayitId.HasValue)
                .Select(k => k.KaynakKayitId!.Value)
                .Distinct()
                .ToList();
            var brutKgMap = kaynakSandikIds.Count == 0
                ? new Dictionary<int, decimal?>()
                : (await unitOfWork.GetRepository<Sandik>().FindAsync(s => kaynakSandikIds.Contains(s.Id)))
                    .ToDictionary(s => s.Id, s => s.GrossKg);

            cancellationToken.ThrowIfCancellationRequested();
            var kalemler = kayitlar.Select(k => KalemOlustur(k, brutKgMap)).ToList();
            var ilk = kayitlar[0];
            return Result<AmbalajUretimFormuModel>.Success(new AmbalajUretimFormuModel
            {
                ProjeId = proje?.Id,
                ProjeNo = proje?.ProjeNo ?? ilk.ManuelProjeNo ?? "BAĞIMSIZ",
                ProjeAdi = proje?.Musteri ?? ilk.ManuelProjeAdi,
                FBNo = proje?.FBNo,
                Kalemler = kalemler,
                NetM3 = kalemler.Sum(k => k.NetM3),
                SarfM3 = kalemler.Sum(k => k.SarfM3),
                ToplamM3 = kalemler.Sum(k => k.ToplamM3)
            });
        }

        private static string KimlikleriYaz(IEnumerable<int> kimlikler)
        {
            const int gosterilecekKayitSayisi = 10;
            var liste = kimlikler.Take(gosterilecekKayitSayisi).ToArray();
            var metin = string.Join(", ", liste);
            return kimlikler.Skip(gosterilecekKayitSayisi).Any() ? $"{metin}, ..." : metin;
        }

        private static string KayitlariYaz(IEnumerable<AmbalajUretimKaydi> kayitlar)
        {
            const int gosterilecekKayitSayisi = 10;
            var liste = kayitlar.Take(gosterilecekKayitSayisi)
                .Select(k => string.IsNullOrWhiteSpace(k.SandikNo) ? $"ID {k.Id}" : $"{k.SandikNo} (ID {k.Id})")
                .ToArray();
            var metin = string.Join(", ", liste);
            return kayitlar.Skip(gosterilecekKayitSayisi).Any() ? $"{metin}, ..." : metin;
        }

        private static AmbalajUretimFormuKalemiModel KalemOlustur(
            AmbalajUretimKaydi kayit,
            IReadOnlyDictionary<int, decimal?> brutKgMap)
        {
            var ayakProfili = AmbalajAyakProfiliBelirleyici.Belirle(kayit.Ad, kayit.KullanimAmaci);
            var icOlculer = AmbalajUretimYardimcilari.HesaplamaIcOlculeriniGetir(kayit);
            var hesap = AmbalajHesaplayici.Hesapla(
                icOlculer.Boy,
                icOlculer.En,
                icOlculer.Yukseklik,
                ayakProfili,
                AmbalajUretimYardimcilari.KaynakSandikOlculeriMi(kayit) ? kayit.Boy : null);
            return new AmbalajUretimFormuKalemiModel
            {
                KayitId = kayit.Id,
                IsAkisKimligi = kayit.IsAkisKimligi,
                SandikNo = kayit.SandikNo,
                SandikAdi = kayit.Ad,
                SandikTuru = AmbalajUretimYardimcilari.TurMetni(kayit.Tur),
                SandikCinsi = AmbalajUretimYardimcilari.CinsMetni(kayit.SandikCinsi, kayit.DigerSandikCinsi),
                Adet = kayit.Adet,
                BrutKg = kayit.KaynakKayitId.HasValue && brutKgMap.TryGetValue(kayit.KaynakKayitId.Value, out var brutKg)
                    ? brutKg
                    : null,
                KullanimAmaci = kayit.KullanimAmaci,
                TalimatVeren = kayit.TalimatVeren,
                Aciklama = kayit.Aciklama,
                IcOlculer = hesap.IcOlculer,
                DisOlculer = hesap.DisOlculer,
                UstKizakAdedi = hesap.UstKizakAdedi,
                AyakAdedi = hesap.AyakAdedi,
                YanKusakAdedi = hesap.YanKusakAdedi,
                OnDuvarYuksekligi = hesap.OnDuvarYuksekligi,
                FormulVersiyonu = kayit.M3HesaplamaVersiyonu,
                HesaplananNetM3 = kayit.HesaplananToplamM3,
                M3Override = kayit.M3Override,
                NetM3 = kayit.M3Override ?? kayit.HesaplananToplamM3,
                SarfOrani = kayit.SarfOrani,
                SarfM3 = kayit.SarfM3,
                ToplamM3 = kayit.ToplamM3,
                FirinPartiNo = kayit.FirinPartiNo,
                UretimTarihi = kayit.UretimTarihi,
                Parcalar = hesap.Parcalar.Select(parca =>
                {
                    var teorikAdet = parca.Adet * kayit.Adet;
                    return new AmbalajUretimFormuParcasiModel
                    {
                        Kod = parca.Kod,
                        Grup = parca.Grup,
                        Aciklama = parca.Aciklama,
                        Malzeme = parca.Malzeme,
                        KesitEn = parca.KesitEn,
                        KesitYukseklik = parca.KesitYukseklik,
                        Uzunluk = parca.Uzunluk,
                        TeorikAdet = teorikAdet,
                        KesimAdedi = (int)Math.Ceiling(teorikAdet),
                        HacimM3 = Math.Round(parca.HacimM3 * kayit.Adet, 6, MidpointRounding.AwayFromZero)
                    };
                }).ToList()
            };
        }
    }
}
