using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public class FinansBelgeService : IFinansBelgeService
    {
        public const long AzamiBoyut = 20 * 1024 * 1024;
        private static readonly IReadOnlyDictionary<string, string[]> IzinliTurler = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".xls"] = ["application/vnd.ms-excel", "application/octet-stream"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/octet-stream"],
            [".doc"] = ["application/msword", "application/octet-stream"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/octet-stream"],
            [".jpg"] = ["image/jpeg"], [".jpeg"] = ["image/jpeg"], [".png"] = ["image/png"]
        };

        private readonly AppDbContext _context;
        private readonly string _kokDizin;

        public FinansBelgeService(AppDbContext context)
        {
            _context = context;
            _kokDizin = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Uploads", "Finans"));
        }

        public async Task<FinansBelgeDto> YukleAsync(FinansBelgeTuru tur, int kayitId, string dosyaAdi, string icerikTuru,
            long boyut, Stream icerik, string kullanici, CancellationToken cancellationToken = default)
        {
            var temizAd = Path.GetFileName(dosyaAdi);
            var uzanti = Path.GetExtension(temizAd).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(temizAd) || !IzinliTurler.TryGetValue(uzanti, out var mimeTurleri) || !mimeTurleri.Contains(icerikTuru, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Yalnız PDF, Excel, Word, JPG, JPEG ve PNG dosyaları yüklenebilir.");
            if (boyut <= 0 || boyut > AzamiBoyut)
                throw new InvalidOperationException("Dosya boyutu 20 MB sınırını aşamaz.");
            if (!await BagliKayitVarAsync(tur, kayitId, cancellationToken))
                throw new KeyNotFoundException("Belgenin bağlanacağı finans kaydı bulunamadı.");

            var saklananAd = $"{Guid.NewGuid():N}{uzanti}";
            var turDizini = Path.Combine(_kokDizin, tur.ToString());
            Directory.CreateDirectory(turDizini);
            var tamYol = Path.GetFullPath(Path.Combine(turDizini, saklananAd));
            if (!tamYol.StartsWith(_kokDizin, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Geçersiz dosya yolu.");

            await using (var hedef = new FileStream(tamYol, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                await icerik.CopyToAsync(hedef, cancellationToken);

            var belge = new FinansBelge
            {
                BelgeTuru = tur, DosyaAdi = temizAd, SaklananDosyaAdi = saklananAd, DosyaUzantisi = uzanti,
                DosyaYolu = Path.GetRelativePath(_kokDizin, tamYol), IcerikTuru = icerikTuru, Boyut = boyut,
                YukleyenKullanici = kullanici
            };
            Bagla(belge, tur, kayitId);
            _context.FinansBelgeleri.Add(belge);
            try { await _context.SaveChangesAsync(cancellationToken); }
            catch { File.Delete(tamYol); throw; }
            return Dto(belge);
        }

        public async Task<IReadOnlyList<FinansBelgeDto>> ListeleAsync(FinansBelgeTuru tur, int kayitId, CancellationToken cancellationToken = default) =>
            (await BagliBelgeler(tur, kayitId).AsNoTracking().OrderByDescending(b => b.CreatedDate).ToListAsync(cancellationToken)).Select(Dto).ToList();

        public async Task<FinansDosyaIcerigi?> AcAsync(int belgeId, CancellationToken cancellationToken = default)
        {
            var belge = await _context.FinansBelgeleri.AsNoTracking().FirstOrDefaultAsync(b => b.Id == belgeId, cancellationToken);
            if (belge == null) return null;
            var yol = GuvenliTamYol(belge.DosyaYolu);
            if (!File.Exists(yol)) return null;
            return new FinansDosyaIcerigi(new FileStream(yol, FileMode.Open, FileAccess.Read, FileShare.Read), belge.IcerikTuru, belge.DosyaAdi);
        }

        public async Task<bool> SilAsync(int belgeId, CancellationToken cancellationToken = default)
        {
            var belge = await _context.FinansBelgeleri.FirstOrDefaultAsync(b => b.Id == belgeId, cancellationToken);
            if (belge == null) return false;
            var yol = GuvenliTamYol(belge.DosyaYolu);
            if (!File.Exists(yol)) return false;
            try { File.Delete(yol); }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            _context.FinansBelgeleri.Remove(belge);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<bool> BagliKayitVarAsync(FinansBelgeTuru tur, int id, CancellationToken ct) => tur switch
        {
            FinansBelgeTuru.Siparis => await _context.FinansSiparisleri.AnyAsync(x => x.Id == id, ct),
            FinansBelgeTuru.Fatura => await _context.FinansFaturalari.AnyAsync(x => x.Id == id, ct),
            FinansBelgeTuru.OzelIs => await _context.FinansOzelIsleri.AnyAsync(x => x.Id == id, ct),
            FinansBelgeTuru.Gider => await _context.FinansGiderleri.AnyAsync(x => x.Id == id, ct),
            _ => false
        };

        private IQueryable<FinansBelge> BagliBelgeler(FinansBelgeTuru tur, int id) => tur switch
        {
            FinansBelgeTuru.Siparis => _context.FinansBelgeleri.Where(x => x.SiparisId == id),
            FinansBelgeTuru.Fatura => _context.FinansBelgeleri.Where(x => x.FaturaId == id),
            FinansBelgeTuru.OzelIs => _context.FinansBelgeleri.Where(x => x.OzelIsId == id),
            FinansBelgeTuru.Gider => _context.FinansBelgeleri.Where(x => x.GiderId == id),
            _ => _context.FinansBelgeleri.Where(x => false)
        };

        private static void Bagla(FinansBelge belge, FinansBelgeTuru tur, int id)
        {
            if (tur == FinansBelgeTuru.Siparis) belge.SiparisId = id;
            else if (tur == FinansBelgeTuru.Fatura) belge.FaturaId = id;
            else if (tur == FinansBelgeTuru.OzelIs) belge.OzelIsId = id;
            else if (tur == FinansBelgeTuru.Gider) belge.GiderId = id;
        }

        private string GuvenliTamYol(string goreliYol)
        {
            var yol = Path.GetFullPath(Path.Combine(_kokDizin, goreliYol));
            if (!yol.StartsWith(_kokDizin + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Geçersiz dosya yolu.");
            return yol;
        }

        private static FinansBelgeDto Dto(FinansBelge b) => new(b.Id, b.BelgeTuru,
            b.SiparisId ?? b.FaturaId ?? b.OzelIsId ?? b.GiderId ?? 0, b.DosyaAdi, b.DosyaUzantisi,
            b.IcerikTuru, b.Boyut, b.YukleyenKullanici, b.CreatedDate);
    }
}