using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.RolIslemleri.Commands;

public sealed class RolOlusturCommandHandler(IUnitOfWork unitOfWork, IRolService rolService,
    ICurrentUserService currentUser) : IRequestHandler<RolOlusturCommand, Result<RolDto>>
{
    public async Task<Result<RolDto>> Handle(RolOlusturCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Ad) || string.Equals(request.Ad.Trim(), "Admin", StringComparison.OrdinalIgnoreCase))
            return Result<RolDto>.Failure("Geçerli ve ayrılmış olmayan bir rol adı giriniz.");
        var template = request.SablonKodu == null ? null : RolSablonlari.Tum.FirstOrDefault(x => x.Kod == request.SablonKodu);
        if (request.SablonKodu != null && template == null)
            return Result<RolDto>.Failure("Rol şablonu bulunamadı.");
        var permissions = new List<RolYetkiItemDto>();
        if (template != null)
        {
            permissions.Add(new() { MenuTanimiId = template.ModulMenuId, YetkiTipiId = (int)YetkiTipi.R });
            permissions.AddRange(template.IzinKodlari.Select(code => YetkiKatalogu.Bul(code)!).Select(x =>
                new RolYetkiItemDto { MenuTanimiId = x.Id, YetkiTipiId = (int)x.GerekenYetki }));
        }
        var check = await YetkiAtamaKurallari.DogrulaAsync(unitOfWork, rolService, currentUser, permissions, cancellationToken);
        if (!check.IsSuccess) return Result<RolDto>.Failure(check.Error!.Message, check.StatusCode);
        var repo = unitOfWork.GetRepository<Rol>();
        if ((await repo.FindAsync(x => x.Ad.ToLower() == request.Ad.Trim().ToLower())).Any())
            return Result<RolDto>.Failure("Bu isimde bir rol zaten mevcut.");
        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var rol = new Rol { Ad = request.Ad.Trim() };
            await repo.AddAsync(rol);
            await unitOfWork.SaveChangesAsync(ct);
            await rolService.YetkileriGuncelleAsync(rol.Id, permissions.Select(x => new RolYetki
            { RolId = rol.Id, MenuTanimiId = x.MenuTanimiId, YetkiTipiId = x.YetkiTipiId }).ToList(), ct);
            await unitOfWork.GetRepository<YetkiDegisikligi>().AddAsync(new()
            {
                AktorKullaniciId = currentUser.UserId!.Value, HedefTuru = "Rol", HedefId = rol.Id,
                OncekiDeger = "null", YeniDeger = System.Text.Json.JsonSerializer.Serialize(new { rol.Ad, request.SablonKodu, Yetkiler = permissions })
            });
            await unitOfWork.SaveChangesAsync(ct);
            return Result<RolDto>.Success(new RolDto { Id = rol.Id, Ad = rol.Ad });
        }, cancellationToken);
    }
}
