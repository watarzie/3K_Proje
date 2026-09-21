using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

public sealed class AmbalajFormSurumleriQueryHandler(IUnitOfWork uow, IRolService roles,
    ICurrentUserService user, IAmbalajRaporDosyaService files)
    : IRequestHandler<GetAmbalajFormSurumleriQuery, Result<IReadOnlyList<AmbalajFormSurumuDto>>>,
      IRequestHandler<GetAmbalajFormSurumuDosyasiQuery, Result<AmbalajDosyaDto>>
{
    public async Task<Result<IReadOnlyList<AmbalajFormSurumuDto>>> Handle(GetAmbalajFormSurumleriQuery r, CancellationToken ct)
    {
        if (!r.ProjeId.HasValue && !r.KayitId.HasValue && !r.Id.HasValue)
            return Result<IReadOnlyList<AmbalajFormSurumuDto>>.Failure("Proje, kayıt veya form kimliği gereklidir.");
        var query = uow.GetRepository<AmbalajUretimFormuSurumu>().Queryable();
        if (r.Id.HasValue) query = query.Where(x => x.Id == r.Id);
        if (r.ProjeId.HasValue) query = query.Where(x => x.ProjeId == r.ProjeId);
        if (r.KayitId.HasValue)
        {
            var ids = uow.GetRepository<AmbalajUretimFormuKaydi>().Queryable()
                .Where(x => x.AmbalajUretimKaydiId == r.KayitId).Select(x => x.FormSurumuId);
            query = query.Where(x => ids.Contains(x.Id));
        }
        var sonuc = new List<AmbalajFormSurumuDto>();
        foreach (var entity in query.OrderByDescending(x => x.Surum).Take(100).ToList())
        {
            ct.ThrowIfCancellationRequested();
            sonuc.Add(await AmbalajYasamDongusuYardimcisi.FormDtoAsync(entity, roles, user, ct));
        }
        return Result<IReadOnlyList<AmbalajFormSurumuDto>>.Success(sonuc);
    }

    public async Task<Result<AmbalajDosyaDto>> Handle(GetAmbalajFormSurumuDosyasiQuery r, CancellationToken ct)
    {
        if (r.Format is not ("pdf" or "xlsx")) return Result<AmbalajDosyaDto>.Failure("PDF veya XLSX seçiniz.");
        var entity = await uow.GetRepository<AmbalajUretimFormuSurumu>().GetByIdAsync(r.Id);
        if (entity == null) return Result<AmbalajDosyaDto>.Failure("Form sürümü bulunamadı.", 404);
        var dto = await AmbalajYasamDongusuYardimcisi.FormDtoAsync(entity, roles, user, ct);
        return Result<AmbalajDosyaDto>.Success(new(
            r.Format == "pdf" ? files.UretimFormuPdfOlustur(dto.Form) : files.UretimFormuExcelOlustur(dto.Form),
            r.Format == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"uretim-formu-{entity.Id}-v{entity.Surum}.{r.Format}"));
    }
}

