using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nest;
using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Abstracts.Repositories;
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
using OnlineSohppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;

namespace OnlineSohppingSystem.Application.Features.Products.Handlers;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, BaseResponse<ProductResultDto>>
{
    private const string StoreSellerRole = "StoreSeller";
    private const string StorePlanCode = "store_seller_monthly";

    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IElasticClient _elasticClient;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IStoreSellerSubscriptionService _subscriptionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ICloudinaryService cloudinaryService,
        IMapper mapper,
        IPublishEndpoint publishEndpoint,
        IElasticClient elasticClient,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IStoreSellerSubscriptionService subscriptionService,
        IHttpContextAccessor httpContextAccessor)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _cloudinaryService = cloudinaryService;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
        _elasticClient = elasticClient;
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _subscriptionService = subscriptionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BaseResponse<ProductResultDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId == Guid.Empty)
            return BaseResponse<ProductResultDto>.Fail("Unauthorized", HttpStatusCode.Unauthorized);

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null)
            return BaseResponse<ProductResultDto>.Fail("Category not found", HttpStatusCode.NotFound);

        if (request.IsSecondHand && request.IsFromStore)
            return BaseResponse<ProductResultDto>.Fail(
                "IsSecondHand və IsFromStore sahələrindən yalnız biri true ola bilər",
                HttpStatusCode.BadRequest);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return BaseResponse<ProductResultDto>.Fail("User not found", HttpStatusCode.NotFound);

        if (string.IsNullOrWhiteSpace(user.Address))
            return BaseResponse<ProductResultDto>.Fail(
                "Məhsul yaratmaq üçün profilinizdə ünvan əlavə edin.",
                HttpStatusCode.BadRequest);

        
        if (request.IsSecondHand)
        {
            if (!HasPermission(Permissions.Products.CreateSecondHand))
                return BaseResponse<ProductResultDto>.Fail("İcazə yoxdur: CreateSecondHand", HttpStatusCode.Forbidden);
        }

        if (request.IsFromStore)
        {
            if (!HasPermission(Permissions.Products.CreateStore))
            {
                
                var statusResp = await _subscriptionService.GetMyStatusAsync(userId, cancellationToken);
                var nowUtc = DateTime.UtcNow;
                var skew = TimeSpan.FromMinutes(5);

                var hasActiveSub =
                    statusResp.IsSuccess &&
                    statusResp.Data is not null &&
                    statusResp.Data.Status != null &&
                    statusResp.Data.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) &&
                    statusResp.Data.CurrentPeriodEnd.HasValue &&
                    statusResp.Data.CurrentPeriodEnd.Value.Add(skew) > nowUtc;

                var hasStoreRole = await _userManager.IsInRoleAsync(user, StoreSellerRole);

                if (hasActiveSub)
                {
                   
                    if (!hasStoreRole)
                    {
                        var ensured = await EnsureRoleExistsAsync(StoreSellerRole);
                        if (!ensured)
                            return BaseResponse<ProductResultDto>.Fail(
                                "Abunəlik aktivdir, lakin StoreSeller rolu sistemdə mövcud deyil və yaradıla bilmədi.",
                                HttpStatusCode.InternalServerError);

                        var roleResult = await _userManager.AddToRoleAsync(user, StoreSellerRole);
                        if (!roleResult.Succeeded)
                            return BaseResponse<ProductResultDto>.Fail(
                                "Abunəlik aktivdir, lakin StoreSeller rolu təyin edilə bilmədi.",
                                HttpStatusCode.InternalServerError);
                    }
                }
                else if (!hasStoreRole)
                {
                    
                    var checkoutUrl = await GetCheckoutUrlAsync(userId, "/products/create", cancellationToken);
                    return PaymentRequired(
                        "Store (Yeni məhsul) əlavə etmək üçün aktiv abunəlik və ya StoreSeller rolu tələb olunur.",
                        checkoutUrl);
                }
            }
        }

        
        var product = _mapper.Map<Product>(request);
        product.UserId = userId;
        product.IsFromStore = request.IsFromStore;
        product.IsSecondHand = request.IsSecondHand;

        foreach (var formFile in request.Images ?? Enumerable.Empty<IFormFile>())
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

        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();

        
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
            CategoryName = category.Name,
            IsSecondHand = product.IsSecondHand,
            IsFromStore = product.IsFromStore,
            Condition = (int)product.Condition,
            ImageUrls = product.ProductImages.Select(x => x.Url).ToList(),
            CreatedAt = product.CreatedAt,
            SellerId = product.UserId,
            SellerName = user.FullName ?? user.UserName ?? "Unknown"
        };
        await _elasticClient.IndexDocumentAsync(indexModel, cancellationToken);

        
        var imageHtml = string.Join("", product.ProductImages.Select(img =>
            $"<img src='{img.Url}' alt='Product Image' width='200' style='margin:5px;'/>"));

        var emailBody = $@"
              <h2 style='color:#2d2d2d;'>Yeni {(product.IsSecondHand ? "" : "mağaza ")}məhsul əlavə olundu!</h2>
              <p><strong>Məhsul Adı:</strong> {product.Name}</p>
              <p><strong>Qiymət:</strong> {product.Price} AZN</p>
              {(product.OriginalPrice.HasValue ? $"<p><strong>Əvvəlki Qiymət:</strong> {product.OriginalPrice} AZN</p>" : "")}
              <p><strong>Kategoriya:</strong> {category.Name}</p>
              <p><strong>Stok:</strong> {product.StockQuantity}</p>
              <p><strong>Açıqlama:</strong> {product.Description}</p>
              <p><strong>Tarix:</strong> {product.CreatedAt:dd.MM.yyyy HH:mm}</p>
              <h4 style='margin-top:20px;'>Məhsul şəkilləri:</h4>
              <div style='display:flex; flex-wrap:wrap;'>{imageHtml}</div>
              <p style='margin-top:30px;'>Buynity platformasına məhsul əlavə etdiyiniz üçün təşəkkür edirik.</p>";

        var subject = $"Buynity - Yeni {(product.IsSecondHand ? "2-ci əl" : "mağaza")} məhsul əlavə olundu: {product.Name}";

        await _publishEndpoint.Publish(new EmailNotificationEvent(
            UserId: user.Id,
            To: user.Email!,
            Subject: subject,
            Body: emailBody,
            FullName: user.FullName,
            UserName: user.UserName,
            ProfileImageUrl: user.ProfilePicture,
            UseHtmlTemplate: true
        ), cancellationToken);

        
        var followers = await _unitOfWork.Repository<SellerFollower>()
            .GetAll()
            .Where(x => x.SellerId == userId && x.BuyerId != userId)
            .Select(x => x.Buyer)
            .ToListAsync(cancellationToken);

        foreach (var buyer in followers)
        {
            await _publishEndpoint.Publish(new NotificationEvent
            {
                UserId = buyer.Id,
                Title = "İzlədiyiniz satıcı yeni məhsul əlavə etdi!",
                Message = $"\"{product.Name}\" adlı məhsul artıq Buynity platformasında aktivdir.",
                Type = NotificationType.ProductUpdate,
                Link = $"/products/{product.Id}",
                Severity = NotificationSeverity.Low,
                Source = "product-service",
                CreatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);

            if (!string.IsNullOrWhiteSpace(buyer.Email))
            {
                var followerEmailBody = $@"
                      <h2>Salam {buyer.FullName ?? buyer.UserName},</h2>
                      <p>İzlədiyiniz satıcı <strong>{user.FullName ?? user.UserName}</strong> yeni məhsul əlavə etdi:</p>
                        <ul>
                          <li><strong>Ad:</strong> {product.Name}</li>
                          <li><strong>Qiymət:</strong> {product.Price} AZN</li>
                          {(product.OriginalPrice.HasValue ? $"<li><strong>Əvvəlki qiymət:</strong> {product.OriginalPrice} AZN</li>" : "")}
                          <li><strong>Kateqoriya:</strong> {category.Name}</li>
                          <li><strong>Tarix:</strong> {product.CreatedAt:dd.MM.yyyy HH:mm}</li>
                        </ul>
                      <p>Daha ətraflı məlumat üçün Buynity platformasına daxil olun.</p>";

                await _publishEndpoint.Publish(new EmailNotificationEvent(
                    UserId: buyer.Id,
                    To: buyer.Email!,
                    Subject: $"Yeni məhsul: {product.Name}",
                    Body: followerEmailBody,
                    FullName: buyer.FullName,
                    UserName: buyer.UserName,
                    ProfileImageUrl: buyer.ProfilePicture,
                    UseHtmlTemplate: true
                ), cancellationToken);
            }
        }

        var resultDto = _mapper.Map<ProductResultDto>(product);
        return BaseResponse<ProductResultDto>.CreateSuccess(resultDto, "Product created successfully", HttpStatusCode.Created);
    }

    private bool HasPermission(string permission)
    {
        var claims = _httpContextAccessor.HttpContext?.User?.Claims;
        if (claims is null) return false;
        return claims.Any(c => c.Type == Permissions.ClaimType && c.Value == permission);
    }

    private async Task<bool> EnsureRoleExistsAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
            return true;

        var create = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        return create.Succeeded;
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
