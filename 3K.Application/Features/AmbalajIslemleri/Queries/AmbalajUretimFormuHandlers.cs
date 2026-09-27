using MediatR;
using _3K.Application.Common;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Queries
{
    public sealed class GetAmbalajUretimFormuDosyasiQueryHandler
        : IRequestHandler<GetAmbalajUretimFormuDosyasiQuery, Result<DosyaIcerigi>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAmbalajRaporDosyaService _dosyaService;
        private readonly IRolService _roles;
        private readonly ICurrentUserService _user;

        public GetAmbalajUretimFormuDosyasiQueryHandler(
            IUnitOfWork unitOfWork,
            IAmbalajRaporDosyaService dosyaService,
            IRolService roles,
            ICurrentUserService user)
        {
            _unitOfWork = unitOfWork;
            _dosyaService = dosyaService;
            _roles = roles;
            _user = user;
        }

        public async Task<Result<DosyaIcerigi>> Handle(
            GetAmbalajUretimFormuDosyasiQuery request,
            CancellationToken cancellationToken)
        {
            var form = AmbalajYasamDongusuYardimcisi.KayitliForm(
                _unitOfWork, request.ProjeId, request.Tur, request.BagimsizKayitMi);
            if (form == null)
                return Result<DosyaIcerigi>.Failure(
                    "Kaydedilmiş üretim formu bulunamadı. Önce Üretim Formu Oluştur işlemini kullanınız.", 404);
            AmbalajYasamDongusuYardimcisi.Maskele(
                form, await AmbalajYetkilendirmeYardimcisi.GorunumYetkileriniGetirAsync(_roles, _user, cancellationToken));

            var guvenliProjeNo = string.Concat((form.ProjeNo.Length == 0 ? "bagimsiz" : form.ProjeNo)
                .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character));
            var zaman = TurkeyTime.Now.ToString("yyyyMMdd-HHmmss");
            return Result<DosyaIcerigi>.Success(new DosyaIcerigi(
                _dosyaService.UretimFormuPdfOlustur(form),
                "application/pdf",
                $"ambalaj-uretim-formu-{guvenliProjeNo}-{zaman}.pdf"));
        }
    }
}
