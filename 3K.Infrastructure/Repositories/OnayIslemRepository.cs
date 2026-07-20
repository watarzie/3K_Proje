using System.Globalization;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Repositories
{
    public sealed class OnayIslemRepository : IOnayIslemRepository
    {
        private readonly AppDbContext _context;

        public OnayIslemRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<OnayBekleyenIslem?> GetByIdNoTrackingAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return _context.OnayBekleyenIslemler
                .AsNoTracking()
                .FirstOrDefaultAsync(islem => islem.Id == id, cancellationToken);
        }

        public async Task<bool> OnayKarariniAlVeCalistirmayiBaslatAsync(
            int id,
            int kararVerenKullaniciId,
            DateTime kararTarihi,
            CancellationToken cancellationToken = default)
        {
            var updatedBy = kararVerenKullaniciId.ToString(CultureInfo.InvariantCulture);
            var etkilenen = await _context.OnayBekleyenIslemler
                .Where(islem => islem.Id == id && islem.Durum == OnayDurumu.Bekliyor)
                .ExecuteUpdateAsync(
                    guncelleme => guncelleme
                        .SetProperty(islem => islem.Durum, OnayDurumu.Onaylandi)
                        .SetProperty(islem => islem.OnaylayanKullaniciId, kararVerenKullaniciId)
                        .SetProperty(islem => islem.KararTarihi, kararTarihi)
                        .SetProperty(islem => islem.KararAciklamasi, (string?)null)
                        .SetProperty(islem => islem.RedAciklamasi, (string?)null)
                        .SetProperty(islem => islem.CalistirmaDurumu, OnayCalistirmaDurumu.Calisiyor)
                        .SetProperty(islem => islem.CalistirmaBaslamaTarihi, kararTarihi)
                        .SetProperty(islem => islem.CalistirmaBitisTarihi, (DateTime?)null)
                        .SetProperty(islem => islem.CalistirmaHatasi, (string?)null)
                        .SetProperty(islem => islem.UpdatedDate, kararTarihi)
                        .SetProperty(islem => islem.UpdatedBy, updatedBy),
                    cancellationToken);

            return etkilenen == 1;
        }

        public async Task<bool> ReddetAsync(
            int id,
            int kararVerenKullaniciId,
            DateTime kararTarihi,
            string kararAciklamasi,
            CancellationToken cancellationToken = default)
        {
            var updatedBy = kararVerenKullaniciId.ToString(CultureInfo.InvariantCulture);
            var etkilenen = await _context.OnayBekleyenIslemler
                .Where(islem => islem.Id == id && islem.Durum == OnayDurumu.Bekliyor)
                .ExecuteUpdateAsync(
                    guncelleme => guncelleme
                        .SetProperty(islem => islem.Durum, OnayDurumu.Reddedildi)
                        .SetProperty(islem => islem.OnaylayanKullaniciId, kararVerenKullaniciId)
                        .SetProperty(islem => islem.KararTarihi, kararTarihi)
                        .SetProperty(islem => islem.KararAciklamasi, kararAciklamasi)
                        .SetProperty(islem => islem.RedAciklamasi, kararAciklamasi)
                        .SetProperty(islem => islem.CalistirmaDurumu, OnayCalistirmaDurumu.Atlandi)
                        .SetProperty(islem => islem.CalistirmaBaslamaTarihi, (DateTime?)null)
                        .SetProperty(islem => islem.CalistirmaBitisTarihi, (DateTime?)null)
                        .SetProperty(islem => islem.CalistirmaHatasi, (string?)null)
                        .SetProperty(islem => islem.UpdatedDate, kararTarihi)
                        .SetProperty(islem => islem.UpdatedBy, updatedBy),
                    cancellationToken);

            return etkilenen == 1;
        }

        public async Task<bool> CalistirmayiTamamlaAsync(
            int id,
            int kararVerenKullaniciId,
            OnayCalistirmaDurumu durum,
            DateTime bitisTarihi,
            string? kullaniciyaGuvenliHata,
            CancellationToken cancellationToken = default)
        {
            if (durum is not (OnayCalistirmaDurumu.Basarili or OnayCalistirmaDurumu.Basarisiz))
                throw new ArgumentOutOfRangeException(nameof(durum), "Çalıştırma yalnız başarılı veya başarısız olarak tamamlanabilir.");

            var updatedBy = kararVerenKullaniciId.ToString(CultureInfo.InvariantCulture);
            var etkilenen = await _context.OnayBekleyenIslemler
                .Where(islem =>
                    islem.Id == id &&
                    islem.Durum == OnayDurumu.Onaylandi &&
                    islem.CalistirmaDurumu == OnayCalistirmaDurumu.Calisiyor &&
                    islem.OnaylayanKullaniciId == kararVerenKullaniciId)
                .ExecuteUpdateAsync(
                    guncelleme => guncelleme
                        .SetProperty(islem => islem.CalistirmaDurumu, durum)
                        .SetProperty(islem => islem.CalistirmaBitisTarihi, bitisTarihi)
                        .SetProperty(islem => islem.CalistirmaHatasi, kullaniciyaGuvenliHata)
                        .SetProperty(islem => islem.UpdatedDate, bitisTarihi)
                        .SetProperty(islem => islem.UpdatedBy, updatedBy),
                    cancellationToken);

            return etkilenen == 1;
        }

        public async Task<OnayGecmisiSayfaliSonuc> GetGecmisAsync(
            int kullaniciId,
            bool bekleyenleriGorebilir,
            OnayErisimKapsami erisimKapsami,
            OnayGecmisiFiltresi filtre,
            CancellationToken cancellationToken = default)
        {
            var query = KapsamaGoreFiltrele(
                _context.OnayBekleyenIslemler.AsNoTracking(),
                kullaniciId,
                bekleyenleriGorebilir,
                erisimKapsami,
                filtre.Kapsam);

            if (filtre.Durum.HasValue)
                query = query.Where(islem => islem.Durum == filtre.Durum.Value);

            if (filtre.CalistirmaDurumu.HasValue)
            {
                query = query.Where(islem =>
                    islem.CalistirmaDurumu == filtre.CalistirmaDurumu.Value);
            }

            if (filtre.BaslangicTarihi.HasValue)
            {
                query = query.Where(islem =>
                    (islem.KararTarihi ?? islem.CreatedDate) >= filtre.BaslangicTarihi.Value);
            }

            if (filtre.BitisTarihiHaric.HasValue)
            {
                query = query.Where(islem =>
                    (islem.KararTarihi ?? islem.CreatedDate) < filtre.BitisTarihiHaric.Value);
            }

            if (!string.IsNullOrWhiteSpace(filtre.Arama))
            {
                var aramaDeseni = $"%{IlikeDeseniniKacir(filtre.Arama.Trim())}%";
                query = query.Where(islem =>
                    EF.Functions.ILike(islem.IslemKodu, aramaDeseni, "\\") ||
                    EF.Functions.ILike(islem.IslemAciklamasi, aramaDeseni, "\\") ||
                    EF.Functions.ILike(islem.TalepEdenKullanici.AdSoyad, aramaDeseni, "\\") ||
                    (islem.OnaylayanKullanici != null &&
                     EF.Functions.ILike(islem.OnaylayanKullanici.AdSoyad, aramaDeseni, "\\")) ||
                    (islem.Proje != null &&
                     EF.Functions.ILike(islem.Proje.ProjeNo, aramaDeseni, "\\")));
            }

            var toplamKayit = await query.CountAsync(cancellationToken);
            var atlanacakKayit = (filtre.Sayfa - 1) * filtre.SayfaBoyutu;
            var kayitlar = await Projelendir(
                    query,
                    kullaniciId,
                    bekleyenleriGorebilir,
                    erisimKapsami)
                .OrderByDescending(kayit => kayit.KararTarihi ?? kayit.TalepTarihi)
                .ThenByDescending(kayit => kayit.Id)
                .Skip(atlanacakKayit)
                .Take(filtre.SayfaBoyutu)
                .ToListAsync(cancellationToken);

            return new OnayGecmisiSayfaliSonuc
            {
                Kayitlar = kayitlar,
                ToplamKayit = toplamKayit
            };
        }

        public Task<OnayGecmisiKaydi?> GetGecmisDetayiAsync(
            int id,
            int kullaniciId,
            bool bekleyenleriGorebilir,
            OnayErisimKapsami erisimKapsami,
            CancellationToken cancellationToken = default)
        {
            var query = KapsamaGoreFiltrele(
                    _context.OnayBekleyenIslemler.AsNoTracking(),
                    kullaniciId,
                    bekleyenleriGorebilir,
                    erisimKapsami,
                    OnayGecmisiKapsami.Tumu)
                .Where(islem => islem.Id == id);

            return Projelendir(
                    query,
                    kullaniciId,
                    bekleyenleriGorebilir,
                    erisimKapsami)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<OnayBekleyenSorguKaydi>> GetYetkiliBekleyenlerAsync(
            int kullaniciId,
            OnayErisimKapsami erisimKapsami,
            CancellationToken cancellationToken = default)
        {
            return await YetkiliBekleyenleriFiltrele(
                    _context.OnayBekleyenIslemler.AsNoTracking(),
                    kullaniciId,
                    erisimKapsami)
                .OrderBy(islem => islem.CreatedDate)
                .Select(islem => new OnayBekleyenSorguKaydi
                {
                    Id = islem.Id,
                    IslemKodu = islem.IslemKodu,
                    IslemAciklamasi = islem.IslemAciklamasi,
                    TalepEdenKisi = islem.TalepEdenKullanici.AdSoyad,
                    OlusturulmaTarihi = islem.CreatedDate,
                    Durum = islem.Durum
                })
                .ToListAsync(cancellationToken);
        }

        public Task<int> GetYetkiliBekleyenSayisiAsync(
            int kullaniciId,
            OnayErisimKapsami erisimKapsami,
            CancellationToken cancellationToken = default)
        {
            return YetkiliBekleyenleriFiltrele(
                    _context.OnayBekleyenIslemler.AsNoTracking(),
                    kullaniciId,
                    erisimKapsami)
                .CountAsync(cancellationToken);
        }

        public Task<CekiRevizyonOnizlemeKaydi?> GetRevizyonOnizlemeKaydiAsync(
            int talepId,
            int talepEdenKullaniciId,
            int? projeId,
            CancellationToken cancellationToken = default)
        {
            return _context.CekiRevizyonTalepleri
                .AsNoTracking()
                .Where(talep =>
                    talep.Id == talepId &&
                    talep.TalepEdenKullaniciId == talepEdenKullaniciId &&
                    (!projeId.HasValue || talep.ProjeId == projeId.Value))
                .Select(talep => new CekiRevizyonOnizlemeKaydi
                {
                    OnizlemeJson = talep.OnizlemeJson,
                    OnizlemeHash = talep.OnizlemeHash,
                    OnizlemeSurumu = talep.OnizlemeSurumu
                })
                .SingleOrDefaultAsync(cancellationToken);
        }

        private static IQueryable<OnayBekleyenIslem> KapsamaGoreFiltrele(
            IQueryable<OnayBekleyenIslem> query,
            int kullaniciId,
            bool bekleyenleriGorebilir,
            OnayErisimKapsami erisimKapsami,
            OnayGecmisiKapsami kapsam)
        {
            if (kapsam == OnayGecmisiKapsami.KararVerdiklerim)
                return query.Where(islem => islem.OnaylayanKullaniciId == kullaniciId);

            if (kapsam == OnayGecmisiKapsami.Taleplerim)
                return query.Where(islem => islem.TalepEdenKullaniciId == kullaniciId);

            if (kapsam == OnayGecmisiKapsami.Bekleyenler)
            {
                return bekleyenleriGorebilir
                    ? YetkiliBekleyenleriFiltrele(query, kullaniciId, erisimKapsami)
                    : query.Where(_ => false);
            }

            var kisiselQuery = query.Where(islem =>
                islem.TalepEdenKullaniciId == kullaniciId ||
                islem.OnaylayanKullaniciId == kullaniciId);

            if (!bekleyenleriGorebilir)
                return kisiselQuery;

            var islemKodlari = erisimKapsami.IslemKodlari.ToArray();
            return query.Where(islem =>
                islem.TalepEdenKullaniciId == kullaniciId ||
                islem.OnaylayanKullaniciId == kullaniciId ||
                (islem.Durum == OnayDurumu.Bekliyor &&
                 (erisimKapsami.TumIslemler || islemKodlari.Contains(islem.IslemKodu)) &&
                 (erisimKapsami.KendiTalepleriniOnaylayabilir ||
                  islem.TalepEdenKullaniciId != kullaniciId)));
        }

        private static IQueryable<OnayBekleyenIslem> YetkiliBekleyenleriFiltrele(
            IQueryable<OnayBekleyenIslem> query,
            int kullaniciId,
            OnayErisimKapsami erisimKapsami)
        {
            var islemKodlari = erisimKapsami.IslemKodlari.ToArray();
            return query.Where(islem =>
                islem.Durum == OnayDurumu.Bekliyor &&
                (erisimKapsami.TumIslemler || islemKodlari.Contains(islem.IslemKodu)) &&
                (erisimKapsami.KendiTalepleriniOnaylayabilir ||
                 islem.TalepEdenKullaniciId != kullaniciId));
        }

        private static IQueryable<OnayGecmisiKaydi> Projelendir(
            IQueryable<OnayBekleyenIslem> query,
            int kullaniciId,
            bool bekleyenleriGorebilir,
            OnayErisimKapsami erisimKapsami)
        {
            var islemKodlari = erisimKapsami.IslemKodlari.ToArray();
            return query.Select(islem => new OnayGecmisiKaydi
            {
                Id = islem.Id,
                IslemKodu = islem.IslemKodu,
                IslemAciklamasi = islem.IslemAciklamasi,
                TalepEdenKullaniciId = islem.TalepEdenKullaniciId,
                TalepEdenKisi = islem.TalepEdenKullanici.AdSoyad,
                KararVerenKullaniciId = islem.OnaylayanKullaniciId,
                KararVerenKisi = islem.OnaylayanKullanici != null
                    ? islem.OnaylayanKullanici.AdSoyad
                    : null,
                Durum = islem.Durum,
                TalepTarihi = islem.CreatedDate,
                KararTarihi = islem.KararTarihi,
                KararAciklamasi = islem.KararAciklamasi ?? islem.RedAciklamasi,
                CalistirmaDurumu = islem.CalistirmaDurumu,
                CalistirmaBaslamaTarihi = islem.CalistirmaBaslamaTarihi,
                CalistirmaBitisTarihi = islem.CalistirmaBitisTarihi,
                CalistirmaHatasi = islem.CalistirmaHatasi,
                ReferansTipi = islem.ReferansTipi,
                ReferansId = islem.ReferansId,
                ProjeId = islem.ProjeId,
                ProjeNo = islem.Proje != null ? islem.Proje.ProjeNo : null,
                HedefUrl = islem.HedefUrl != null &&
                           islem.HedefUrl.StartsWith("/") &&
                           !islem.HedefUrl.StartsWith("//")
                    ? islem.HedefUrl
                    : null,
                AksiyonAktifMi = bekleyenleriGorebilir &&
                    islem.Durum == OnayDurumu.Bekliyor &&
                    (erisimKapsami.TumIslemler || islemKodlari.Contains(islem.IslemKodu)) &&
                    (erisimKapsami.KendiTalepleriniOnaylayabilir ||
                     islem.TalepEdenKullaniciId != kullaniciId)
            });
        }

        private static string IlikeDeseniniKacir(string deger)
        {
            return deger
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
        }
    }
}
