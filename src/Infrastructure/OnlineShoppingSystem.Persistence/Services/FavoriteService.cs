using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Favorite;
using System.Net;
using System.Security.Claims;

namespace OnlineShoppingSystem.Persistence.Services
{
    public sealed class FavoriteService : IFavoriteService
    {
        private readonly IRepository<Favorite> _favoriteRepo;
        private readonly IRepository<Product> _productRepo;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<FavoriteService> _logger;

        public FavoriteService(
            IRepository<Favorite> favoriteRepo,
            IRepository<Product> productRepo,
            IHttpContextAccessor http,
            ILogger<FavoriteService> logger)
        {
            _favoriteRepo = favoriteRepo;
            _productRepo = productRepo;
            _http = http;
            _logger = logger;
        }

        public async Task<BaseResponse> AddAsync(FavoriteCreateDto dto, CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return BaseResponse.Fail("İstifadəçi identifikasiyası tapılmadı.", HttpStatusCode.Unauthorized);

            var productActive = await _productRepo
                .GetAll(false)
                .AnyAsync(p => p.Id == dto.ProductId && !p.IsDeleted, ct);
            if (!productActive)
                return BaseResponse.Fail("Məhsul tapılmadı və ya silinib.", HttpStatusCode.NotFound);

            var already = await _favoriteRepo
                .GetAll(true)
                .AnyAsync(f => f.UserId == userId && f.ProductId == dto.ProductId, ct);
            if (already)
                return new BaseResponse("Bu məhsul artıq favoritdədir.", true, HttpStatusCode.OK);

            var favorite = new Favorite
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = dto.ProductId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _favoriteRepo.AddAsync(favorite);
            var saved = await _favoriteRepo.SaveChangesAsync();

            return saved
                ? new BaseResponse("Məhsul favoritə əlavə olundu.", true, HttpStatusCode.Created)
                : BaseResponse.Fail("Favoritə əlavə edilə bilmədi.", HttpStatusCode.InternalServerError);
        }

        public async Task<BaseResponse> RemoveAsync(Guid productId, CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return BaseResponse.Fail("İstifadəçi identifikasiyası tapılmadı.", HttpStatusCode.Unauthorized);

            var fav = await _favoriteRepo
                .GetAll(true)
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId, ct);

            if (fav is null)
                return BaseResponse.Fail("Bu məhsul favorit siyahınızda deyil.", HttpStatusCode.NotFound);

            _favoriteRepo.SoftDelete(fav);
            var saved = await _favoriteRepo.SaveChangesAsync();

            return saved
                ? BaseResponse.Success("Məhsul favoritdən silindi.")
                : BaseResponse.Fail("Favoritdən silinmədi.", HttpStatusCode.InternalServerError);
        }

        public async Task<BaseResponse<IReadOnlyList<FavoriteResultDto>>> GetMyAsync(CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return BaseResponse<IReadOnlyList<FavoriteResultDto>>.Fail("İstifadəçi identifikasiyası tapılmadı.", HttpStatusCode.Unauthorized);

            var q =
                from f in _favoriteRepo.GetAll(false).Where(f => f.UserId == userId)
                join p in _productRepo.GetAll(false) on f.ProductId equals p.Id
                where !p.IsDeleted
                orderby f.CreatedAt descending
                select new FavoriteResultDto
                {
                    Id = f.Id,
                    ProductId = f.ProductId,
                    CreatedAt = f.CreatedAt,
                    Product = new ProductBriefDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        IsSecondHand = p.IsSecondHand,
                        IsFromStore = p.IsFromStore,
                        MainImageUrl = p.ProductImages
                            .OrderBy(i => i.CreatedAt)
                            .Select(i => i.Url)
                            .FirstOrDefault(),
                        SellerId = p.UserId,
                        SellerName = p.User != null ? (p.User.FullName ?? p.User.UserName) : null
                    }
                };

            var list = await q.ToListAsync(ct);
            return BaseResponse<IReadOnlyList<FavoriteResultDto>>.CreateSuccess(list);
        }

      
        public async Task<BaseResponse<bool>> IsFavoritedAsync(Guid productId, CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return BaseResponse<bool>.Fail("İstifadəçi identifikasiyası tapılmadı.", HttpStatusCode.Unauthorized);

            var exists = await _favoriteRepo
                .GetAll(false)
                .AnyAsync(f => f.UserId == userId && f.ProductId == productId, ct);

            return BaseResponse<bool>.CreateSuccess(exists);
        }

        
        public async Task<BaseResponse<FavoriteResultDto>> GetFavoritedDetailAsync(Guid productId, CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return BaseResponse<FavoriteResultDto>.Fail("İstifadəçi identifikasiyası tapılmadı.", HttpStatusCode.Unauthorized);

            var fav = await _favoriteRepo
                .GetAll(false)
                .Include(f => f.Product).ThenInclude(p => p.ProductImages)
                .Include(f => f.Product).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId, ct);

            if (fav is null)
                return BaseResponse<FavoriteResultDto>.Fail("Məhsul favorit siyahınızda deyil.", HttpStatusCode.NotFound);

            var dto = new FavoriteResultDto
            {
                Id = fav.Id,
                ProductId = fav.ProductId,
                CreatedAt = fav.CreatedAt,
                Product = MapToBrief(fav.Product)
            };

            return BaseResponse<FavoriteResultDto>.CreateSuccess(dto);
        }

        public async Task<BaseResponse<int>> CountMyAsync(CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return BaseResponse<int>.Fail("İstifadəçi identifikasiyası tapılmadı.", HttpStatusCode.Unauthorized);

            var count = await _favoriteRepo
                .GetAll(isTracking: false)
                .Where(f => f.UserId == userId)
                .CountAsync(ct);

            return BaseResponse<int>.CreateSuccess(count);
        }


        private Guid GetUserId()
        {
            var userIdStr = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? _http.HttpContext?.User?.FindFirst("sub")?.Value;
            return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
        }

       
        private static ProductBriefDto MapToBrief(Product? p)
        {
            if (p == null) return new ProductBriefDto();

            var firstImgUrl = p.ProductImages
                ?.OrderBy(i => i.CreatedAt)
                ?.Select(i => i.Url)
                ?.FirstOrDefault();

            return new ProductBriefDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                IsSecondHand = p.IsSecondHand,
                IsFromStore = p.IsFromStore,
                MainImageUrl = firstImgUrl,
                SellerId = p.UserId,
                SellerName = p.User?.FullName ?? p.User?.UserName
            };
        }
    }
}
