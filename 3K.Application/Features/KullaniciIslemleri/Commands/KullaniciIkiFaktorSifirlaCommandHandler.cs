using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public sealed class KullaniciIkiFaktorSifirlaCommandHandler
        : IRequestHandler<KullaniciIkiFaktorSifirlaCommand, Result<bool>>
    {
        private readonly IIkiFaktorService _ikiFaktorService;

        public KullaniciIkiFaktorSifirlaCommandHandler(IIkiFaktorService ikiFaktorService)
        {
            _ikiFaktorService = ikiFaktorService;
        }

        public async Task<Result<bool>> Handle(
            KullaniciIkiFaktorSifirlaCommand request,
            CancellationToken cancellationToken)
        {
            var kullaniciVar = await _ikiFaktorService.SifirlaAsync(
                request.KullaniciId,
                cancellationToken);

            return kullaniciVar
                ? Result<bool>.Success(true)
                : Result<bool>.Failure("Kullanıcı bulunamadı.", 404);
        }
    }
}
