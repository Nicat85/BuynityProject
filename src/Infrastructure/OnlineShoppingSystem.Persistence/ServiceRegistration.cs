using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShoppingSystem.Infrastructure.Services;
using OnlineShoppingSystem.Persistence.Repositories;
using OnlineShoppingSystem.Persistence.Services;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.Abstracts.Repositories;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.Shared.Settings;
using Stripe;

namespace OnlineShoppingSystem.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new ConnectionSettings(new Uri(configuration["ElasticSearchSettings:Uri"]))
            .DefaultIndex("products")
            .EnableDebugMode();

        var client = new ElasticClient(settings);
        services.AddSingleton<IElasticClient>(client);
        return services;
    }

    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<StripeSettings>(config.GetSection("StripeSettings"));
        services.AddScoped<IPaymentService, StripePaymentService>();
        return services;
    }

    public static void RegisterService(this IServiceCollection services)
    {
        #region Services
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddHttpClient<IFacebookService, FacebookService>();
        services.AddScoped<IGoogleService, GoogleService>();
        services.AddScoped<IDatabaseCleanupService, DatabaseCleanupService>();
        services.AddScoped<IProfileService, UserProfileService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductCleanupService, ProductCleanupService>();
        services.AddScoped<RedisRefreshTokenService>();
        services.AddScoped<IAccessTokenService, AccessTokenService>();
        services.AddScoped<IRedisCacheService, RedisCacheService>();
        services.AddScoped<IRedisNotificationService, RedisNotificationService>();
        services.AddScoped<IRedisMessageService, RedisMessageService>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddHttpContextAccessor();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReviewService, OnlineShoppingSystem.Persistence.Services.ReviewService>();
        services.AddScoped<IStoreSellerSubscriptionService, StoreSellerSubscriptionService>();
        services.AddScoped<IStoreSellerSubscriptionCleanupService, StoreSellerSubscriptionCleanupService>();
        services.AddScoped<ISupportChatService, SupportChatService>();
        services.AddScoped<ISubscriptionStatusHandler, SubscriptionStatusHandler>();
        services.AddScoped<ICheckoutService, StripeCheckoutService>();
        services.AddScoped<ICourierService, CourierService>();
        #endregion

        #region Repositories
        services.AddScoped(typeof(OnlineShppingSystem.Application.Abstracts.Repositories.IRepository<>), typeof(Repository<>));
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        #endregion
    }
}
