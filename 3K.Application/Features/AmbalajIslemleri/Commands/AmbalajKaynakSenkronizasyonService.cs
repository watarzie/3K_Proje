using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public sealed record AmbalajSenkronizasyonSonucu(int Eklenen, int Guncellenen, int Degismeyen);

    /// <summary>
    /// Projenin kaynak sandıklarını ambalaj üretim kayıtlarına yansıtır. Plan, ambalaj kararı,
    /// bağımsız sandık ve form komutları ile arka plan kuyruğu aynı kuralları buradan kullanır.
    /// </summary>
    public interface IAmbalajKaynakSenkronizasyonService
    {
        Task<Result<AmbalajSenkronizasyonSonucu>> SenkronizeEtAsync(
            int projeId,
            ICurrentUserService islemiYapan,
            CancellationToken cancellationToken);
    }

    public sealed class AmbalajKaynakSenkronizasyonService : IAmbalajKaynakSenkronizasyonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFinansUretimAktarimService _finansService;

        public AmbalajKaynakSenkronizasyonService(IUnitOfWork unitOfWork, IFinansUretimAktarimService finansService)
        {
            _unitOfWork = unitOfWork;
            _finansService = finansService;
        }

        public async Task<Result<AmbalajSenkronizasyonSonucu>> SenkronizeEtAsync(
            int projeId,
            ICurrentUserService islemiYapan,
            CancellationToken cancellationToken)
        {
            var kullaniciId = islemiYapan.IslemKullaniciId;
            if (!kullaniciId.HasValue || kullaniciId <= 0)
                return Result<AmbalajSenkronizasyonSonucu>.Failure("Senkronizasyon için geçerli bir kullanıcı gereklidir.", 401);
            return await _unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                // Proje/kaynak/mevcut kayıt okumaları da SERIALIZABLE transaction'ın
                // içinde olmalıdır; aksi halde iki eşzamanlı senkronizasyon aynı kaynağı
                // ayrı ayrı eklemeye karar verebilir.
                var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(projeId);
                if (proje == null)
                    return Result<AmbalajSenkronizasyonSonucu>.Failure("Proje bulunamadı.", 404);

                var kaynakModul = KaynakModulBelirle(proje.ProjeTipiId);
                var tur = TurBelirle(proje.ProjeTipiId);
                var sandiklar = _unitOfWork.GetRepository<Sandik>().Queryable()
                    .Where(s => s.ProjeId == projeId)
                    .OrderBy(s => s.SandikNo)
                    .ToList();
                var kaynakKayitIdleri = sandiklar.Select(s => s.Id).ToHashSet();
                var kayitRepo = _unitOfWork.GetRepository<AmbalajUretimKaydi>();

                var globalMevcutlar = kaynakKayitIdleri.Count == 0
                    ? new Dictionary<int, AmbalajUretimKaydi>()
                    : kayitRepo.Queryable()
                        .Where(k => k.KaynakKayitId.HasValue && kaynakKayitIdleri.Contains(k.KaynakKayitId.Value))
                        .ToList()
                        .GroupBy(k => k.KaynakKayitId!.Value)
                        .ToDictionary(
                            g => g.Key,
                            g => g.OrderByDescending(k => k.KaynakModul == kaynakModul)
                                .ThenByDescending(k => k.Id)
                                .First());

                var eksilenKayitlar = kayitRepo.Queryable()
                    .Where(k => k.ProjeId == proje.Id &&
                                k.KaynakModul == kaynakModul &&
                                k.KaynakKayitId.HasValue)
                    .ToList()
                    .Where(k => !kaynakKayitIdleri.Contains(k.KaynakKayitId!.Value) &&
                                k.KaynakEksikligindeIptalEdilebilirMi())
                    .ToList();

                var eklenen = 0;
                var guncellenen = 0;
                var degismeyen = 0;
                var finansaAktarilacaklar = new Dictionary<Guid, AmbalajUretimKaydi>();

                foreach (var eksilen in eksilenKayitlar)
                {
                    var eski = AmbalajUretimYardimcilari.Snapshot(eksilen);
                    eksilen.KaynakEksikligindenIptalEt();
                    kayitRepo.Update(eksilen);
                    await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                        _unitOfWork,
                        eksilen,
                        eski,
                        "Kaynakta bulunmayan planlı kayıt sistem tarafından pasifleştirildi",
                        kullaniciId.Value,
                        AmbalajUretimKaydi.SistemKaynakEksigiNedeni);
                    finansaAktarilacaklar[eksilen.IsAkisKimligi] = eksilen;
                    guncellenen++;
                }

                var tamamlanmisProje = AmbalajUretimPolitikasi.ProjeDurumu(
                    kayitRepo.Queryable().Where(k => k.ProjeId == proje.Id).ToList()) == AmbalajUretimDurumu.Tamamlandi;
                // Plan başlangıcı projenin ilk senkron kaydıdır; bu senkronda eklenen kayıtlar henüz
                // kaydedilmediği için hesaba katılmaz.
                var planBaslangici = kayitRepo.Queryable()
                    .Where(k => k.ProjeId == proje.Id && !k.IptalMi && !k.BagimsizKayitMi && k.KaynakKayitId.HasValue)
                    .Select(k => (DateTime?)k.CreatedDate)
                    .Min();
                foreach (var sandik in sandiklar)
                {
                    transactionToken.ThrowIfCancellationRequested();
                    if (!globalMevcutlar.TryGetValue(sandik.Id, out var kayit))
                    {
                        kayit = YeniKayitOlustur(proje, sandik, kaynakModul,
                            AmbalajUretimPolitikasi.KaynakGrubu(tur, null, planBaslangici, sandik.CreatedDate));
                        if (tamamlanmisProje) kayit.AmbalajKarariniUygula(false);
                        await kayitRepo.AddAsync(kayit);
                        await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                            _unitOfWork,
                            kayit,
                            null,
                            "Kaynak sandık ambalaj üretimine aktarıldı",
                            kullaniciId.Value,
                            $"Kaynak: {kaynakModul}, Sandık: {sandik.SandikNo}");
                        globalMevcutlar[sandik.Id] = kayit;
                        eklenen++;
                        continue;
                    }

                    var sistemIptalindenDonuyor = kayit.SistemIptaliGeriAlinabilirMi();
                    if ((kayit.IptalMi && !sistemIptalindenDonuyor) ||
                        kayit.BagimsizKayitMi ||
                        kayit.KaynakSenkronizasyonuKilitliMi ||
                        kayit.UretimDurumu != AmbalajUretimDurumu.Planlandi)
                    {
                        // Kullanıcı iptali ve kilitli/ilerlemiş kayıtlar otomatik
                        // senkronizasyon tarafından asla geri alınmaz veya ezilmez.
                        degismeyen++;
                        continue;
                    }

                    var eski = AmbalajUretimYardimcilari.Snapshot(kayit);
                    if (sistemIptalindenDonuyor) kayit.SistemIptaliniGeriAl();

                    KaynakAlanlariniUygula(kayit, proje, sandik, kaynakModul,
                        AmbalajUretimPolitikasi.KaynakGrubu(tur, kayit.Tur, planBaslangici, sandik.CreatedDate));
                    var yeni = AmbalajUretimYardimcilari.Snapshot(kayit);
                    var sadeceSenkronizasyonTarihiDegisti = yeni
                        .Where(x => x.Key != nameof(AmbalajUretimKaydi.KaynakSonSenkronizasyonTarihi))
                        .All(x => eski.TryGetValue(x.Key, out var eskiDeger) && eskiDeger == x.Value);
                    if (sadeceSenkronizasyonTarihiDegisti)
                    {
                        kayit.KaynakSonSenkronizasyonTarihi = TarihiCoz(
                            eski[nameof(AmbalajUretimKaydi.KaynakSonSenkronizasyonTarihi)]);
                        degismeyen++;
                        continue;
                    }

                    kayitRepo.Update(kayit);
                    await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                        _unitOfWork,
                        kayit,
                        eski,
                        sistemIptalindenDonuyor
                            ? "Sistemin pasifleştirdiği kaynak kayıt yeniden aktifleştirildi"
                            : "Kaynak sandık değişiklikleri senkronize edildi",
                        kullaniciId.Value,
                        sistemIptalindenDonuyor ? "Kaynak yeniden görüldü." : null);
                    if (kayit.UretimeAlindi || sistemIptalindenDonuyor)
                        finansaAktarilacaklar[kayit.IsAkisKimligi] = kayit;
                    guncellenen++;
                }

                await _unitOfWork.SaveChangesAsync(transactionToken);
                if (finansaAktarilacaklar.Count > 0)
                {
                    await _finansService.UretimKayitlariniAktarAsync(
                        finansaAktarilacaklar.Values
                            .Select(k => AmbalajFinansSenkronizasyonu.ModelOlustur(k, proje))
                            .ToList(),
                        transactionToken);
                }

                return Result<AmbalajSenkronizasyonSonucu>.Success(
                    new AmbalajSenkronizasyonSonucu(eklenen, guncellenen, degismeyen));
            }, cancellationToken);
        }

        private static AmbalajUretimKaydi YeniKayitOlustur(
            Proje proje,
            Sandik sandik,
            AmbalajKaynakModulu kaynakModul,
            AmbalajSandikTuru tur)
        {
            var kayit = new AmbalajUretimKaydi
            {
                AmbalajaDahil = !AmbalajUretimPolitikasi.VarsayilanYapilmazMi(sandik.Ad, sandik.AdIngilizce),
                UretimeAlindi = false,
                SarfOrani = AmbalajHesaplayici.VarsayilanSarfOrani,
                UretimDurumu = AmbalajUretimDurumu.Planlandi
            };
            KaynakAlanlariniUygula(kayit, proje, sandik, kaynakModul, tur);
            return kayit;
        }

        /// <summary>
        /// Üretim formu oluşturulurken seçimdeki kaydı güncel kaynak sandıktan tazeler (ad, cins, numara/adet,
        /// ölçü ve m³). Otomatik senkronizasyon kilitli/üretimdeki kaydı ezmez; aynı kaynak kuralı burada
        /// kullanıcının açık form işlemiyle uygulanır. Kayıtlı Normal/İlave grubu korunur.
        /// </summary>
        internal static void FormIcinKaynaktanTazele(AmbalajUretimKaydi kayit, Proje proje, Sandik sandik) =>
            KaynakAlanlariniUygula(kayit, proje, sandik, KaynakModulBelirle(proje.ProjeTipiId),
                AmbalajUretimPolitikasi.KaynakGrubu(TurBelirle(proje.ProjeTipiId), kayit.Tur, null, sandik.CreatedDate));

        private static void KaynakAlanlariniUygula(
            AmbalajUretimKaydi kayit,
            Proje proje,
            Sandik sandik,
            AmbalajKaynakModulu kaynakModul,
            AmbalajSandikTuru tur)
        {
            kayit.ProjeId = proje.Id;
            kayit.ManuelProjeNo = null;
            kayit.ManuelProjeAdi = null;
            kayit.Tur = tur;
            kayit.KaynakModul = kaynakModul;
            kayit.KaynakKayitId = sandik.Id;
            kayit.KaynakSonSenkronizasyonTarihi = TurkeyTime.Now;
            var kaynak = AmbalajUretimPolitikasi.KaynakDegerleri(sandik);
            kayit.SandikNo = sandik.SandikNo;
            kayit.Ad = kaynak.Ad;
            kayit.SandikCinsi = kaynak.Cins;
            kayit.Adet = AmbalajUretimYardimcilari.SandikAdediHesapla(sandik.SandikNo);
            // Sandık ölçüsü ölçeksiz numeric, ambalaj kaydınınki numeric(18,4)'tür: aynı değer (1400 ile 1400.0000)
            // yeniden atanırsa hareket geçmişine sahte değişiklik yazılır; yalnız farklı değer atanır.
            if (kayit.Boy != kaynak.Boy) kayit.Boy = kaynak.Boy;
            if (kayit.En != kaynak.En) kayit.En = kaynak.En;
            if (kayit.Yukseklik != kaynak.Yukseklik) kayit.Yukseklik = kaynak.Yukseklik;
            AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
        }

        private static DateTime? TarihiCoz(string? value) =>
            DateTime.TryParse(value, out var result) ? result : null;

        private static AmbalajKaynakModulu KaynakModulBelirle(int projeTipiId) => projeTipiId switch
        {
            (int)ProjeTipi.Saha => AmbalajKaynakModulu.Saha,
            (int)ProjeTipi.Yedek => AmbalajKaynakModulu.Yedek,
            _ => AmbalajKaynakModulu.Sandik
        };

        private static AmbalajSandikTuru TurBelirle(int projeTipiId) => projeTipiId switch
        {
            (int)ProjeTipi.Saha => AmbalajSandikTuru.Saha,
            (int)ProjeTipi.Yedek => AmbalajSandikTuru.Yedek,
            _ => AmbalajSandikTuru.Normal
        };
    }
}
