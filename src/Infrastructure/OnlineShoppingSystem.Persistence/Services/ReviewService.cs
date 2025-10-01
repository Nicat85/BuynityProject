using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Review;
using OnlineSohppingSystem.Application.Shared;
using OnlineSohppingSystem.Domain.Enums;
using System.Net;

namespace OnlineShoppingSystem.Persistence.Services;

public sealed class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly UserManager<AppUser> _userManager;
    private readonly IMapper _mapper;
    private readonly OnlineShoppingSystemDbContext _db;

    public ReviewService(
        IReviewRepository reviewRepo,
        IRepository<Product> productRepo,
        UserManager<AppUser> userManager,
        IMapper mapper,
        OnlineShoppingSystemDbContext db) 
    {
        _reviewRepo = reviewRepo;
        _productRepo = productRepo;
        _userManager = userManager;
        _mapper = mapper;
        _db = db; 
    }

    public async Task<BaseResponse<ReviewResultDto>> CreateAsync(Guid userId, ReviewCreateDto dto, CancellationToken ct = default)
    {
        
        var product = await _db.Set<Product>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId, ct);

        if (product is null)
            return BaseResponse<ReviewResultDto>.Fail("Product not found.", HttpStatusCode.NotFound);

        
        if (product.UserId == userId)
            return BaseResponse<ReviewResultDto>.Fail("You cannot review your own product.", HttpStatusCode.Forbidden);

       
        var allowedOrderStatuses = new[] { OrderStatus.Delivered }; 
        var buyerHasThisProduct = await _db.Set<Order>()
            .Where(o => o.BuyerId == userId && allowedOrderStatuses.Contains(o.Status))
            .SelectMany(o => o.OrderItems)
            .AnyAsync(oi => oi.ProductId == dto.ProductId, ct);

        if (!buyerHasThisProduct)
            return BaseResponse<ReviewResultDto>.Fail(
                "Only customers who purchased and received this product can leave a review.",
                HttpStatusCode.Forbidden);

        
        if (await _reviewRepo.ExistsByUserAndProductAsync(userId, dto.ProductId, ct))
            return BaseResponse<ReviewResultDto>.Fail("You have already reviewed this product.", HttpStatusCode.Conflict);

        
        var review = new Review
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        await _reviewRepo.AddAsync(review);
        await _reviewRepo.SaveChangesAsync();

        var full = await _reviewRepo.GetByIdWithUserAsync(review.Id, ct);
        var result = _mapper.Map<ReviewResultDto>(full!);

        return BaseResponse<ReviewResultDto>.CreateSuccess(result, "Review created.", HttpStatusCode.Created);
    }

    public async Task<BaseResponse<ReviewResultDto>> UpdateAsync(Guid id, Guid userId, bool isAdminLike, ReviewUpdateDto dto, CancellationToken ct = default)
    {
        var review = await _reviewRepo.GetByIdAsync(id);
        if (review is null)
            return BaseResponse<ReviewResultDto>.Fail("Review not found.", HttpStatusCode.NotFound);

        if (!isAdminLike && review.UserId != userId)
            return BaseResponse<ReviewResultDto>.Fail("You are not allowed to edit this review.", HttpStatusCode.Forbidden);

        review.Rating = dto.Rating;
        review.Comment = dto.Comment;
        review.UpdatedAt = DateTime.UtcNow;

        _reviewRepo.Update(review);
        await _reviewRepo.SaveChangesAsync();

        var full = await _reviewRepo.GetByIdWithUserAsync(review.Id, ct);
        var result = _mapper.Map<ReviewResultDto>(full!);

        return BaseResponse<ReviewResultDto>.CreateSuccess(result, "Review updated.");
    }

    public async Task<BaseResponse<bool>> DeleteAsync(Guid id, Guid userId, bool isAdminLike, CancellationToken ct = default)
    {
        var review = await _reviewRepo.GetByIdAsync(id);
        if (review is null)
            return BaseResponse<bool>.Fail("Review not found.", HttpStatusCode.NotFound);

        if (!isAdminLike && review.UserId != userId)
            return BaseResponse<bool>.Fail("You are not allowed to delete this review.", HttpStatusCode.Forbidden);

        review.IsDeleted = true;
        review.DeletedAt = DateTime.UtcNow;

        _reviewRepo.Update(review);
        await _reviewRepo.SaveChangesAsync();

        return BaseResponse<bool>.CreateSuccess(true, "Review deleted.");
    }

    public async Task<BaseResponse<ReviewResultDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var review = await _reviewRepo.GetByIdWithUserAsync(id, ct);
        if (review is null)
            return BaseResponse<ReviewResultDto>.Fail("Review not found.", HttpStatusCode.NotFound);

        var data = _mapper.Map<ReviewResultDto>(review);
        return BaseResponse<ReviewResultDto>.CreateSuccess(data, "Success.");
    }

    public async Task<BaseResponse<PagedResponse<ReviewResultDto>>> GetByProductAsync(Guid productId, int page, int pageSize, CancellationToken ct = default)
    {
        var total = await _reviewRepo.CountByProductAsync(productId, ct);
        var items = await _reviewRepo.GetByProductAsync(productId, page, pageSize, ct);
        var data = items.Select(_mapper.Map<ReviewResultDto>).ToList();

        var paged = new PagedResponse<ReviewResultDto>(data, total, page, pageSize);
        return BaseResponse<PagedResponse<ReviewResultDto>>.CreateSuccess(paged, "Success.");
    }

    public async Task<BaseResponse<List<ReviewResultDto>>> GetMyAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await _reviewRepo.GetByUserAsync(userId, ct);
        var data = list.Select(_mapper.Map<ReviewResultDto>).ToList();
        return BaseResponse<List<ReviewResultDto>>.CreateSuccess(data, "Success.");
    }

    public async Task<BaseResponse<ReviewSummaryDto>> GetSummaryAsync(Guid productId, CancellationToken ct = default)
    {
        var count = await _reviewRepo.CountByProductAsync(productId, ct);
        var avg = await _reviewRepo.GetAverageAsync(productId, ct);
        var dist = await _reviewRepo.GetDistributionAsync(productId, ct);

        var dto = new ReviewSummaryDto
        {
            ProductId = productId,
            Count = count,
            Average = Math.Round(avg, 2),
            Distribution = dist
        };

        return BaseResponse<ReviewSummaryDto>.CreateSuccess(dto, "Success.");
    }
}
