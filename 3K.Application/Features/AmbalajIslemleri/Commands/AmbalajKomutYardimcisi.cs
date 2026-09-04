using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.AmbalajIslemleri.Commands
{
    internal sealed record AmbalajBaglantiSonucu(
        Proje? Proje,
        AmbalajUretimKaydi? UstKayit,
        string? Hata,
        int HataKodu = 400);

    internal static class AmbalajKomutYardimcisi
    {
        public static async Task<AmbalajBaglantiSonucu> BaglantilariDogrulaAsync(
            IUnitOfWork unitOfWork,
            IAmbalajKayitAlanlari request,
            int? mevcutKayitId = null)
        {
            Proje? proje = null;
            if (request.ProjeId.HasValue)
            {
                proje = await unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId.Value);
                if (proje == null)
                    return new AmbalajBaglantiSonucu(null, null, "Proje bulunamadı.", 404);
            }

            AmbalajUretimKaydi? ustKayit = null;
            if (request.UstKayitId.HasValue)
            {
                if (request.UstKayitId == mevcutKayitId)
                    return new AmbalajBaglantiSonucu(proje, null, "Bir kayıt kendisinin üst sandığı olamaz.");

                ustKayit = await unitOfWork.GetRepository<AmbalajUretimKaydi>()
                    .GetByIdAsync(request.UstKayitId.Value);
                if (ustKayit == null)
                    return new AmbalajBaglantiSonucu(proje, null, "Üst sandık bulunamadı.", 404);
                if (ustKayit.IptalMi)
                    return new AmbalajBaglantiSonucu(proje, ustKayit, "İptal edilmiş kayıt üst sandık olarak seçilemez.", 409);
                if (ustKayit.Tur == AmbalajSandikTuru.Ic)
                    return new AmbalajBaglantiSonucu(proje, ustKayit, "İç sandık başka bir iç sandığın üst sandığı olamaz.", 409);
                if (!AyniProje(request, ustKayit))
                    return new AmbalajBaglantiSonucu(proje, ustKayit, "İç sandık ile üst sandık aynı projeye ait olmalıdır.", 409);
            }

            var normalSandikNo = request.SandikNo.Trim().ToUpperInvariant();
            var normalManuelProjeNo = AmbalajUretimYardimcilari.Temizle(request.ManuelProjeNo)?.ToUpperInvariant();
            var ayniKayitVar = unitOfWork.GetRepository<AmbalajUretimKaydi>()
                .Queryable()
                .Any(k =>
                    !k.IptalMi &&
                    (!mevcutKayitId.HasValue || k.Id != mevcutKayitId.Value) &&
                    k.SandikNo.ToUpper() == normalSandikNo &&
                    (request.ProjeId.HasValue
                        ? k.ProjeId == request.ProjeId
                        : k.ProjeId == null && k.ManuelProjeNo != null &&
                          k.ManuelProjeNo.ToUpper() == normalManuelProjeNo));
            if (ayniKayitVar)
                return new AmbalajBaglantiSonucu(proje, ustKayit, "Bu projede aynı sandık numarasıyla aktif bir üretim kaydı zaten var.", 409);

            return new AmbalajBaglantiSonucu(proje, ustKayit, null);
        }

        public static void OrtakAlanlariUygula(AmbalajUretimKaydi kayit, IAmbalajKayitAlanlari request)
        {
            kayit.ProjeId = request.ProjeId;
            kayit.ManuelProjeNo = request.ProjeId.HasValue
                ? null
                : AmbalajUretimYardimcilari.Temizle(request.ManuelProjeNo);
            kayit.ManuelProjeAdi = request.ProjeId.HasValue
                ? null
                : AmbalajUretimYardimcilari.Temizle(request.ManuelProjeAdi);
            kayit.UstKayitId = request.UstKayitId;
            kayit.Tur = request.Tur;
            kayit.SandikNo = request.SandikNo.Trim();
            kayit.Ad = AmbalajUretimYardimcilari.Temizle(request.Ad);
            kayit.SandikCinsi = request.SandikCinsi;
            kayit.DigerSandikCinsi = request.SandikCinsi == AmbalajSandikCinsi.Diger
                ? AmbalajUretimYardimcilari.Temizle(request.DigerSandikCinsi)
                : null;
            kayit.Adet = request.Adet;
            kayit.Boy = request.Boy;
            kayit.En = request.En;
            kayit.Yukseklik = request.Yukseklik;
            kayit.KullanimAmaci = AmbalajUretimYardimcilari.Temizle(request.KullanimAmaci);
            kayit.TalepEdenKisi = AmbalajUretimYardimcilari.Temizle(request.TalepEdenKisi);
            kayit.TalepEdenBolum = AmbalajUretimYardimcilari.Temizle(request.TalepEdenBolum);
            kayit.TalimatVeren = AmbalajUretimYardimcilari.Temizle(request.TalimatVeren);
            kayit.FirinPartiNo = AmbalajUretimYardimcilari.Temizle(request.FirinPartiNo);
            kayit.Aciklama = AmbalajUretimYardimcilari.Temizle(request.Aciklama);
            AmbalajUretimYardimcilari.M3DegerleriniHesapla(kayit);
        }

        public static AmbalajUretimKaydiDto DtoOlustur(
            AmbalajUretimKaydi kayit,
            Proje? proje,
            AmbalajUretimKaydi? ustKayit) =>
            AmbalajUretimYardimcilari.DtoOlustur(
                kayit,
                proje?.ProjeNo,
                proje?.Musteri,
                ustKayit?.SandikNo);

        private static bool AyniProje(IAmbalajKayitAlanlari request, AmbalajUretimKaydi ustKayit)
        {
            if (request.ProjeId.HasValue)
                return ustKayit.ProjeId == request.ProjeId;

            return ustKayit.ProjeId == null &&
                   string.Equals(
                       AmbalajUretimYardimcilari.Temizle(request.ManuelProjeNo),
                       AmbalajUretimYardimcilari.Temizle(ustKayit.ManuelProjeNo),
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
