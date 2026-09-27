using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries
{
    public sealed class FinansQueryHandlers :
        IRequestHandler<FinansProjeSecenekleriQuery, Result<FinansSayfaliSonuc<FinansProjeSecenekModel>>>,
        IRequestHandler<FinansIsKayitlariSecimQuery, Result<IReadOnlyList<FinansIsKaydiModel>>>,
        IRequestHandler<FinansIsKaydiGetirQuery, Result<FinansIsKaydiModel>>,
        IRequestHandler<FinansSiparislerQuery, Result<FinansSayfaliSonuc<FinansSiparisModel>>>,
        IRequestHandler<FinansSiparisGetirQuery, Result<FinansSiparisModel>>,
        IRequestHandler<FinansFaturalarQuery, Result<FinansSayfaliSonuc<FinansFaturaModel>>>,
        IRequestHandler<FinansFaturaGetirQuery, Result<FinansFaturaModel>>,
        IRequestHandler<FinansDuzenliIslerQuery, Result<FinansSayfaliSonuc<FinansDuzenliIsModel>>>,
        IRequestHandler<FinansAylikIslerQuery, Result<FinansAylikSayfaliSonuc>>,
        IRequestHandler<FinansGiderlerQuery, Result<FinansSayfaliSonuc<FinansGiderModel>>>,
        IRequestHandler<FinansGiderKategorileriQuery, Result<IReadOnlyList<FinansGiderKategoriModel>>>,
        IRequestHandler<FinansGiderKalemleriQuery, Result<IReadOnlyList<FinansGiderKalemiModel>>>,
        IRequestHandler<FinansGiderKutuphaneKategorileriQuery, Result<IReadOnlyList<FinansGiderKategoriModel>>>,
        IRequestHandler<FinansGiderKutuphaneKalemleriQuery, Result<IReadOnlyList<FinansGiderKalemiModel>>>,
        IRequestHandler<FinansUrunlerQuery, Result<FinansSayfaliSonuc<FinansUrunModel>>>,
        IRequestHandler<FinansDegisiklikGecmisiQuery, Result<FinansSayfaliSonuc<FinansDegisiklikModel>>>
    {
        private readonly IFinansService _service;

        public FinansQueryHandlers(IFinansService service) => _service = service;

        public async Task<Result<FinansSayfaliSonuc<FinansProjeSecenekModel>>> Handle(FinansProjeSecenekleriQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansProjeSecenekModel>>.Success(
                await _service.ProjeSecenekleriAsync(request.Arama, request.PageNumber, request.PageSize, cancellationToken));

        public async Task<Result<IReadOnlyList<FinansIsKaydiModel>>> Handle(FinansIsKayitlariSecimQuery request, CancellationToken cancellationToken)
            => Result<IReadOnlyList<FinansIsKaydiModel>>.Success(
                await _service.IsKayitlariSecimAsync(request.Ids, cancellationToken));

        public async Task<Result<FinansIsKaydiModel>> Handle(FinansIsKaydiGetirQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.IsKaydiGetirAsync(request.Id, cancellationToken);
            return value is null ? FinansHandlerHelper.NotFound<FinansIsKaydiModel>("Finans iş kaydı bulunamadı.") : Result<FinansIsKaydiModel>.Success(value);
        }

        public async Task<Result<FinansSayfaliSonuc<FinansSiparisModel>>> Handle(FinansSiparislerQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansSiparisModel>>.Success(await _service.SiparislerAsync(request.Filtre, cancellationToken));

        public async Task<Result<FinansSiparisModel>> Handle(FinansSiparisGetirQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.SiparisGetirAsync(request.Id, cancellationToken);
            return value is null ? FinansHandlerHelper.NotFound<FinansSiparisModel>("Sipariş bulunamadı.") : Result<FinansSiparisModel>.Success(value);
        }

        public async Task<Result<FinansSayfaliSonuc<FinansFaturaModel>>> Handle(FinansFaturalarQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansFaturaModel>>.Success(await _service.FaturalarAsync(request.Filtre, cancellationToken));

        public async Task<Result<FinansFaturaModel>> Handle(FinansFaturaGetirQuery request, CancellationToken cancellationToken)
        {
            var value = await _service.FaturaGetirAsync(request.Id, cancellationToken);
            return value is null ? FinansHandlerHelper.NotFound<FinansFaturaModel>("Fatura bulunamadı.") : Result<FinansFaturaModel>.Success(value);
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

        public async Task<Result<FinansSayfaliSonuc<FinansDegisiklikModel>>> Handle(FinansDegisiklikGecmisiQuery request, CancellationToken cancellationToken)
            => Result<FinansSayfaliSonuc<FinansDegisiklikModel>>.Success(await _service.DegisiklikGecmisiAsync(request.VarlikTuru, request.VarlikId, request.PageNumber, request.PageSize, cancellationToken));
    }
}
