using System.Text.Json;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Enums;

using Microsoft.Extensions.Caching.Memory;

namespace _3K.Application.Behaviors
{
    public class ApprovalBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISseNotifier _sseNotifier;
        private readonly IMemoryCache _cache;

        public ApprovalBehavior(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ISseNotifier sseNotifier, IMemoryCache cache)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _sseNotifier = sseNotifier;
            _cache = cache;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is IRequireApproval approvalReq)
            {
                var karsilamaTipi = approvalReq.GetApprovalKarsilamaTipi();
                var cacheKey = $"ApprovalRule_UcK_{karsilamaTipi}";

                var isRuleActive = await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                    
                    var lookupRepo = _unitOfWork.GetRepository<LookupUcKDurum>();
                    var lookupList = await lookupRepo.FindAsync(l => l.Deger == karsilamaTipi);
                    var lookup = lookupList.FirstOrDefault();
                    
                    if (lookup == null) return false;

                    var ruleRepo = _unitOfWork.GetRepository<IslemOnayKurali>();
                    var ruleList = await ruleRepo.FindAsync(r => r.LookupUcKDurumId == lookup.Id);
                    var kural = ruleList.FirstOrDefault();
                    
                    return kural?.OnayGerektirirMi ?? false;
                });

                if (isRuleActive)
                {
                    var role = _currentUserService.Roles.FirstOrDefault() ?? "";
                    
                    // If Admin or specifically authorized role -> bypass approval
                    if (role == StatusConstants.KullaniciRol.Admin || role == StatusConstants.KullaniciRol.Yonetici)
                    {
                        return await next();
                    }

                    // Instead of processing, create OnayBekleyenIslem
                    var reqType = typeof(TRequest);
                    var islemAdi = approvalReq.GetApprovalDescription();

                    var jsonPayload = JsonSerializer.Serialize((object)request); // Need cast to object for polymorphic serialization if needed, though TRequest works.

                    var onay = new OnayBekleyenIslem
                    {
                        CommandType = reqType.AssemblyQualifiedName ?? reqType.FullName!,
                        PayloadJson = jsonPayload,
                        IslemAciklamasi = islemAdi,
                        TalepEdenKullaniciId = _currentUserService.UserId ?? 0,
                        Durum = OnayDurumu.Bekliyor
                    };

                    var repo = _unitOfWork.GetRepository<OnayBekleyenIslem>();
                    await repo.AddAsync(onay);
                    await _unitOfWork.SaveChangesAsync();

                    // Trigger SSE notification
                    await _sseNotifier.BroadcastApprovalUpdateAsync();

                    // Generate short-circuit Result<T> or Result
                    var resType = typeof(TResponse);

                    // If it's a generic Result<TValue>
                    if (resType.IsGenericType && resType.GetGenericTypeDefinition() == typeof(Result<>))
                    {
                        var valueType = resType.GetGenericArguments()[0];
                        var genericSuccess = resType.GetMethod("Success", new[] { valueType, typeof(int) });
                        if (genericSuccess != null)
                        {
                            var resultObj = genericSuccess.Invoke(null, new object?[] { null, StatusConstants.ActionQueuedForApproval });
                            return (TResponse)resultObj!;
                        }
                    }
                    else if (resType == typeof(Result))
                    {
                        return (TResponse)(object)Result.Success(StatusConstants.ActionQueuedForApproval);
                    }

                    throw new InvalidOperationException("ApprovalBehavior sadece Result veya Result<T> dönen command'lerle çalışır.");
                }
            }

            return await next();
        }

        private string GetIslemAciklamasi(string commandName)
        {
            if (commandName.Contains("UcKDurumGuncelle")) return "3K Modülü Ürün Karşılama (Kritik İşlem İşareti)";
            if (commandName.Contains("Stok")) return "Stok Operasyonu";
            return "Kritik Kullanıcı İşlemi";
        }
    }
}
