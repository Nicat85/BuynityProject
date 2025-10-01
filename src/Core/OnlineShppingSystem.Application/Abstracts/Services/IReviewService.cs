using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Review;
using OnlineSohppingSystem.Application.Shared;

namespace OnlineSohppingSystem.Application.Abstracts.Services;

public interface IReviewService
{
    Task<BaseResponse<ReviewResultDto>> CreateAsync(Guid userId, ReviewCreateDto dto, CancellationToken ct = default);
    Task<BaseResponse<ReviewResultDto>> UpdateAsync(Guid id, Guid userId, bool isAdminLike, ReviewUpdateDto dto, CancellationToken ct = default);
    Task<BaseResponse<bool>> DeleteAsync(Guid id, Guid userId, bool isAdminLike, CancellationToken ct = default);

    Task<BaseResponse<ReviewResultDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BaseResponse<PagedResponse<ReviewResultDto>>> GetByProductAsync(Guid productId, int page, int pageSize, CancellationToken ct = default);
    Task<BaseResponse<List<ReviewResultDto>>> GetMyAsync(Guid userId, CancellationToken ct = default);
    Task<BaseResponse<ReviewSummaryDto>> GetSummaryAsync(Guid productId, CancellationToken ct = default);
}