using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries
{
    public sealed class FinansQueryHandlers :
        IRequestHandler<FinansDashboardQuery, Result<FinansDashboardDto>>,
        IRequestHandler<FinansGelirOzetiQuery, Result<FinansHassasOzetDto>>,
        IRequestHandler<FinansDurumTutarOzetiQuery, Result<FinansDurumTutarOzetiDto>>,
        IRequestHandler<FinansGiderOzetiQuery, Result<FinansHassasOzetDto>>,
        IRequestHandler<FinansNetOzetiQuery, Result<FinansHassasOzetDto>>,
        IRequestHandler<FinansProjelerQuery, Result<FinansSayfaliSonuc<FinansProjeOzetModel>>>,
        IRequestHandler<FinansProjeSecenekleriQuery, Result<FinansSayfaliSonuc<FinansProjeSecenekModel>>>,
        IRequestHandler<FinansIsKayitlariQuery, Result<FinansSayfaliSonuc<FinansIsKaydiModel>>>,
        IRequestHandler<FinansIsKayitlariSecimQuery, Result<IReadOnlyList<FinansIsKaydiModel>>>,
        IRequestHandler<FinansIsKaydiGetirQuery, Result<FinansIsKaydiModel>>,
        IRequestHandler<FinansOzelIslerQuery, Result<FinansSayfaliSonuc<FinansOzelIsModel>>>,
        IRequestHandler<FinansSiparislerQuery, Result<FinansSayfaliSonuc<FinansSiparisModel>>>,
        IRequestHandler<FinansSiparisGetirQuery, Result<FinansSiparisModel>>,
        IRequestHandler<FinansSiparisOperasyonQuery, Result<FinansSayfaliSonuc<FinansSiparisModel>>>,
        IRequestHandler<FinansFaturalamaSiparisleriQuery, Result<FinansSayfaliSonuc<FinansSiparisModel>>>,
        IRequestHandler<FinansFaturalarQuery, Result<FinansSayfaliSonuc<FinansFaturaModel>>>,
        IRequestHandler<FinansFaturaGetirQuery, Result<FinansFaturaModel>>,
        IRequestHandler<FinansFaturaOperasyonQuery, Result<FinansSayfaliSonuc<FinansFaturaModel>>>,
        IRequestHandler<FinansFaturaOperasyonDetayQuery, Result<FinansFaturaModel>>,
        IRequestHandler<FinansDuzenliIslerQuery, Result<FinansSayfaliSonuc<FinansDuzenliIsModel>>>,
        IRequestHandler<FinansAylikIslerQuery, Result<FinansAylikSayfaliSonuc>>,
        IRequestHandler<FinansAylikOperasyonIslerQuery, Result<FinansSayfaliSonuc<FinansIsKaydiModel>>>,
        IRequestHandler<FinansGiderlerQuery, Result<FinansSayfaliSonuc<FinansGiderModel>>>,
        IRequestHandler<FinansGiderKategorileriQuery, Result<IReadOnlyList<FinansGiderKategoriModel>>>,
        IRequestHandler<FinansGiderKalemleriQuery, Result<IReadOnlyList<FinansGiderKalemiModel>>>,
        IRequestHandler<FinansGiderKutuphaneKategorileriQuery, Result<IReadOnlyList<FinansGiderKategoriModel>>>,
        IRequestHandler<FinansGiderKutuphaneKalemleriQuery, Result<IReadOnlyList<FinansGiderKalemiModel>>>,
        IRequestHandler<FinansUrunlerQuery, Result<FinansSayfaliSonuc<FinansUrunModel>>>,
        IRequestHandler<FinansUrunSecenekleriQuery, Result<IReadOnlyList<FinansUrunSecenekModel>>>,
        IRequestHandler<FinansUrunKutuphaneQuery, Result<FinansSayfaliSonuc<FinansUrunModel>>>,
        IRequestHandler<FinansFiyatTarifeleriQuery, Result<FinansSayfaliSonuc<FinansFiyatTarifesiModel>>>,
        IRequestHandler<FinansRaporVerisiQuery, Result<FinansRaporModel>>,
        IRequestHandler<FinansDegisiklikGecmisiQuery, Result<FinansSayfaliSonuc<FinansDegisiklikModel>>>
    {
        private readonly IFinansService _service;

        public FinansQueryHandlers(IFinansService service) => _service = service;

        public async Task<Result<FinansDashboardDto>> Handle(FinansDashboardQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.DashboardAsync(request.Baslangic, request.Bitis, cancellationToken);
            return Result<FinansDashboardDto>.Success(new FinansDashboardDto(
                value.ToplamIs, value.ToplamSandik, value.ToplamM3, value.SiparisBekleyen,
                value.SiparisAcik, value.KismiSiparis, value.FaturaBekleyen, value.Faturalanan,
                value.BuAyOzelIs, value.BuAyGider));
        }

        public async Task<Result<FinansHassasOzetDto>> Handle(FinansGelirOzetiQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.DashboardGelirAsync(request.Baslangic, request.Bitis, cancellationToken);
            return Result<FinansHassasOzetDto>.Success(new FinansHassasOzetDto(value.Gelirler));
        }

        public async Task<Result<FinansDurumTutarOzetiDto>> Handle(FinansDurumTutarOzetiQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.DashboardDurumTutarlariAsync(request.Baslangic, request.Bitis, cancellationToken);
            return Result<FinansDurumTutarOzetiDto>.Success(new FinansDurumTutarOzetiDto(
                value.SiparisBekleyenTutarlar,
                value.SiparisAcikTutarlar,
                value.FaturalananTutarlar));
        }

        public async Task<Result<FinansHassasOzetDto>> Handle(FinansGiderOzetiQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.DashboardGiderAsync(request.Baslangic, request.Bitis, cancellationToken);
            return Result<FinansHassasOzetDto>.Success(new FinansHassasOzetDto(value.Giderler));
        }

        public async Task<Result<FinansHassasOzetDto>> Handle(FinansNetOzetiQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.DashboardNetAsync(request.Baslangic, request.Bitis, cancellationToken);
            return Result<FinansHassasOzetDto>.Success(new FinansHassasOzetDto(value.Netler));
        }

        public async Task<Result<FinansSayfaliSonuc<FinansProjeOzetModel>>> Handle(FinansProjelerQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansProjeOzetModel>>.Success(await _service.ProjelerAsync(request.Filtre, cancellationToken));

        public async Task<Result<FinansSayfaliSonuc<FinansProjeSecenekModel>>> Handle(FinansProjeSecenekleriQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansProjeSecenekModel>>.Success(
                await _service.ProjeSecenekleriAsync(request.Arama, request.PageNumber, request.PageSize, cancellationToken));

        public async Task<Result<FinansSayfaliSonuc<FinansIsKaydiModel>>> Handle(FinansIsKayitlariQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansIsKaydiModel>>.Success(await _service.IsKayitlariAsync(request.Filtre, cancellationToken));

        public async Task<Result<IReadOnlyList<FinansIsKaydiModel>>> Handle(FinansIsKayitlariSecimQuery request, CancellationToken cancellationToken)
            => Result<IReadOnlyList<FinansIsKaydiModel>>.Success(
                await _service.IsKayitlariSecimAsync(request.Ids, cancellationToken));

        public async Task<Result<FinansIsKaydiModel>> Handle(FinansIsKaydiGetirQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.IsKaydiGetirAsync(request.Id, cancellationToken);
            return value is null ? FinansHandlerHelper.NotFound<FinansIsKaydiModel>("Finans iş kaydı bulunamadı.") : Result<FinansIsKaydiModel>.Success(value);
        }

        public async Task<Result<FinansSayfaliSonuc<FinansOzelIsModel>>> Handle(FinansOzelIslerQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansOzelIsModel>>.Success(
                await _service.OzelIslerAsync(request.Filtre, cancellationToken));

        public async Task<Result<FinansSayfaliSonuc<FinansSiparisModel>>> Handle(FinansSiparislerQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansSiparisModel>>.Success(await _service.SiparislerAsync(request.Filtre, cancellationToken));

        public async Task<Result<FinansSiparisModel>> Handle(FinansSiparisGetirQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.SiparisGetirAsync(request.Id, cancellationToken);
            return value is null ? FinansHandlerHelper.NotFound<FinansSiparisModel>("Sipariş bulunamadı.") : Result<FinansSiparisModel>.Success(value);
        }

        public async Task<Result<FinansSayfaliSonuc<FinansSiparisModel>>> Handle(FinansSiparisOperasyonQuery request, CancellationToken cancellationToken)
        {
            var source = await _service.SiparislerAsync(request.Filtre, cancellationToken);
            return Result<FinansSayfaliSonuc<FinansSiparisModel>>.Success(new FinansSayfaliSonuc<FinansSiparisModel>
            {
                Items = source.Items.Select(FinansHassasAlanMaskeleme.Siparis).ToArray(),
                PageNumber = source.PageNumber,
                PageSize = source.PageSize,
                TotalCount = source.TotalCount
            });
        }

        public async Task<Result<FinansSayfaliSonuc<FinansSiparisModel>>> Handle(FinansFaturalamaSiparisleriQuery request, CancellationToken cancellationToken)
        {
            var source = await _service.SiparislerAsync(
                request.Filtre with { FaturalamaBekleyen = true },
                cancellationToken);
            return Result<FinansSayfaliSonuc<FinansSiparisModel>>.Success(new FinansSayfaliSonuc<FinansSiparisModel>
            {
                Items = source.Items.Select(FinansHassasAlanMaskeleme.Siparis).ToArray(),
                PageNumber = source.PageNumber,
                PageSize = source.PageSize,
                TotalCount = source.TotalCount
            });
        }

        public async Task<Result<FinansSayfaliSonuc<FinansFaturaModel>>> Handle(FinansFaturalarQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansFaturaModel>>.Success(await _service.FaturalarAsync(request.Filtre, cancellationToken));

        public async Task<Result<FinansFaturaModel>> Handle(FinansFaturaGetirQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.FaturaGetirAsync(request.Id, cancellationToken);
            return value is null ? FinansHandlerHelper.NotFound<FinansFaturaModel>("Fatura bulunamadı.") : Result<FinansFaturaModel>.Success(value);
        }

        public async Task<Result<FinansSayfaliSonuc<FinansFaturaModel>>> Handle(FinansFaturaOperasyonQuery request, CancellationToken cancellationToken)
        {
            var source = await _service.FaturalarAsync(request.Filtre, cancellationToken);
            return Result<FinansSayfaliSonuc<FinansFaturaModel>>.Success(new FinansSayfaliSonuc<FinansFaturaModel>
            {
                Items = source.Items.Select(FinansHassasAlanMaskeleme.Fatura).ToArray(),
                PageNumber = source.PageNumber,
                PageSize = source.PageSize,
                TotalCount = source.TotalCount
            });
        }

        public async Task<Result<FinansFaturaModel>> Handle(FinansFaturaOperasyonDetayQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.FaturaGetirAsync(request.Id, cancellationToken);
            return value is null
                ? FinansHandlerHelper.NotFound<FinansFaturaModel>("Fatura bulunamadı.")
                : Result<FinansFaturaModel>.Success(FinansHassasAlanMaskeleme.Fatura(value));
        }

        public async Task<Result<FinansSayfaliSonuc<FinansDuzenliIsModel>>> Handle(FinansDuzenliIslerQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansDuzenliIsModel>>.Success(
                await _service.DuzenliIslerSayfaliAsync(
                    request.SadeceAktif, request.Arama, request.PageNumber, request.PageSize, cancellationToken));

        public async Task<Result<FinansAylikSayfaliSonuc>> Handle(FinansAylikIslerQuery request, CancellationToken cancellationToken)
        {
            if (request.Yil is < 2000 or > 2100 || request.Ay is < 1 or > 12)
                return Result<FinansAylikSayfaliSonuc>.Failure("Geçerli bir yıl ve ay seçilmelidir.");
            return Result<FinansAylikSayfaliSonuc>.Success(
                await _service.AylikOzetSayfaliAsync(request.Yil, request.Ay, request.Filtre, cancellationToken));
        }

        public async Task<Result<FinansSayfaliSonuc<FinansIsKaydiModel>>> Handle(FinansAylikOperasyonIslerQuery request, CancellationToken cancellationToken)
        {
            if (request.Yil is < 2000 or > 2200 || request.Ay is < 1 or > 12)
                return Result<FinansSayfaliSonuc<FinansIsKaydiModel>>.Failure("Geçerli bir yıl ve ay seçilmelidir.");
            var start = new DateTime(request.Yil, request.Ay, 1);
            var filter = request.Filtre with { Baslangic = start, Bitis = start.AddMonths(1).AddDays(-1) };
            var source = await _service.IsKayitlariAsync(filter, cancellationToken);
            return Result<FinansSayfaliSonuc<FinansIsKaydiModel>>.Success(new FinansSayfaliSonuc<FinansIsKaydiModel>
            {
                Items = source.Items.Select(FinansHassasAlanMaskeleme.IsKaydi).ToArray(),
                PageNumber = source.PageNumber,
                PageSize = source.PageSize,
                TotalCount = source.TotalCount
            });
        }

        public async Task<Result<FinansSayfaliSonuc<FinansGiderModel>>> Handle(FinansGiderlerQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansGiderModel>>.Success(await _service.GiderlerAsync(request.Filtre, cancellationToken));

        public async Task<Result<IReadOnlyList<FinansGiderKategoriModel>>> Handle(FinansGiderKategorileriQuery request, CancellationToken cancellationToken)
            => Result<IReadOnlyList<FinansGiderKategoriModel>>.Success(await _service.GiderKategorileriAsync(request.SadeceAktif, cancellationToken));

        public async Task<Result<IReadOnlyList<FinansGiderKalemiModel>>> Handle(FinansGiderKalemleriQuery request, CancellationToken cancellationToken)
            => Result<IReadOnlyList<FinansGiderKalemiModel>>.Success(await _service.GiderKalemleriAsync(request.KategoriId, request.SadeceAktif, cancellationToken));

        public async Task<Result<IReadOnlyList<FinansGiderKategoriModel>>> Handle(FinansGiderKutuphaneKategorileriQuery request, CancellationToken cancellationToken)
            => Result<IReadOnlyList<FinansGiderKategoriModel>>.Success(await _service.GiderKategorileriAsync(request.SadeceAktif, cancellationToken));

        public async Task<Result<IReadOnlyList<FinansGiderKalemiModel>>> Handle(FinansGiderKutuphaneKalemleriQuery request, CancellationToken cancellationToken)
            => Result<IReadOnlyList<FinansGiderKalemiModel>>.Success(await _service.GiderKalemleriAsync(request.KategoriId, request.SadeceAktif, cancellationToken));

        public async Task<Result<FinansSayfaliSonuc<FinansUrunModel>>> Handle(FinansUrunlerQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansUrunModel>>.Success(
                await _service.UrunlerSayfaliAsync(
                    request.SadeceAktif, request.TarifeTarihi, request.Arama,
                    request.PageNumber, request.PageSize, cancellationToken));

        public async Task<Result<IReadOnlyList<FinansUrunSecenekModel>>> Handle(FinansUrunSecenekleriQuery request, CancellationToken cancellationToken)
            => Result<IReadOnlyList<FinansUrunSecenekModel>>.Success(
                await _service.UrunSecenekleriAsync(cancellationToken));

        public async Task<Result<FinansSayfaliSonuc<FinansUrunModel>>> Handle(FinansUrunKutuphaneQuery request, CancellationToken cancellationToken)
        {
            var source = await _service.UrunlerSayfaliAsync(
                request.SadeceAktif, request.TarifeTarihi, request.Arama,
                request.PageNumber, request.PageSize, cancellationToken);
            return Result<FinansSayfaliSonuc<FinansUrunModel>>.Success(new FinansSayfaliSonuc<FinansUrunModel>
            {
                Items = source.Items.Select(FinansHassasAlanMaskeleme.Urun).ToArray(),
                PageNumber = source.PageNumber,
                PageSize = source.PageSize,
                TotalCount = source.TotalCount
            });
        }

        public async Task<Result<FinansSayfaliSonuc<FinansFiyatTarifesiModel>>> Handle(FinansFiyatTarifeleriQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansFiyatTarifesiModel>>.Success(
                await _service.FiyatTarifeleriSayfaliAsync(
                    request.UrunId, request.Yil, request.SadeceAktif, request.Arama,
                    request.PageNumber, request.PageSize, cancellationToken));

        public async Task<Result<FinansRaporModel>> Handle(FinansRaporVerisiQuery request, CancellationToken cancellationToken)
            => Result<FinansRaporModel>.Success(await _service.RaporVerisiAsync(request.Filtre, cancellationToken));

        public async Task<Result<FinansSayfaliSonuc<FinansDegisiklikModel>>> Handle(FinansDegisiklikGecmisiQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansDegisiklikModel>>.Success(await _service.DegisiklikGecmisiAsync(request.VarlikTuru, request.VarlikId, request.PageNumber, request.PageSize, cancellationToken));
    }
}
