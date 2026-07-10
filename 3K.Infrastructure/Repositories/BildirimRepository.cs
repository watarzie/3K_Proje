using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Repositories
{
    public class BildirimRepository : IBildirimRepository
    {
        private readonly AppDbContext _context;

        public BildirimRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyList<KullaniciBildirimi> Bildirimler, int Toplam)> GetOkunmamisAsync(
            int kullaniciId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            var query = _context.KullaniciBildirimleri
                .AsNoTracking()
                .Include(kb => kb.Bildirim)
                .Where(kb => kb.KullaniciId == kullaniciId && !kb.OkunduMu);

            var toplam = await query.CountAsync(cancellationToken);
            var bildirimler = await query
                .OrderByDescending(kb => kb.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return (bildirimler, toplam);
        }

        public Task<KullaniciBildirimi?> GetKullaniciBildirimiAsync(
            int bildirimId,
            int kullaniciId,
            CancellationToken cancellationToken = default)
        {
            return _context.KullaniciBildirimleri
                .FirstOrDefaultAsync(
                    kb => kb.BildirimId == bildirimId && kb.KullaniciId == kullaniciId,
                    cancellationToken);
        }

        public Task<int> TumOkunmamisBildirimleriOkunduIsaretleAsync(
            int kullaniciId,
            DateTime okunmaTarihi,
            CancellationToken cancellationToken = default)
        {
            return _context.KullaniciBildirimleri
                .Where(kb => kb.KullaniciId == kullaniciId && !kb.OkunduMu)
                .ExecuteUpdateAsync(
                    guncelleme => guncelleme
                        .SetProperty(kb => kb.OkunduMu, true)
                        .SetProperty(kb => kb.OkunmaTarihi, okunmaTarihi)
                        .SetProperty(kb => kb.UpdatedDate, okunmaTarihi)
                        .SetProperty(kb => kb.UpdatedBy, kullaniciId.ToString()),
                    cancellationToken);
        }

        public async Task<BildirimSayfaliSorguSonucu> GetSayfaliAsync(
            int kullaniciId,
            BildirimListeFiltresi filtre,
            CancellationToken cancellationToken = default)
        {
            var kullaniciBildirimleri = FiltreliKullaniciBildirimleri(kullaniciId, filtre);

            var toplamKayit = await kullaniciBildirimleri.CountAsync(cancellationToken);
            var toplamOkunmamis = await _context.KullaniciBildirimleri
                .AsNoTracking()
                .CountAsync(
                    kullaniciBildirimi =>
                        kullaniciBildirimi.KullaniciId == kullaniciId &&
                        !kullaniciBildirimi.OkunduMu,
                    cancellationToken);

            var atlanacakKayit = (filtre.Sayfa - 1) * filtre.SayfaBoyutu;
            var bildirimler = await kullaniciBildirimleri
                .OrderByDescending(kullaniciBildirimi => kullaniciBildirimi.Bildirim.CreatedDate)
                .ThenByDescending(kullaniciBildirimi => kullaniciBildirimi.Id)
                .Skip(atlanacakKayit)
                .Take(filtre.SayfaBoyutu)
                .Select(kullaniciBildirimi => new BildirimSorguKaydi
                {
                    Id = kullaniciBildirimi.BildirimId,
                    TipId = kullaniciBildirimi.Bildirim.TipId,
                    Baslik = kullaniciBildirimi.Bildirim.Baslik,
                    Mesaj = kullaniciBildirimi.Bildirim.Mesaj,
                    OlusturulmaTarihi = kullaniciBildirimi.Bildirim.CreatedDate,
                    OkunduMu = kullaniciBildirimi.OkunduMu,
                    OkunmaTarihi = kullaniciBildirimi.OkunmaTarihi,
                    HedefUrl = kullaniciBildirimi.Bildirim.HedefUrl,
                    ReferansTipi = kullaniciBildirimi.Bildirim.ReferansTipi,
                    ReferansId = kullaniciBildirimi.Bildirim.ReferansId,
                    OlusturanKullaniciId = kullaniciBildirimi.Bildirim.OlusturanKullaniciId,
                    OlusturanKullaniciAdi = kullaniciBildirimi.Bildirim.OlusturanKullanici != null
                        ? kullaniciBildirimi.Bildirim.OlusturanKullanici.AdSoyad
                        : null
                })
                .ToListAsync(cancellationToken);

            await ProjeBilgileriniDoldurAsync(bildirimler, cancellationToken);

            return new BildirimSayfaliSorguSonucu
            {
                Bildirimler = bildirimler,
                ToplamKayit = toplamKayit,
                ToplamOkunmamis = toplamOkunmamis
            };
        }

        public async Task<BildirimSorguKaydi?> GetDetayAsync(
            int bildirimId,
            int kullaniciId,
            CancellationToken cancellationToken = default)
        {
            var bildirim = await _context.KullaniciBildirimleri
                .AsNoTracking()
                .Where(kullaniciBildirimi =>
                    kullaniciBildirimi.BildirimId == bildirimId &&
                    kullaniciBildirimi.KullaniciId == kullaniciId)
                .Select(kullaniciBildirimi => new BildirimSorguKaydi
                {
                    Id = kullaniciBildirimi.BildirimId,
                    TipId = kullaniciBildirimi.Bildirim.TipId,
                    Baslik = kullaniciBildirimi.Bildirim.Baslik,
                    Mesaj = kullaniciBildirimi.Bildirim.Mesaj,
                    OlusturulmaTarihi = kullaniciBildirimi.Bildirim.CreatedDate,
                    OkunduMu = kullaniciBildirimi.OkunduMu,
                    OkunmaTarihi = kullaniciBildirimi.OkunmaTarihi,
                    HedefUrl = kullaniciBildirimi.Bildirim.HedefUrl,
                    ReferansTipi = kullaniciBildirimi.Bildirim.ReferansTipi,
                    ReferansId = kullaniciBildirimi.Bildirim.ReferansId,
                    OlusturanKullaniciId = kullaniciBildirimi.Bildirim.OlusturanKullaniciId,
                    OlusturanKullaniciAdi = kullaniciBildirimi.Bildirim.OlusturanKullanici != null
                        ? kullaniciBildirimi.Bildirim.OlusturanKullanici.AdSoyad
                        : null
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (bildirim == null)
                return null;

            await ProjeBilgileriniDoldurAsync([bildirim], cancellationToken);
            return bildirim;
        }

        private IQueryable<KullaniciBildirimi> FiltreliKullaniciBildirimleri(
            int kullaniciId,
            BildirimListeFiltresi filtre)
        {
            var query = _context.KullaniciBildirimleri
                .AsNoTracking()
                .Where(kullaniciBildirimi => kullaniciBildirimi.KullaniciId == kullaniciId);

            if (filtre.OkunduMu.HasValue)
            {
                query = query.Where(kullaniciBildirimi =>
                    kullaniciBildirimi.OkunduMu == filtre.OkunduMu.Value);
            }

            if (filtre.BaslangicTarihi.HasValue)
            {
                query = query.Where(kullaniciBildirimi =>
                    kullaniciBildirimi.Bildirim.CreatedDate >= filtre.BaslangicTarihi.Value);
            }

            if (filtre.BitisTarihiHaric.HasValue)
            {
                query = query.Where(kullaniciBildirimi =>
                    kullaniciBildirimi.Bildirim.CreatedDate < filtre.BitisTarihiHaric.Value);
            }

            if (filtre.TipId.HasValue)
            {
                query = query.Where(kullaniciBildirimi =>
                    kullaniciBildirimi.Bildirim.TipId == filtre.TipId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var aramaDeseni = $"%{IlikeDeseniniKacir(filtre.Arama.Trim())}%";
                query = query.Where(kullaniciBildirimi =>
                    EF.Functions.ILike(kullaniciBildirimi.Bildirim.Baslik, aramaDeseni, "\\") ||
                    EF.Functions.ILike(kullaniciBildirimi.Bildirim.Mesaj, aramaDeseni, "\\") ||
                    (kullaniciBildirimi.Bildirim.ReferansTipi != null &&
                     EF.Functions.ILike(kullaniciBildirimi.Bildirim.ReferansTipi, aramaDeseni, "\\")) ||
                    (kullaniciBildirimi.Bildirim.ReferansTipi == BildirimReferansTipleri.Ceki &&
                     _context.Cekiler.Any(ceki =>
                         ceki.Id == kullaniciBildirimi.Bildirim.ReferansId &&
                         (EF.Functions.ILike(ceki.Proje.ProjeNo, aramaDeseni, "\\") ||
                          EF.Functions.ILike(ceki.Proje.Musteri, aramaDeseni, "\\")))));
            }

            return query;
        }

        private static string IlikeDeseniniKacir(string deger)
        {
            return deger
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
        }

        private async Task ProjeBilgileriniDoldurAsync(
            IReadOnlyCollection<BildirimSorguKaydi> bildirimler,
            CancellationToken cancellationToken)
        {
            var cekiIdleri = bildirimler
                .Where(bildirim =>
                    bildirim.ReferansTipi == BildirimReferansTipleri.Ceki &&
                    bildirim.ReferansId.HasValue)
                .Select(bildirim => bildirim.ReferansId!.Value)
                .Distinct()
                .ToList();

            if (cekiIdleri.Count == 0)
                return;

            var projeBilgileri = await _context.Cekiler
                .AsNoTracking()
                .Where(ceki => cekiIdleri.Contains(ceki.Id))
                .Select(ceki => new
                {
                    CekiId = ceki.Id,
                    ceki.ProjeId,
                    ceki.Proje.ProjeNo
                })
                .ToDictionaryAsync(bilgi => bilgi.CekiId, cancellationToken);

            foreach (var bildirim in bildirimler)
            {
                if (!bildirim.ReferansId.HasValue ||
                    !projeBilgileri.TryGetValue(bildirim.ReferansId.Value, out var projeBilgisi))
                {
                    continue;
                }

                bildirim.ProjeId = projeBilgisi.ProjeId;
                bildirim.ProjeNo = projeBilgisi.ProjeNo;
            }
        }
    }
}