public sealed class AmbalajGerceklesenRaporQueryHandler(IUnitOfWork uow, IRolService roles,
    ICurrentUserService user, IAmbalajRaporDosyaService files)
    : IRequestHandler<GetAmbalajGerceklesenRaporQuery, Result<AmbalajGerceklesenRaporDto>>,
      IRequestHandler<GetAmbalajGerceklesenRaporDosyasiQuery, Result<AmbalajDosyaDto>>
{
    public async Task<Result<AmbalajGerceklesenRaporDto>> Handle(GetAmbalajGerceklesenRaporQuery r, CancellationToken ct)
    {
        if (r.Baslangic == default || r.Bitis == default || r.Bitis < r.Baslangic || (r.Bitis - r.Baslangic).TotalDays > 3660)
            return Result<AmbalajGerceklesenRaporDto>.Failure("Geçerli başlangıç/bitiş ve en fazla 10 yıllık tarih aralığı seçiniz.");
        var baslangic = DateTime.SpecifyKind(r.Baslangic.Date, DateTimeKind.Unspecified);
        var bitis = DateTime.SpecifyKind(r.Bitis.Date.AddDays(1), DateTimeKind.Unspecified);
        var tum = uow.GetRepository<AmbalajUretimGerceklesmesi>().Queryable();
        // Önce güncel sürüm belirlenir, sonra tarih filtresi uygulanır. Tarihi taşınan eski sürüm geri gelmez.
        var gecerli = tum.Where(x => !tum.Any(y => y.OncekiSurumId == x.Id));
        var query = gecerli.Where(x => x.GerceklesmeTarihi >= baslangic && x.GerceklesmeTarihi < bitis);
        if (r.ProjeId.HasValue) query = query.Where(x => x.ProjeId == r.ProjeId);
        var veriler = query.OrderBy(x => x.GerceklesmeTarihi).ThenBy(x => x.Id).Take(50001).ToList();
        if (veriler.Count > 50000) return Result<AmbalajGerceklesenRaporDto>.Failure("50.000 kayıt sınırı aşıldı. Tarih aralığını daraltınız.");
        ct.ThrowIfCancellationRequested();
        var izin = await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(roles, user, ct);
        var raporM3 = await AmbalajYetkilendirmeYardimcisi.YetkiliMiAsync(roles, user, AmbalajMenuKodlari.M3RaporGoruntule, ct, YetkiTipi.R);
        var m3 = izin.M3Gorunur && raporM3;
        var sarf = izin.SarfGorunur && m3;
        var dtos = veriler.Select(x => Dto(x, m3, sarf)).ToList();
        AmbalajGerceklesmeOzetDto Ozet(string key, IEnumerable<AmbalajGerceklesmeDto> list)
        {
            var values = list.ToList();
            return new(key, values.Sum(x => x.Adet), m3 && values.Any(x => x.NetM3.HasValue) ? values.Sum(x => x.NetM3) : null,
                sarf && values.Any(x => x.SarfM3.HasValue) ? values.Sum(x => x.SarfM3) : null,
                sarf && values.Any(x => x.SarfDahilM3.HasValue) ? values.Sum(x => x.SarfDahilM3) : null);
        }
        return Result<AmbalajGerceklesenRaporDto>.Success(new()
        {
            Baslangic = baslangic, Bitis = r.Bitis.Date, M3Gorunur = m3, SarfGorunur = sarf,
            Kayitlar = dtos, Toplam = Ozet("Toplam", dtos),
            Projeler = dtos.GroupBy(x => x.ProjeNo).Select(g => Ozet(g.Key, g)).ToList(),
            Gunler = dtos.GroupBy(x => x.Tarih.ToString("yyyy-MM-dd")).Select(g => Ozet(g.Key, g)).ToList(),
            Aylar = dtos.GroupBy(x => x.Tarih.ToString("yyyy-MM")).Select(g => Ozet(g.Key, g)).ToList(),
            Cinsler = dtos.GroupBy(x => x.SandikCinsi).Select(g => Ozet(AmbalajUretimYardimcilari.CinsMetni(g.Key, null), g) with
                { NetM3 = m3 && AmbalajUretimPolitikasi.M3HesaplanabilirMi(g.Key) ? g.Sum(x => x.NetM3) : null,
                  SarfM3 = sarf && AmbalajUretimPolitikasi.M3HesaplanabilirMi(g.Key) ? g.Sum(x => x.SarfM3) : null,
                  SarfDahilM3 = sarf && AmbalajUretimPolitikasi.M3HesaplanabilirMi(g.Key) ? g.Sum(x => x.SarfDahilM3) : null }).ToList(),
            TarihiBelirsizEskiKayitSayisi = uow.GetRepository<AmbalajUretimKaydi>().Queryable().Count(k =>
                k.UretimDurumu == AmbalajUretimDurumu.Tamamlandi && (!r.ProjeId.HasValue || k.ProjeId == r.ProjeId) &&
                !tum.Any(x => x.AmbalajUretimKaydiId == k.Id))
        });
    }

    internal static AmbalajGerceklesmeDto Dto(AmbalajUretimGerceklesmesi x, bool m3, bool sarf) => new()
    {
        Id = x.Id, GerceklesmeKimligi = x.GerceklesmeKimligi, Surum = x.Surum, KayitId = x.AmbalajUretimKaydiId,
        Tarih = x.GerceklesmeTarihi, ProjeId = x.ProjeId, ProjeNo = x.ProjeNo, SandikNo = x.SandikNo,
        SandikCinsi = x.SandikCinsi, Tur = x.Tur, Adet = x.Adet,
        M3HesaplanabilirMi = AmbalajUretimPolitikasi.M3HesaplanabilirMi(x.SandikCinsi),
        NetM3 = m3 ? x.NetM3 : null, SarfM3 = sarf ? x.SarfM3 : null,
        SarfDahilM3 = m3 && sarf ? x.NetM3 + x.SarfM3 : null
    };

    public async Task<Result<AmbalajDosyaDto>> Handle(GetAmbalajGerceklesenRaporDosyasiQuery r, CancellationToken ct)
    {
        if (r.Format is not ("pdf" or "xlsx")) return Result<AmbalajDosyaDto>.Failure("PDF veya XLSX seçiniz.");
        var result = await Handle(new GetAmbalajGerceklesenRaporQuery { Baslangic = r.Baslangic, Bitis = r.Bitis, ProjeId = r.ProjeId }, ct);
        if (!result.IsSuccess) return Result<AmbalajDosyaDto>.Failure(result.Error!.Message, result.StatusCode);
        var rapor = result.Value!;
        var satirlar = rapor.Kayitlar.Select(k => new AmbalajRaporSatiri
        {
            KayitId = k.KayitId, ProjeNo = k.ProjeNo, SandikNo = k.SandikNo, Adet = k.Adet,
            SandikCinsi = AmbalajUretimYardimcilari.CinsMetni(k.SandikCinsi, null), Tur = k.Tur,
            NetM3 = k.NetM3, SarfM3 = k.SarfM3, ToplamM3 = k.SarfDahilM3,
            BirimM3 = k.NetM3 / k.Adet, UretimTarihi = k.Tarih, UretimDurumu = AmbalajUretimDurumu.Tamamlandi,
            AmbalajaDahil = true, UretimeAlindi = true, Aciklama = $"Gerçekleşme {k.GerceklesmeKimligi} / sürüm {k.Surum}"
        }).ToList();
        var ozet = new AmbalajRaporOzeti(satirlar.Count, rapor.Toplam.Adet, rapor.Toplam.NetM3, rapor.Toplam.SarfM3, rapor.Toplam.SarfDahilM3);
        return Result<AmbalajDosyaDto>.Success(new(r.Format == "pdf" ? files.PdfOlustur(satirlar, ozet) : files.ExcelOlustur(satirlar, ozet),
            r.Format == "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"gerceklesen-uretim-{r.Baslangic:yyyyMMdd}-{r.Bitis:yyyyMMdd}.{r.Format}"));
    }
}
