using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Commands;

public sealed class AmbalajGerceklesmeDuzeltCommandHandler(IUnitOfWork uow, ICurrentUserService user, IRolService roles)
    : IRequestHandler<AmbalajGerceklesmeDuzeltCommand, Result<AmbalajGerceklesmeDto>>
{
    public async Task<Result<AmbalajGerceklesmeDto>> Handle(AmbalajGerceklesmeDuzeltCommand r, CancellationToken ct)
    {
        if (!user.IslemKullaniciId.HasValue) return Result<AmbalajGerceklesmeDto>.Failure("Oturum gereklidir.", 401);
        if (r.Adet <= 0 || r.Tarih == default || r.Tarih.Date > TurkeyTime.Now.Date || string.IsNullOrWhiteSpace(r.Gerekce) || r.Gerekce.Length > 1000 || r.NetM3 < 0 || r.SarfM3 < 0)
            return Result<AmbalajGerceklesmeDto>.Failure("Geçerli tarih, adet, miktar ve düzeltme gerekçesi gereklidir.");
        return await uow.ExecuteInTransactionAsync(async token =>
        {
            var repo = uow.GetRepository<AmbalajUretimGerceklesmesi>();
            var eski = await repo.GetByIdAsync(r.Id);
            if (eski == null) return Result<AmbalajGerceklesmeDto>.Failure("Gerçekleşme bulunamadı.", 404);
            if (r.BeklenenSurum != eski.Surum || repo.Queryable().Any(x => x.OncekiSurumId == eski.Id))
                return Result<AmbalajGerceklesmeDto>.Failure("Gerçekleşme değişti; güncel sürümü açınız.", 409);
            var m3 = AmbalajUretimPolitikasi.M3HesaplanabilirMi(eski.SandikCinsi);
            if ((!m3 && (r.NetM3.HasValue || r.SarfM3.HasValue)) || (m3 && (!r.NetM3.HasValue || !r.SarfM3.HasValue)))
                return Result<AmbalajGerceklesmeDto>.Failure("Ürün cinsi ile üretim m³ alanları uyumlu değil.");
            var yeni = new AmbalajUretimGerceklesmesi
            {
                GerceklesmeKimligi = eski.GerceklesmeKimligi, Surum = eski.Surum + 1, OncekiSurumId = eski.Id,
                AmbalajUretimKaydiId = eski.AmbalajUretimKaydiId, IsAkisKimligi = eski.IsAkisKimligi,
                GerceklesmeTarihi = DateTime.SpecifyKind(r.Tarih, DateTimeKind.Unspecified),
                ProjeId = eski.ProjeId, ProjeNo = eski.ProjeNo, ProjeAdi = eski.ProjeAdi,
                SandikNo = eski.SandikNo, SandikAdi = eski.SandikAdi, SandikCinsi = eski.SandikCinsi, Tur = eski.Tur,
                Boy = eski.Boy, En = eski.En, Yukseklik = eski.Yukseklik, FormulVersiyonu = eski.FormulVersiyonu,
                Adet = r.Adet, NetM3 = r.NetM3, SarfM3 = r.SarfM3, KullaniciId = user.IslemKullaniciId.Value, Gerekce = r.Gerekce.Trim()
            };
            await repo.AddAsync(yeni);
            await uow.GetRepository<AmbalajUretimHareketi>().AddAsync(new()
            {
                AmbalajUretimKaydiId = eski.AmbalajUretimKaydiId, KullaniciId = user.IslemKullaniciId.Value,
                Islem = "Gerçekleşme düzeltildi", AlanAdi = "GerceklesmeSnapshot",
                EskiDeger = System.Text.Json.JsonSerializer.Serialize(new { eski.Surum, eski.Adet, eski.GerceklesmeTarihi, eski.NetM3, eski.SarfM3 }),
                YeniDeger = System.Text.Json.JsonSerializer.Serialize(new { yeni.Surum, yeni.Adet, yeni.GerceklesmeTarihi, yeni.NetM3, yeni.SarfM3 }),
                Aciklama = r.Gerekce.Trim()
            });
            await uow.SaveChangesAsync(token);
            var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(roles, user, token);
            return Result<AmbalajGerceklesmeDto>.Success(AmbalajGerceklesenRaporQueryHandler.Dto(yeni, izin.M3Gorunur, izin.SarfGorunur));
        }, ct);
    }
}
