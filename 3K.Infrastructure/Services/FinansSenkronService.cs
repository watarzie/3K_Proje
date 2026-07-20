using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public class FinansSenkronService : IFinansSenkronService
    {
        private const string KaynakModul = "AmbalajUretimKalemi";
        private const string BagimsizKaynakModul = "AmbalajBagimsizSandik";
        private const string SarfKeresteKaynakModul = "AmbalajSarfKereste";
        private const string SarfKeresteKodNo = "FC500208";
        private const decimal SarfKeresteOrani = 0.11m;
        private readonly AppDbContext _context;

        public FinansSenkronService(AppDbContext context)
        {
            _context = context;
        }

        public async Task UretimFormuAlindiAsync(int projeId, int? tur, CancellationToken cancellationToken = default)
        {
            if (tur.HasValue && tur is not (1 or 2 or 3))
                throw new ArgumentOutOfRangeException(nameof(tur));

            var proje = await _context.Projeler
                .Include(p => p.Sandiklar)
                .Include(p => p.AmbalajUretimPlani)!
                    .ThenInclude(p => p.Kalemler)
                .FirstOrDefaultAsync(p => p.Id == projeId, cancellationToken);
            if (proje == null)
                return;
            if (proje.ProjeTipiId != (int)ProjeTipi.Normal)
                return;

            var plan = proje.AmbalajUretimPlani ?? new AmbalajUretimPlani { ProjeId = projeId };
            if (proje.AmbalajUretimPlani == null)
            {
                _context.AmbalajUretimPlanlari.Add(plan);
                proje.AmbalajUretimPlani = plan;
            }

            var mevcutKayitlar = plan.Kalemler
                .Where(k => k.KaynakSandikId.HasValue)
                .ToDictionary(k => k.KaynakSandikId!.Value);
            foreach (var sandik in proje.Sandiklar.Where(s => s.AmbalajaDahilMi != false))
            {
                mevcutKayitlar.TryGetValue(sandik.Id, out var kalem);
                var kaynakTuru = plan.Id == 0 ? 1 : kalem?.Tur == 2 || sandik.CreatedDate > plan.CreatedDate ? 2 : kalem?.Tur ?? 1;
                if (tur.HasValue && kaynakTuru != tur.Value)
                    continue;

                if (kalem == null)
                {
                    kalem = new AmbalajUretimKalemi
                    {
                        KaynakSandikId = sandik.Id,
                        Tur = kaynakTuru,
                        UretimeAlindi = true
                    };
                    plan.Kalemler.Add(kalem);
                }

                kalem.SandikNo = sandik.SandikNo;
                kalem.Ad = sandik.Ad;
                kalem.Adet = SandikAdediHesapla(sandik.SandikNo);
                kalem.Boy = sandik.Boy ?? 0;
                kalem.En = sandik.En ?? 0;
                kalem.Yukseklik = sandik.Yukseklik ?? 0;
            }

            if (!tur.HasValue || tur == 1) plan.ProjeSandiklariDurumId = 2;
            if (!tur.HasValue || tur == 2) plan.IlaveSandiklarDurumId = 2;
            if (!tur.HasValue || tur == 3) plan.IcSandiklarDurumId = 2;
            plan.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);
            await ProjeyiSenkronizeEtAsync(projeId, cancellationToken);
        }

        public async Task TumunuSenkronizeEtAsync(CancellationToken cancellationToken = default)
        {
            var projeIds = await _context.AmbalajUretimPlanlari.AsNoTracking()
                .Where(p => p.Kalemler.Any(k => k.UretimeAlindi))
                .Select(p => p.ProjeId)
                .ToListAsync(cancellationToken);
            foreach (var projeId in projeIds)
                await ProjeyiSenkronizeEtAsync(projeId, cancellationToken);

            var bagimsizSandikIds = await _context.AmbalajBagimsizSandiklar.AsNoTracking()
                .Where(s => s.UretimeAlindi)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
            foreach (var sandikId in bagimsizSandikIds)
                await BagimsizSandigiSenkronizeEtAsync(sandikId, cancellationToken);
        }

        public async Task OzelUretimFormuAlindiAsync(int projeId, int tur, CancellationToken cancellationToken = default)
        {
            var sandiklar = await _context.AmbalajBagimsizSandiklar
                .Where(s => s.ProjeId == projeId && s.Tur == tur && s.UretimeAlindi)
                .ToListAsync(cancellationToken);
            foreach (var sandik in sandiklar)
                sandik.DurumId = 2;

            await _context.SaveChangesAsync(cancellationToken);
            foreach (var sandik in sandiklar)
                await BagimsizSandigiSenkronizeEtAsync(sandik.Id, cancellationToken);
        }

        public async Task ProjeyiSenkronizeEtAsync(int projeId, CancellationToken cancellationToken = default)
        {
            var proje = await _context.Projeler
                .AsNoTracking()
                .Include(p => p.AmbalajUretimPlani)!
                    .ThenInclude(p => p.Kalemler)
                .FirstOrDefaultAsync(p => p.Id == projeId, cancellationToken);
            if (proje == null)
                return;

            var kaynaklar = proje.ProjeTipiId == (int)ProjeTipi.Normal
                ? proje.AmbalajUretimPlani?.Kalemler.Where(k => k.UretimeAlindi).ToList() ?? []
                : [];
            var kaynakIds = kaynaklar.Select(k => k.Id).ToHashSet();
            var finansKayitlari = await _context.FinansIsKayitlari
                .Include(k => k.SiparisKalemleri)
                    .ThenInclude(k => k.Siparis)
                .Where(k => k.ProjeId == projeId && k.KaynakModul == KaynakModul)
                .ToListAsync(cancellationToken);

            foreach (var kaynak in kaynaklar)
            {
                var finansKaydi = finansKayitlari.FirstOrDefault(k => k.KaynakKayitId == kaynak.Id);
                if (finansKaydi == null)
                {
                    finansKaydi = new FinansIsKaydi
                    {
                        ProjeId = proje.Id,
                        KaynakKayitId = kaynak.Id,
                        KaynakModul = KaynakModul,
                        AktarimTarihi = DateTime.Now
                    };
                    _context.FinansIsKayitlari.Add(finansKaydi);
                    finansKayitlari.Add(finansKaydi);
                }

                finansKaydi.KaynakAktif = true;
                if (!AktifSipariseBagli(finansKaydi))
                    KaynaktanGuncelle(finansKaydi, kaynak, proje);
            }

            foreach (var finansKaydi in finansKayitlari.Where(k => k.KaynakKayitId.HasValue && !kaynakIds.Contains(k.KaynakKayitId.Value)))
                finansKaydi.KaynakAktif = false;

            await SarfKeresteyiSenkronizeEtAsync(proje, kaynaklar, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task SarfKeresteyiSenkronizeEtAsync(
            Proje proje,
            IReadOnlyCollection<AmbalajUretimKalemi> kaynaklar,
            CancellationToken cancellationToken)
        {
            var finansKaydi = await _context.FinansIsKayitlari
                .Include(k => k.SiparisKalemleri)
                    .ThenInclude(k => k.Siparis)
                .FirstOrDefaultAsync(k => k.KaynakModul == SarfKeresteKaynakModul && k.KaynakKayitId == proje.Id, cancellationToken);
            var toplamHacim = kaynaklar.Sum(k => k.Adet * BirimHacimHesapla(k));

            if (finansKaydi == null)
            {
                finansKaydi = new FinansIsKaydi
                {
                    ProjeId = proje.Id,
                    KaynakKayitId = proje.Id,
                    KaynakModul = SarfKeresteKaynakModul,
                    AktarimTarihi = DateTime.Now
                };
                _context.FinansIsKayitlari.Add(finansKaydi);
            }

            finansKaydi.KaynakAktif = toplamHacim > 0;
            if (AktifSipariseBagli(finansKaydi))
                return;

            finansKaydi.ProjeId = proje.Id;
            finansKaydi.ProjeNo = proje.ProjeNo;
            finansKaydi.Musteri = proje.Musteri ?? string.Empty;
            finansKaydi.SandikNo = SarfKeresteKodNo;
            finansKaydi.SandikAdi = "SARF KERESTE";
            finansKaydi.IsTuru = FinansIsTuru.SarfKereste;
            finansKaydi.Adet = 1;
            finansKaydi.BirimM3 = toplamHacim * SarfKeresteOrani;
            finansKaydi.UretimeAlinmaTarihi = kaynaklar.Count > 0
                ? kaynaklar.Max(k => k.UpdatedDate ?? k.CreatedDate)
                : DateTime.Now;
            finansKaydi.UretimDurumu = finansKaydi.KaynakAktif ? "Üretime Alındı" : "Üretim Dışı";
        }

        public async Task BagimsizSandigiSenkronizeEtAsync(int sandikId, CancellationToken cancellationToken = default)
        {
            var kaynak = await _context.AmbalajBagimsizSandiklar.AsNoTracking()
                .Include(s => s.Proje)
                .FirstOrDefaultAsync(s => s.Id == sandikId, cancellationToken);
            var finansKaydi = await _context.FinansIsKayitlari
                .Include(k => k.SiparisKalemleri).ThenInclude(k => k.Siparis)
                .FirstOrDefaultAsync(k => k.KaynakModul == BagimsizKaynakModul && k.KaynakKayitId == sandikId, cancellationToken);

            if (kaynak == null)
            {
                if (finansKaydi != null)
                {
                    finansKaydi.KaynakAktif = false;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                return;
            }

            if (finansKaydi == null)
            {
                finansKaydi = new FinansIsKaydi
                {
                    ProjeId = kaynak.ProjeId,
                    KaynakKayitId = kaynak.Id,
                    KaynakModul = BagimsizKaynakModul,
                    AktarimTarihi = DateTime.Now
                };
                _context.FinansIsKayitlari.Add(finansKaydi);
            }

            finansKaydi.KaynakAktif = kaynak.UretimeAlindi;
            if (!AktifSipariseBagli(finansKaydi))
                BagimsizKaynaktanGuncelle(finansKaydi, kaynak);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static bool AktifSipariseBagli(FinansIsKaydi kayit) =>
            kayit.SiparisKalemleri.Any(k => k.Siparis.Durum != FinansSiparisDurumu.IptalEdildi);

        private static void KaynaktanGuncelle(FinansIsKaydi hedef, AmbalajUretimKalemi kaynak, Proje proje)
        {
            hedef.ProjeNo = proje.ProjeNo;
            hedef.Musteri = proje.Musteri ?? string.Empty;
            hedef.SandikNo = kaynak.SandikNo;
            hedef.SandikAdi = kaynak.Ad ?? string.Empty;
            hedef.SandikTipi = kaynak.SandikTipi;
            hedef.Boy = kaynak.Boy;
            hedef.En = kaynak.En;
            hedef.Yukseklik = kaynak.Yukseklik;
            hedef.IcSandikSablonId = kaynak.Tur == 3 ? kaynak.IcSandikSablonId : null;
            hedef.IsTuru = kaynak.Tur switch
            {
                2 => FinansIsTuru.IlaveSandik,
                3 => FinansIsTuru.IcSandik,
                _ => FinansIsTuru.NormalSandik
            };
            hedef.Adet = kaynak.Adet;
            hedef.BirimM3 = BirimHacimHesapla(kaynak);
            hedef.UretimeAlinmaTarihi = kaynak.UpdatedDate ?? kaynak.CreatedDate;
            hedef.UretimDurumu = "Üretime Alındı";
        }

        private static decimal BirimHacimHesapla(AmbalajUretimKalemi kaynak)
        {
            if (kaynak.SandikTipi == "Kontrplak Sandık" || kaynak.Boy <= 0 || kaynak.En <= 0 || kaynak.Yukseklik <= 0)
                return 0;

            var boy = kaynak.KaynakSandikId.HasValue ? kaynak.Boy - 92m : kaynak.Boy;
            var en = kaynak.KaynakSandikId.HasValue ? kaynak.En - 92m : kaynak.En;
            var yukseklik = kaynak.KaynakSandikId.HasValue ? kaynak.Yukseklik - 255m : kaynak.Yukseklik;
            return boy > 0 && en > 0 && yukseklik > 0
                ? AmbalajHesaplayici.Hesapla(boy, en, yukseklik).ToplamHacimM3
                : 0;
        }

        private static int SandikAdediHesapla(string sandikNo)
        {
            var match = System.Text.RegularExpressions.Regex.Match(sandikNo ?? string.Empty, @"^(\d+)\s*-\s*(\d+)$");
            if (!match.Success)
                return 1;

            var baslangic = int.Parse(match.Groups[1].Value);
            var bitis = int.Parse(match.Groups[2].Value);
            return bitis >= baslangic ? bitis - baslangic + 1 : 1;
        }

        private static void BagimsizKaynaktanGuncelle(FinansIsKaydi hedef, AmbalajBagimsizSandik kaynak)
        {
            hedef.ProjeId = kaynak.ProjeId;
            hedef.ProjeNo = kaynak.Proje?.ProjeNo ?? string.Empty;
            hedef.Musteri = kaynak.Proje?.Musteri ?? string.Empty;
            hedef.SandikNo = kaynak.SandikNo;
            hedef.SandikAdi = kaynak.Ad;
            hedef.SandikTipi = kaynak.SandikTipi;
            hedef.Boy = kaynak.Boy;
            hedef.En = kaynak.En;
            hedef.Yukseklik = kaynak.Yukseklik;
            hedef.IcSandikSablonId = kaynak.Tur == 3 ? kaynak.IcSandikSablonId : null;
            hedef.IsTuru = kaynak.Tur switch
            {
                2 => FinansIsTuru.IlaveSandik,
                3 => FinansIsTuru.IcSandik,
                4 => FinansIsTuru.SahaSandigi,
                5 => FinansIsTuru.YedekSandik,
                _ => FinansIsTuru.DigerAmbalajIsi
            };
            hedef.Adet = kaynak.Adet;
            hedef.BirimM3 = AmbalajHesaplayici.Hesapla(kaynak.Boy, kaynak.En, kaynak.Yukseklik).ToplamHacimM3;
            hedef.UretimeAlinmaTarihi = kaynak.UpdatedDate ?? kaynak.CreatedDate;
            hedef.UretimDurumu = kaynak.UretimeAlindi ? "Üretime Alındı" : "Üretim Dışı";
        }
    }
}