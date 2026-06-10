using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    public class GetProjeSevkiyatlariQueryHandler : IRequestHandler<GetProjeSevkiyatlariQuery, Result<List<SevkiyatDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetProjeSevkiyatlariQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<Result<List<SevkiyatDto>>> Handle(GetProjeSevkiyatlariQuery request, CancellationToken cancellationToken)
        {
            var sevkiyatlar = _unitOfWork.GetRepository<Sevkiyat>().Queryable()
                .Where(s => s.ProjeId == request.ProjeId)
                .Where(s => s.Sandiklar.Any())
                .OrderBy(s => s.SevkiyatNo)
                .Select(s => new SevkiyatDto
                {
                    Id = s.Id,
                    SevkiyatNo = s.SevkiyatNo,
                    SevkTarihi = s.SevkTarihi,
                    Aciklama = s.Aciklama,
                    AracPlaka = s.AracPlaka,
                    KullaniciAdSoyad = s.Kullanici.AdSoyad,
                    SandikSayisi = s.Sandiklar.Count,
                    KayitTipi = "Sevkiyat",
                    IsKilitAcma = false,
                    Sandiklar = s.Sandiklar
                        .Select(ss => new SevkiyatSandikDto
                        {
                            SandikId = ss.SandikId,
                            SandikNo = ss.Sandik.SandikNo,
                            SandikAdi = ss.Sandik.Ad
                        })
                        .ToList()
                })
                .ToList();

            foreach (var sevkiyat in sevkiyatlar)
            {
                sevkiyat.Sandiklar = sevkiyat.Sandiklar
                    .OrderBy(s => GetSandikNoSortNumber(s.SandikNo))
                    .ThenBy(s => s.SandikNo)
                    .ToList();
            }

            var eskiSevkDurumleri = new[]
            {
                ((int)ProjeDurum.SevkEdildi).ToString(),
                ((int)ProjeDurum.EksikSevkEdildi).ToString()
            };

            var kilitAcmaKayitlari = _unitOfWork.GetRepository<HareketGecmisi>().Queryable()
                .Where(h => h.ProjeId == request.ProjeId
                    && ((h.ReferansTipi == "Proje"
                            && h.Islem.StartsWith("Proje Kilidi")
                            && eskiSevkDurumleri.Contains(h.EskiDeger ?? string.Empty)
                            && h.YeniDeger == ((int)ProjeDurum.Hazirlaniyor).ToString())
                        || (h.ReferansTipi == "Sandik"
                            && h.Islem.StartsWith("Sandık Kilidi"))))
                .Select(h => new SevkiyatDto
                {
                    Id = h.Id,
                    SevkiyatNo = 0,
                    SevkTarihi = h.Tarih,
                    Aciklama = h.Aciklama,
                    KullaniciAdSoyad = h.Kullanici.AdSoyad,
                    SandikSayisi = 0,
                    KayitTipi = "KilitAcma",
                    IsKilitAcma = true,
                    Sandiklar = new List<SevkiyatSandikDto>()
                })
                .ToList();

            var result = sevkiyatlar
                .Concat(kilitAcmaKayitlari)
                .OrderBy(x => x.SevkTarihi)
                .ThenBy(x => x.IsKilitAcma ? 1 : 0)
                .ToList();

            return Task.FromResult(Result<List<SevkiyatDto>>.Success(result));
        }

        private static int GetSandikNoSortNumber(string? sandikNo)
        {
            if (string.IsNullOrWhiteSpace(sandikNo))
                return int.MaxValue;

            var trimmed = sandikNo.Trim();
            var digits = new string(trimmed
                .SkipWhile(c => !char.IsDigit(c))
                .TakeWhile(char.IsDigit)
                .ToArray());

            return int.TryParse(digits, out var number) ? number : int.MaxValue;
        }
    }
}
