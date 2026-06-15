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
        private readonly IApprovalExecutionContext _approvalExecutionContext;

        public ApprovalBehavior(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ISseNotifier sseNotifier, IMemoryCache cache, IApprovalExecutionContext approvalExecutionContext)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _sseNotifier = sseNotifier;
            _cache = cache;
            _approvalExecutionContext = approvalExecutionContext;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is IAlwaysRequireApproval alwaysApprovalReq)
            {
                if (_approvalExecutionContext.IsExecutingApprovedCommand)
                {
                    return await next();
                }

                return await QueueForApprovalAsync(request, alwaysApprovalReq.GetApprovalDescription());
            }

            if (request is IRequireApproval approvalReq)
            {
                var lookupUcKDurumId = approvalReq.GetApprovalLookupUcKDurumId();
                var cacheKey = $"ApprovalRule_UcK_{lookupUcKDurumId}";

                var isRuleActive = await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                    var ruleRepo = _unitOfWork.GetRepository<IslemOnayKurali>();
                    var ruleList = await ruleRepo.FindAsync(r => r.LookupUcKDurumId == lookupUcKDurumId);
                    var kural = ruleList.FirstOrDefault();
                    
                    return kural?.OnayGerektirirMi ?? false;
                });

                if (isRuleActive)
                {
                    if (_approvalExecutionContext.IsExecutingApprovedCommand)
                    {
                        return await next();
                    }

                    return await QueueForApprovalAsync(request, approvalReq.GetApprovalDescription());
                }
            }

            return await next();
        }

        private async Task<TResponse> QueueForApprovalAsync(TRequest request, string islemAdi)
        {
            var reqType = typeof(TRequest);
            var jsonPayload = JsonSerializer.Serialize((object)request);

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

            await _sseNotifier.BroadcastApprovalUpdateAsync();

            var resType = typeof(TResponse);

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

        private string GetIslemAciklamasi(string commandName)
        {
            if (commandName.Contains("UcKDurumGuncelle")) return "3K Modülü Ürün Karşılama (Kritik İşlem İşareti)";
            if (commandName.Contains("Stok")) return "Stok Operasyonu";
            return "Kritik Kullanıcı İşlemi";
        }
    }
}
