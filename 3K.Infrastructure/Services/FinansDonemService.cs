using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public class FinansDonemService : IFinansDonemService
    {
        private readonly AppDbContext _context;
        public FinansDonemService(AppDbContext context) => _context = context;

        public async Task<FinansDonemOlusturSonuc> OlusturAsync(DateTime referansTarihi, CancellationToken cancellationToken = default)
        {
            var tarih = new DateTime(referansTarihi.Year, referansTarihi.Month, 1);
            var sonrakiAy = tarih.AddMonths(1);
            var tanimlar = await _context.FinansDuzenliIsleri.Where(d => d.Aktif && d.BaslangicTarihi < sonrakiAy &&
                (!d.BitisTarihi.HasValue || d.BitisTarihi.Value >= tarih)).ToListAsync(cancellationToken);
            var olusturulan = 0;
            foreach (var tanim in tanimlar.Where(t => DonemUygun(t, tarih)))
            {
                var donem = $"{tarih:yyyy-MM}";
                if (await _context.FinansOzelIsleri.AnyAsync(o => o.DuzenliIsId == tanim.Id && o.DonemAnahtari == donem, cancellationToken))
                    continue;
                var ad = $"{AyAdi(tarih.Month)} {tarih.Year} {tanim.IsAdi}";
                var ozelIs = new FinansOzelIs
                {
                    KayitNo = $"OZL-{Guid.NewGuid():N}"[..18].ToUpperInvariant(), ProjeId = tanim.ProjeId,
                    DuzenliIsId = tanim.Id, DonemAnahtari = donem, IsTuru = tanim.IsTuru, Musteri = tanim.Musteri,
                    IsAdi = ad, Aciklama = tanim.Aciklama,
                    Miktar = tanim.HesaplamaYontemi == FinansHesaplamaYontemi.DegiskenAdet ? 0 : 1,
                    Birim = tanim.Birim, HesaplamaYontemi = tanim.HesaplamaYontemi, RaporGrubu = tanim.RaporGrubu,
                    BirimFiyat = tanim.HesaplamaYontemi == FinansHesaplamaYontemi.DegiskenTutar ? 0 : tanim.BirimFiyat,
                    ParaBirimi = tanim.ParaBirimi, KdvOrani = tanim.KdvOrani,
                    IsTarihi = new DateTime(tarih.Year, tarih.Month, Math.Min(tanim.OlusturmaGunu, DateTime.DaysInMonth(tarih.Year, tarih.Month)))
                };
                _context.FinansOzelIsleri.Add(ozelIs);
                await _context.SaveChangesAsync(cancellationToken);
                var proje = tanim.ProjeId.HasValue ? await _context.Projeler.AsNoTracking().FirstOrDefaultAsync(p => p.Id == tanim.ProjeId, cancellationToken) : null;
                _context.FinansIsKayitlari.Add(new FinansIsKaydi
                {
                    OzelIsId = ozelIs.Id, ProjeId = tanim.ProjeId, ProjeNo = proje?.ProjeNo ?? string.Empty,
                    Musteri = tanim.Musteri, KaynakModul = "FinansDuzenliIs", KaynakKayitId = ozelIs.Id,
                    SandikNo = ozelIs.KayitNo, SandikAdi = ozelIs.IsAdi, IsTuru = FinansIsTuru.OzelIs,
                    Adet = ozelIs.Miktar, UretimeAlinmaTarihi = ozelIs.IsTarihi, UretimDurumu = "Düzenli İş", AktarimTarihi = DateTime.Now
                });
                tanim.SonOlusturulanDonem = new DateTime(tarih.Year, tarih.Month, 1);
                await _context.SaveChangesAsync(cancellationToken);
                olusturulan++;
            }
            return new FinansDonemOlusturSonuc(tanimlar.Count, olusturulan, tarih);
        }

        private static bool DonemUygun(FinansDuzenliIs tanim, DateTime tarih)
        {
            var ayFarki = (tarih.Year - tanim.BaslangicTarihi.Year) * 12 + tarih.Month - tanim.BaslangicTarihi.Month;
            return tanim.TekrarSikligi switch
            {
                "Aylık" => ayFarki >= 0,
                "Üç Aylık" => ayFarki >= 0 && ayFarki % 3 == 0,
                "Yıllık" => ayFarki >= 0 && ayFarki % 12 == 0,
                _ => false
            };
        }

        private static string AyAdi(int ay) => new[] { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" }[ay];
    }
}