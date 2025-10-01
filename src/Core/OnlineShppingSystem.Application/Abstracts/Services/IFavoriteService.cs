using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.DTOs.Favorite;

namespace OnlineSohppingSystem.Application.Abstracts.Services
{
    public interface IFavoriteService
    {
        Task<BaseResponse> AddAsync(FavoriteCreateDto dto, CancellationToken ct = default);
        Task<BaseResponse> RemoveAsync(Guid productId, CancellationToken ct = default);
        Task<BaseResponse<IReadOnlyList<FavoriteResultDto>>> GetMyAsync(CancellationToken ct = default);
        Task<BaseResponse<bool>> IsFavoritedAsync(Guid productId, CancellationToken ct = default); 
        Task<BaseResponse<int>> CountMyAsync(CancellationToken ct = default);
        Task<BaseResponse<FavoriteResultDto>> GetFavoritedDetailAsync(Guid productId, CancellationToken ct = default);
    }
}
