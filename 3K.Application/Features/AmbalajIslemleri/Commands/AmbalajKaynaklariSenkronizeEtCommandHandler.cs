using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    public interface IAmbalajKaynakSenkronizasyonService
    {
        Task<Result<AmbalajSenkronizasyonSonucuDto>> SenkronizeEtAsync(
            int projeId,
            ICurrentUserService islemiYapan,
            CancellationToken cancellationToken,
            bool sonucKayitlariniOlustur = true);
    }

    public sealed class AmbalajKaynaklariSenkronizeEtCommandHandler
        : IRequestHandler<AmbalajKaynaklariSenkronizeEtCommand, Result<AmbalajSenkronizasyonSonucuDto>>,
          IAmbalajKaynakSenkronizasyonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFinansUretimAktarimService _finansService;
        private readonly IRolService _rolService;

        public AmbalajKaynaklariSenkronizeEtCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IFinansUretimAktarimService finansService,
            IRolService rolService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _finansService = finansService;
            _rolService = rolService;
        }

        public async Task<Result<AmbalajSenkronizasyonSonucuDto>> Handle(
            AmbalajKaynaklariSenkronizeEtCommand request,
            CancellationToken cancellationToken)
            => await SenkronizeEtAsync(request.ProjeId, _currentUserService, cancellationToken);

        public async Task<Result<AmbalajSenkronizasyonSonucuDto>> SenkronizeEtAsync(
            int projeId,
            ICurrentUserService islemiYapan,
            CancellationToken cancellationToken,
            bool sonucKayitlariniOlustur = true)
        {
            var kullaniciId = islemiYapan.UserId;
            if (!kullaniciId.HasValue || kullaniciId <= 0)
                return Result<AmbalajSenkronizasyonSonucuDto>.Failure("Senkronizasyon için geçerli bir kullanıcı gereklidir.", 401);
            return await _unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
            {
                // Proje/kaynak/mevcut kayıt okumaları da SERIALIZABLE transaction'ın
                // içinde olmalıdır; aksi halde iki eşzamanlı senkronizasyon aynı kaynağı
                // ayrı ayrı eklemeye karar verebilir.
                var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(projeId);
                if (proje == null)
                    return Result<AmbalajSenkronizasyonSonucuDto>.Failure("Proje bulunamadı.", 404);

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

                foreach (var sandik in sandiklar)
                {
                    transactionToken.ThrowIfCancellationRequested();
                    if (!globalMevcutlar.TryGetValue(sandik.Id, out var kayit))
                    {
                        kayit = YeniKayitOlustur(proje, sandik, kaynakModul, tur);
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

                var tumKayitlar = globalMevcutlar.Values.OrderBy(k => k.SandikNo).ToList();
                IReadOnlyList<AmbalajUretimKaydiDto> dtoKayitlar = [];
                if (sonucKayitlariniOlustur)
                {
                    var detayliKayitlar = tumKayitlar
                        .Select(k => AmbalajKomutYardimcisi.DtoOlustur(k, proje, null))
                        .ToList();
                    var yetkiler = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(
                        _rolService, islemiYapan, transactionToken);
                    detayliKayitlar.ForEach(dto => AmbalajYetkilendirmeYardimcisi.DtoyuMaskele(dto, yetkiler));
                    dtoKayitlar = detayliKayitlar;
                }

                return Result<AmbalajSenkronizasyonSonucuDto>.Success(new AmbalajSenkronizasyonSonucuDto(
                    eklenen,
                    guncellenen,
                    degismeyen,
                    tumKayitlar.Count(k => !AmbalajUretimYardimcilari.OlculerGecerli(k)),
                    dtoKayitlar));
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
                AmbalajaDahil = true,
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
            kayit.SandikCinsi = CinsBelirle(sandik.TipId);
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

        private static AmbalajSandikCinsi CinsBelirle(int tipId) => tipId switch
        {
            (int)SandikTipi.KatlanirSandik => AmbalajSandikCinsi.Katlanir,
            _ => AmbalajSandikCinsi.AhsapKapali
        };
    }
}
