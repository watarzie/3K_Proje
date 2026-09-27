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
                                AmbalajKaynakSenkronizasyonPolitikasi.KaynakEksigindeSistemIptalineUygunMu(k))
                    .ToList();

                var eklenen = 0;
                var guncellenen = 0;
                var degismeyen = 0;
                var finansaAktarilacaklar = new Dictionary<Guid, AmbalajUretimKaydi>();

                foreach (var eksilen in eksilenKayitlar)
                {
                    var eski = AmbalajUretimYardimcilari.Snapshot(eksilen);
                    eksilen.IptalOncesiUretimDurumu = eksilen.UretimDurumu;
                    eksilen.IptalMi = true;
                    eksilen.IptalTarihi = TurkeyTime.Now;
                    eksilen.IptalEdenKullaniciId = null;
                    eksilen.IptalNedeni = AmbalajKaynakSenkronizasyonPolitikasi.SistemKaynakEksigiNedeni;
                    kayitRepo.Update(eksilen);
                    await AmbalajUretimYardimcilari.AlanHareketleriniEkleAsync(
                        _unitOfWork,
                        eksilen,
                        eski,
                        "Kaynakta bulunmayan planlı kayıt sistem tarafından pasifleştirildi",
                        kullaniciId.Value,
                        AmbalajKaynakSenkronizasyonPolitikasi.SistemKaynakEksigiNedeni);
                    finansaAktarilacaklar[eksilen.IsAkisKimligi] = eksilen;
                    guncellenen++;
                }

                var tamamlanmisProje = AmbalajUretimPolitikasi.ProjeDurumu(
                    kayitRepo.Queryable().Where(k => k.ProjeId == proje.Id).ToList()) == AmbalajUretimDurumu.Tamamlandi;
                foreach (var sandik in sandiklar)
                {
                    transactionToken.ThrowIfCancellationRequested();
                    if (!globalMevcutlar.TryGetValue(sandik.Id, out var kayit))
                    {
                        kayit = YeniKayitOlustur(proje, sandik, kaynakModul, tur);
                        if (tamamlanmisProje) kayit.AmbalajaDahil = false;
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

                    var sistemIptalindenDonuyor =
                        AmbalajKaynakSenkronizasyonPolitikasi.SistemIptalindenOtomatikAktiflesebilirMi(kayit);
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
                    if (sistemIptalindenDonuyor)
                    {
                        kayit.IptalMi = false;
                        kayit.IptalTarihi = null;
                        kayit.IptalEdenKullaniciId = null;
                        kayit.IptalNedeni = null;
                        kayit.UretimDurumu = kayit.IptalOncesiUretimDurumu ?? AmbalajUretimDurumu.Planlandi;
                        kayit.IptalOncesiUretimDurumu = null;
                    }

                    KaynakAlanlariniUygula(kayit, proje, sandik, kaynakModul, tur);
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
            kayit.SandikNo = sandik.SandikNo;
            kayit.Ad = sandik.Ad;
            kayit.SandikCinsi = AmbalajUretimPolitikasi.KaynakCinsi(sandik.TipId);
            kayit.Adet = AmbalajUretimYardimcilari.SandikAdediHesapla(sandik.SandikNo);
            kayit.Boy = sandik.Boy ?? 0;
            kayit.En = sandik.En ?? 0;
            kayit.Yukseklik = sandik.Yukseklik ?? 0;
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
