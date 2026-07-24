using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeSilCommandHandler : IRequestHandler<ProjeSilCommand, Result<bool>>
    {
        private readonly IProjeSilmeService _projeSilmeService;

        public ProjeSilCommandHandler(IProjeSilmeService projeSilmeService)
        {
            _projeSilmeService = projeSilmeService;
        }

        public async Task<Result<bool>> Handle(ProjeSilCommand request, CancellationToken cancellationToken)
        {
            var silindi = await _projeSilmeService.SilAsync(request.ProjeId, cancellationToken);

            if (!silindi)
                return Result<bool>.Failure("Proje bulunamadı.");

            return Result<bool>.Success(true);
        }
    }
}
