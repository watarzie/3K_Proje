using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace _3K.Infrastructure.Services
{
    public class ArsivService : IArsivService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ArsivService> _logger;

        public ArsivService(AppDbContext context, ILogger<ArsivService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> ProjeleriArsivleAsync()
        {
            // Tamamen sevk edilmiş projelerin loglarını o gece arşive taşı.
            // Kısmi sevkte sevk edilmemiş sandıklarda 3K işi devam eder.
            var arsivlenecekProjeler = await _context.Projeler
                .Where(p => p.DurumId == (int)ProjeDurum.SevkEdildi)
                .Select(p => new { p.Id, p.ProjeNo, p.Musteri })
                .ToListAsync();

            if (!arsivlenecekProjeler.Any())
                return 0;

            var projeIdler = arsivlenecekProjeler.Select(p => p.Id).ToList();

            // Bu projelerin hareketlerini çek (arşivde olmayanlar)
            var hareketler = await _context.HareketGecmisleri
                .Include(h => h.Kullanici)
                .Include(h => h.IslemTipiLookup)
                .Where(h => projeIdler.Contains(h.ProjeId))
                .ToListAsync();

            if (!hareketler.Any())
                return 0;

            var projeDict = arsivlenecekProjeler.ToDictionary(p => p.Id);

            // Arşiv kayıtları oluştur (denormalized)
            var arsivKayitlari = hareketler.Select(h =>
            {
                var proje = projeDict.GetValueOrDefault(h.ProjeId);
                return new HareketGecmisiArsiv
                {
                    ProjeId = h.ProjeId,
                    ProjeNo = proje?.ProjeNo ?? "",
                    ReferansTipi = h.ReferansTipi,
                    ReferansId = h.ReferansId,
                    ReferansMetni = h.ReferansMetni,
                    Islem = h.Islem,
                    IslemTipiId = h.IslemTipiId,
                    IslemTipiMetni = h.IslemTipiLookup?.Deger ?? h.Islem,
                    KullaniciId = h.KullaniciId,
                    KullaniciAdi = h.Kullanici?.AdSoyad ?? "",
                    Tarih = h.Tarih,
                    EskiDeger = h.EskiDeger,
                    YeniDeger = h.YeniDeger,
                    Aciklama = h.Aciklama,
                    CreatedDate = h.CreatedDate
                };
            }).ToList();

            // Transaction ile taşı
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.HareketGecmisleriArsiv.AddRangeAsync(arsivKayitlari);
                _context.HareketGecmisleri.RemoveRange(hareketler);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Arşivleme tamamlandı: {ProjeCount} proje, {KayitCount} kayıt taşındı",
                    arsivlenecekProjeler.Count, arsivKayitlari.Count);

                return arsivKayitlari.Count;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}
