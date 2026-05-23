using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Helpers;

namespace _3K.Infrastructure.Services
{
    public class HareketService : IHareketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public HareketService(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task HareketKaydetAsync(HareketGecmisi hareket)
        {
            var repo = _unitOfWork.GetRepository<HareketGecmisi>();
            hareket.Tarih = TurkeyTime.Now;

            // Auto-resolve ReferansMetni if not already set by the caller
            if (string.IsNullOrEmpty(hareket.ReferansMetni))
            {
                hareket.ReferansMetni = await ResolveReferansMetniAsync(hareket.ReferansTipi, hareket.ReferansId);
            }

            await repo.AddAsync(hareket);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int projeId)
        {
            return await _context.HareketGecmisleri
                .Include(h => h.Kullanici)
                .Include(h => h.IslemTipiLookup)
                .Where(h => h.ProjeId == projeId)
                .OrderByDescending(h => h.Tarih)
                .ToListAsync();
        }

        public async Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string referansTipi, string referansId)
        {
            return await _context.HareketGecmisleri
                .Include(h => h.Kullanici)
                .Include(h => h.IslemTipiLookup)
                .Where(h => h.ReferansTipi == referansTipi && h.ReferansId == referansId)
                .OrderByDescending(h => h.Tarih)
                .ToListAsync();
        }

        public async Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(
            int projeId, string? searchTerm, int? islemTipiId, int pageNumber, int pageSize)
        {
            var query = _context.HareketGecmisleri
                .Include(h => h.Kullanici)
                .Include(h => h.IslemTipiLookup)
                .Where(h => h.ProjeId == projeId)
                .AsQueryable();

            if (islemTipiId.HasValue)
            {
                query = query.Where(h => h.IslemTipiId == islemTipiId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lower = searchTerm.ToLower();
                query = query.Where(h =>
                    (h.Islem != null && h.Islem.ToLower().Contains(lower)) ||
                    (h.Aciklama != null && h.Aciklama.ToLower().Contains(lower)) ||
                    (h.ReferansId != null && h.ReferansId.ToLower().Contains(lower)) ||
                    (h.Kullanici != null && h.Kullanici.AdSoyad.ToLower().Contains(lower))
                );
            }

            query = query.OrderByDescending(h => h.Tarih);

            var count = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, count);
        }

        private async Task<string?> ResolveReferansMetniAsync(string referansTipi, string? referansId)
        {
            if (string.IsNullOrWhiteSpace(referansId)) return null;

            // Toplu operations have comma-separated IDs
            if (referansTipi.Contains("Toplu", StringComparison.OrdinalIgnoreCase))
                return "Birden fazla kayıt";

            if (referansTipi.Equals("Proje", StringComparison.OrdinalIgnoreCase))
                return "-";

            if (referansTipi.Equals("StokHareketi", StringComparison.OrdinalIgnoreCase))
                return "Stok İşlemi";

            if (!int.TryParse(referansId, out int id))
                return referansId;

            switch (referansTipi)
            {
                case "CekiSatiri":
                    var satir = await _context.CekiSatirlari.FindAsync(id);
                    if (satir != null)
                    {
                        var poz = satir.OlcuResmiPozNo ?? satir.BarkodNo ?? satir.SiraNo.ToString();
                        return $"Poz: {poz} - {satir.Aciklama}";
                    }
                    return null;

                case "Sandik":
                    var sandik = await _context.Sandiklar.FindAsync(id);
                    return sandik != null ? $"No: {sandik.SandikNo}" : null;

                case "SandikIcerik":
                    var icerik = await _context.SandikIcerikleri.FindAsync(id);
                    if (icerik != null)
                    {
                        string sandikNo = "?";
                        string urunMetni = icerik.Isim ?? icerik.BarkodNo ?? "Ürün";
                        var ilgiliSandik = await _context.Sandiklar.FindAsync(icerik.SandikId);
                        if (ilgiliSandik != null) sandikNo = ilgiliSandik.SandikNo;
                        if (icerik.CekiSatiriId.HasValue)
                        {
                            var ilgiliUrun = await _context.CekiSatirlari.FindAsync(icerik.CekiSatiriId.Value);
                            if (ilgiliUrun != null) urunMetni = ilgiliUrun.Aciklama ?? urunMetni;
                        }
                        return $"Sandık {sandikNo} -> {urunMetni}";
                    }
                    return null;

                default:
                    return referansId;
            }
        }
    }
}
