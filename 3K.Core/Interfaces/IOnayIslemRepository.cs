using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    public interface IOnayIslemRepository
    {
        Task<OnayBekleyenIslem?> GetByIdNoTrackingAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<bool> OnayKarariniAlVeCalistirmayiBaslatAsync(
            int id,
            int kararVerenKullaniciId,
            DateTime kararTarihi,
            CancellationToken cancellationToken = default);

        Task<bool> ReddetAsync(
            int id,
            int kararVerenKullaniciId,
            DateTime kararTarihi,
            string kararAciklamasi,
            CancellationToken cancellationToken = default);

        Task<bool> CalistirmayiTamamlaAsync(
            int id,
            int kararVerenKullaniciId,
            OnayCalistirmaDurumu durum,
            DateTime bitisTarihi,
            string? kullaniciyaGuvenliHata,
            CancellationToken cancellationToken = default);

        Task<OnayGecmisiSayfaliSonuc> GetGecmisAsync(
            int kullaniciId,
            bool bekleyenleriGorebilir,
            OnayErisimKapsami erisimKapsami,
            OnayGecmisiFiltresi filtre,
            CancellationToken cancellationToken = default);

        Task<OnayGecmisiKaydi?> GetGecmisDetayiAsync(
            int id,
            int kullaniciId,
            bool bekleyenleriGorebilir,
            OnayErisimKapsami erisimKapsami,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OnayBekleyenSorguKaydi>> GetYetkiliBekleyenlerAsync(
            int kullaniciId,
            OnayErisimKapsami erisimKapsami,
            CancellationToken cancellationToken = default);

        Task<int> GetYetkiliBekleyenSayisiAsync(
            int kullaniciId,
            OnayErisimKapsami erisimKapsami,
            CancellationToken cancellationToken = default);

        Task<CekiRevizyonOnizlemeKaydi?> GetRevizyonOnizlemeKaydiAsync(
            int talepId,
            int talepEdenKullaniciId,
            int? projeId,
            CancellationToken cancellationToken = default);
    }
}
