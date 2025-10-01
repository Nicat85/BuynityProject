using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Subscription;

namespace OnlineSohppingSystem.Application.Abstracts.Services
{
    public interface IStoreSellerSubscriptionService
    {
        Task<BaseResponse<StartStoreSellerSubscriptionResponse>> StartAsync(
            Guid userId,
            StartStoreSellerSubscriptionRequest req,
            CancellationToken ct = default);

        Task<bool> HasActiveAsync(Guid userId, string planCode, CancellationToken ct = default);

        Task<BaseResponse<bool>> HandleWebhookAsync(string jsonPayload, IDictionary<string, string> headers, CancellationToken ct = default);

        Task<BaseResponse<MySubscriptionStatusDto>> GetMyStatusAsync(Guid userId, CancellationToken ct = default);
    }
}
