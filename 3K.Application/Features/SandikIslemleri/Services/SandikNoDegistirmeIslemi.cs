using System.Globalization;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Services
{
    /// <summary>
    /// Elle eklenen sandığın numarasını değiştirir ve numaranın kopyalandığı kayıtları aynı transaction'da günceller:
    /// projedeki çeki satırlarının <c>FiiliSandikNo</c>/<c>CekideGecenSandikNo</c>'su; sandığa kimlikle bağlı ürün
    /// transferi, saha taşıma, durum olayı ve sevk sonrası düzeltme kayıtlarındaki numara kopyaları; planlı Ambalaj
    /// kaydı (otomatik senkron kuralıyla). Sandık içeriği, sevkiyat ve saha aktarım kalemleri sandığa yalnız kimlikle
    /// bağlıdır. Değişiklik hareket geçmişine "eski → yeni" olarak yazılır. Transaction sınırını çağıran handler
    /// yönetir; başarısız sonuç commit edilmemelidir.
    /// </summary>
    internal sealed class SandikNoDegistirmeIslemi(
        IUnitOfWork unitOfWork,
        IHareketService hareketService,
        IAmbalajKaynakSenkronizasyonService ambalajSenkronizasyonu,
        ICurrentUserService currentUser)
    {
        public const string HareketIslemi = "Sandık No Değiştirildi";

        private List<AmbalajUretimKaydi> _ambalajKayitlari = [];

        /// <summary>Sandık detayındaki numara alanının kilidi: sunucu denetimiyle aynı kural ve veri.</summary>
        public static string? KilitNedeni(IUnitOfWork unitOfWork, Sandik sandik) =>
            SandikNoDegisikligiKurali.KilitNedeni(
                sandik,
                AmbalajCekiSandiklari.ProjeIcinGetir(unitOfWork, sandik.ProjeId),
                BagliAmbalajKayitlari(unitOfWork, sandik.Id));

        /// <summary>
        /// Değişikliği hiçbir kaydı değiştirmeden denetler; handler sandığın öteki alanlarını yalnız bu geçerse yazar.
        /// </summary>
        public Result Denetle(Sandik sandik, string istenenNo)
        {
            var digerNolar = unitOfWork.GetRepository<Sandik>().Queryable()
                .Where(s => s.ProjeId == sandik.ProjeId && s.Id != sandik.Id)
                .Select(s => s.SandikNo)
                .ToList();
            _ambalajKayitlari = BagliAmbalajKayitlari(unitOfWork, sandik.Id);
            var red = SandikNoDegisikligiKurali.Denetle(
                sandik,
                istenenNo,
                AmbalajCekiSandiklari.ProjeIcinGetir(unitOfWork, sandik.ProjeId),
                digerNolar,
                _ambalajKayitlari);
            return red == null ? Result.Success() : Result.Failure(red.Mesaj, red.Kod);
        }

        private static List<AmbalajUretimKaydi> BagliAmbalajKayitlari(IUnitOfWork unitOfWork, int sandikId) =>
            unitOfWork.GetRepository<AmbalajUretimKaydi>().Queryable()
                .Where(k => k.KaynakKayitId == sandikId)
                .ToList();

        /// <summary><see cref="Denetle"/> geçtikten sonra numarayı ve bağlı kayıtları değiştirir, kaydeder.</summary>
        public async Task<Result> UygulaAsync(Sandik sandik, string istenenNo, CancellationToken cancellationToken)
        {
            var yeniNo = istenenNo.Trim();
            var eskiNo = sandik.SandikNo;
            var sandikId = sandik.Id;
            var projeId = sandik.ProjeId;

            sandik.SandikNo = yeniNo;

            // Elle eklenen ürün satırı sandığın numarasını taşır; çeki satırının koli numarası (CekideGecenSandikNo)
            // yalnız elle eklenen satırda değişir, çekiden gelen satırın yalnız fiili sandığı bu sandık olabilir.
            var satirlar = unitOfWork.GetRepository<CekiSatiri>().Queryable()
                .Where(cs => cs.Ceki.ProjeId == projeId &&
                    (cs.FiiliSandikNo == eskiNo || (cs.IsManuelEklenen && cs.CekideGecenSandikNo == eskiNo)))
                .ToList();
            foreach (var satir in satirlar)
            {
                if (satir.FiiliSandikNo == eskiNo)
                    satir.FiiliSandikNo = yeniNo;
                if (satir.IsManuelEklenen && satir.CekideGecenSandikNo == eskiNo)
                    satir.CekideGecenSandikNo = yeniNo;
            }

            foreach (var transfer in unitOfWork.GetRepository<SandikUrunTransferi>().Queryable()
                         .Where(t => t.KaynakSandikId == sandikId || t.HedefSandikId == sandikId)
                         .ToList())
            {
                if (transfer.KaynakSandikId == sandikId) transfer.KaynakSandikNo = yeniNo;
                if (transfer.HedefSandikId == sandikId) transfer.HedefSandikNo = yeniNo;
            }

            foreach (var tasima in unitOfWork.GetRepository<SahaTasimaKalemi>().Queryable()
                         .Where(t => t.KaynakSahaSandikId == sandikId || t.HedefSahaSandikId == sandikId)
                         .ToList())
            {
                if (tasima.KaynakSahaSandikId == sandikId) tasima.KaynakSahaSandikNo = yeniNo;
                if (tasima.HedefSahaSandikId == sandikId) tasima.HedefSahaSandikNo = yeniNo;
            }

            // Durum istatistikleri olayları (ProjeId, SandikNo) ile gruplar; aynı sandığın olayları tek numarada kalır.
            foreach (var olay in unitOfWork.GetRepository<SandikDurumOlayi>().Queryable()
                         .Where(o => o.SandikId == sandikId)
                         .ToList())
                olay.SandikNo = yeniNo;

            foreach (var duzeltme in unitOfWork.GetRepository<SandikDuzeltmeKaydi>().Queryable()
                         .Where(d => d.SandikId == sandikId)
                         .ToList())
                duzeltme.SandikNo = yeniNo;

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = projeId,
                KullaniciId = currentUser.IslemKullaniciId ?? 0,
                ReferansTipi = "Sandik",
                ReferansId = sandikId.ToString(CultureInfo.InvariantCulture),
                ReferansMetni = $"Sandık {yeniNo}",
                Islem = HareketIslemi,
                EskiDeger = eskiNo,
                YeniDeger = yeniNo,
                Aciklama = $"Sandık numarası değiştirildi: {eskiNo} → {yeniNo}"
            });

            // Planlı ve kilitsiz Ambalaj kaydı otomatik senkronun kuralıyla yeni numaraya (ve numaradan çıkan adede)
            // geçer; üretimdeki/kilitli kayıt yukarıda reddedildi. Kaydı olmayan sandığı senkron sonra ekler.
            if (_ambalajKayitlari.Any(k => !k.IptalMi))
            {
                var senkron = await ambalajSenkronizasyonu.SenkronizeEtAsync(projeId, currentUser, cancellationToken);
                if (!senkron.IsSuccess)
                    return Result.Failure(senkron.Error!.Message, senkron.StatusCode);
            }

            return Result.Success();
        }
    }
}
