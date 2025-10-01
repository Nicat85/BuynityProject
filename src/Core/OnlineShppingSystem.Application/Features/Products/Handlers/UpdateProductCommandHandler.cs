using AutoMapper;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nest;
using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Product;
using OnlineSohppingSystem.Application.DTOs.Subscription;
using OnlineSohppingSystem.Application.Events;
using OnlineSohppingSystem.Application.Features.Products.Commands;
using OnlineSohppingSystem.Application.Models.Elasticsearch;
using OnlineSohppingSystem.Domain.Enums;
using System.Net;

namespace OnlineSohppingSystem.Application.Features.Products.Handlers;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, BaseResponse<ProductResultDto>>
{
    private const string StoreSellerRole = "StoreSeller";
    private const string StorePlanCode = "store_seller_monthly";

    private readonly IProductRepository _productRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IElasticClient _elasticClient;
    private readonly UserManager<AppUser> _userManager;
    private readonly ICurrentUser _currentUser;
    private readonly IStoreSellerSubscriptionService _subscriptionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        ICloudinaryService cloudinaryService,
        IMapper mapper,
        IPublishEndpoint publishEndpoint,
        IElasticClient elasticClient,
        UserManager<AppUser> userManager,
        ICurrentUser currentUser,
        IStoreSellerSubscriptionService subscriptionService,
        IHttpContextAccessor httpContextAccessor)
    {
        _productRepository = productRepository;
        _cloudinaryService = cloudinaryService;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
        _elasticClient = elasticClient;
        _userManager = userManager;
        _currentUser = currentUser;
        _subscriptionService = subscriptionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BaseResponse<ProductResultDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId == Guid.Empty)
            return BaseResponse<ProductResultDto>.Fail("Unauthorized", HttpStatusCode.Unauthorized);

        var product = await _productRepository
            .GetQueryable()
            .Include(p => p.ProductImages)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.UserId == userId && p.Status != ProductStatus.Deleted, cancellationToken);

        if (product == null)
            return BaseResponse<ProductResultDto>.Fail("Product not found", HttpStatusCode.NotFound);

        if (request.IsFromStore && request.IsSecondHand)
            return BaseResponse<ProductResultDto>.Fail("IsSecondHand və IsFromStore sahələrindən yalnız biri true ola bilər", HttpStatusCode.BadRequest);

        
        var willBeFromStore = request.IsFromStore || (!request.IsSecondHand && product.IsFromStore);
        if (willBeFromStore)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return BaseResponse<ProductResultDto>.Fail("User not found", HttpStatusCode.NotFound);

            var statusResp = await _subscriptionService.GetMyStatusAsync(userId, cancellationToken);
            var hasActiveSub = statusResp.IsSuccess
                               && statusResp.Data is not null
                               && string.Equals(statusResp.Data.Status, "Active", StringComparison.OrdinalIgnoreCase)
                               && statusResp.Data.CurrentPeriodEnd.HasValue
                               && statusResp.Data.CurrentPeriodEnd.Value > DateTime.UtcNow;

            var hasStoreRole = await _userManager.IsInRoleAsync(user, StoreSellerRole);

            if (!hasActiveSub || !hasStoreRole)
            {
                var checkoutUrl = await GetCheckoutUrlAsync(userId, $"/products/{product.Id}/edit", cancellationToken);
                return PaymentRequired("Store (Yeni məhsul) üçün aktiv abunəlik və StoreSeller rolu tələb olunur.", checkoutUrl);
            }
        }

        
        var remainingImages = product.ProductImages
            .Where(img => request.DeleteImageIds == null || !request.DeleteImageIds.Contains(img.Id))
            .ToList();

        var totalAfterUpdate = remainingImages.Count + (request.Images?.Count ?? 0);
        if (totalAfterUpdate < 3)
            return BaseResponse<ProductResultDto>.Fail("Ən azı 3 şəkil qalmalıdır (yeni + mövcud - silinən)", HttpStatusCode.BadRequest);

        
        if (request.DeleteImageIds is { Count: > 0 })
        {
            var imagesToDelete = product.ProductImages
                .Where(x => request.DeleteImageIds.Contains(x.Id))
                .ToList();

            foreach (var img in imagesToDelete)
            {
                await _cloudinaryService.DeleteImageAsync(img.PublicId);
                product.ProductImages.Remove(img);
            }
        }


        if (request.Images is { Count: > 0 })
        {
            foreach (var formFile in request.Images)
            {
                var upload = await _cloudinaryService.UploadImageAsync(formFile);
                if (!string.IsNullOrWhiteSpace(upload.Url))
                {
                    product.ProductImages.Add(new ProductImage
                    {
                        Url = upload.Url,
                        PublicId = upload.PublicId
                    });
                }
            }
        }

        _mapper.Map(request, product);
        product.IsFromStore = !request.IsSecondHand;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.SaveChangesAsync();

        var user2 = await _userManager.FindByIdAsync(userId.ToString());
        if (user2 is null)
            return BaseResponse<ProductResultDto>.Fail("User not found", HttpStatusCode.NotFound);

        var indexModel = new ProductIndexModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            OriginalPrice = product.OriginalPrice,
            StockQuantity = product.StockQuantity,
            Status = (int)product.Status,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            IsSecondHand = product.IsSecondHand,
            IsFromStore = product.IsFromStore,
            Condition = (int)product.Condition,
            ImageUrls = product.ProductImages.Select(x => x.Url).ToList(),
            CreatedAt = product.CreatedAt,
            SellerName = user2.FullName ?? user2.UserName ?? "Unknown"
        };
        await _elasticClient.IndexDocumentAsync(indexModel, cancellationToken);

        var imageHtml = string.Join("", product.ProductImages.Select(img =>
            $"<img src='{img.Url}' alt='Product Image' width='200' style='margin:5px;'/>"));

        var emailBody = $@"
        <h2 style='color:#2d2d2d;'>Məhsul məlumatları yeniləndi</h2>
        <p><strong>Məhsul Adı:</strong> {product.Name}</p>
        <p><strong>Yeni Qiymət:</strong> {product.Price} AZN</p>
        {(product.OriginalPrice.HasValue ? $"<p><strong>Əvvəlki Qiymət:</strong> {product.OriginalPrice} AZN</p>" : "")}
        <p><strong>Kategoriya:</strong> {product.Category?.Name}</p>
        <p><strong>Stok:</strong> {product.StockQuantity}</p>
        <p><strong>Açıqlama:</strong> {product.Description}</p>
        <p><strong>Yenilənmə Tarixi:</strong> {product.UpdatedAt:dd.MM.yyyy HH:mm}</p>
        <h4 style='margin-top:20px;'>Məhsul şəkilləri:</h4>
        <div style='display:flex; flex-wrap:wrap;'>{imageHtml}</div>
        <p style='margin-top:30px;'>Buynity-də məhsul məlumatlarını uğurla yenilədiniz.</p>";

        await _publishEndpoint.Publish(new EmailNotificationEvent(
            user2.Id,
            user2.Email!,
            $"Buynity - Məhsul yeniləndi: {product.Name}",
            emailBody,
            user2.FullName,
            user2.UserName,
            user2.ProfilePicture,
            UseHtmlTemplate: true
        ), cancellationToken);

        var resultDto = _mapper.Map<ProductResultDto>(product);
        return BaseResponse<ProductResultDto>.CreateSuccess(resultDto, "Product updated successfully", HttpStatusCode.OK);
    }

    private bool HasPermission(string permission)
    {
        var claims = _httpContextAccessor.HttpContext?.User?.Claims;
        if (claims is null) return false;
        return claims.Any(c => c.Type == Permissions.ClaimType && c.Value == permission);
    }

    private async Task<string> GetCheckoutUrlAsync(Guid userId, string returnToPath, CancellationToken ct)
    {
        var req = _httpContextAccessor.HttpContext?.Request;
        var origin = req?.Headers["Origin"].ToString();
        if (string.IsNullOrWhiteSpace(origin))
            origin = $"{req?.Scheme}://{req?.Host.Value}";

        var success = $"{origin}/billing/success?plan={StorePlanCode}&return={Uri.EscapeDataString(returnToPath)}";
        var cancel = $"{origin}/billing/cancel";

        var resp = await _subscriptionService.StartAsync(userId, new StartStoreSellerSubscriptionRequest
        {
            PlanCode = StorePlanCode,
            SuccessUrl = success,
            CancelUrl = cancel
        }, ct);

        if (!resp.IsSuccess || resp.Data is null || string.IsNullOrWhiteSpace(resp.Data.CheckoutUrl))
            return $"{origin}/billing/cancel"; 

        return resp.Data.CheckoutUrl;
    }

    private static BaseResponse<ProductResultDto> PaymentRequired(string message, string checkoutUrl)
        => BaseResponse<ProductResultDto>.Fail($"{message}  CHECKOUT_URL={checkoutUrl}", HttpStatusCode.PaymentRequired);
}
