using CloudinaryDotNet;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nest;
using OnlineShoppingSystem.Infrastructure.Security;
using OnlineShoppingSystem.Infrastructure.Services;
using OnlineShoppingSystem.Infrastructure.SignalR;
using OnlineShoppingSystem.Persistence;
using OnlineShoppingSystem.Persistence.Consumers;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShoppingSystem.Persistence.Services;
using OnlineShoppingSystem.WebApplication.Filters;
using OnlineShoppingSystem.WebApplication.Middleware;
using OnlineShoppingSystem.WebApplication.Middlewares;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineShppingSystem.Application.Mappers;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Application.Validations.AuthValidations;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.Behaviors;
using OnlineSohppingSystem.Application.Common.SignalR;
using OnlineSohppingSystem.Application.Features.Products.Handlers;
using OnlineSohppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Application.Validations.OrderValidations;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console()
          .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day);
});


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var jwtSettings = builder.Configuration.GetSection("JWTSettings").Get<JWTSettings>()
    ?? throw new ArgumentNullException("JWTSettings section is missing.");
builder.Services.Configure<JWTSettings>(builder.Configuration.GetSection("JWTSettings"));
builder.Services.AddSingleton(jwtSettings);

var redisSettings = builder.Configuration.GetSection("RedisSettings").Get<RedisSettings>()
    ?? throw new ArgumentNullException("RedisSettings section is missing.");
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("RedisSettings"));
builder.Services.AddSingleton(redisSettings);

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuthSettings"));
builder.Services.Configure<FacebookAuthSettings>(builder.Configuration.GetSection("FacebookAuthSettings"));
builder.Services.Configure<IdentitySeedSettings>(builder.Configuration.GetSection("IdentitySeed"));


builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));


builder.Services.Configure<AdminBootstrapOptions>(builder.Configuration.GetSection("AdminBootstrap"));
builder.Services.AddSingleton<IAdminBootstrapGuard, AdminBootstrapGuard>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("BootstrapTight", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, builder.Configuration.GetSection("AdminBootstrap").GetValue<int?>("RateLimitPerMinute") ?? 5),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});


builder.Services.AddDbContext<OnlineShoppingSystemDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddPaymentInfrastructure(builder.Configuration);

builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<OnlineShoppingSystemDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppClaimsPrincipalFactory>();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
               (path.StartsWithSegments("/hub/notifications")
             || path.StartsWithSegments("/hubs/message")
             || path.StartsWithSegments("/hub/support")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.GetAll())
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim(Permissions.ClaimType, permission));
    }
});

builder.Services.AddCors(opt =>
{
    var frontend = builder.Configuration["AppSettings:FrontendUrl"];
    var client = builder.Configuration["AppSettings:ClientURL"];
    var adminApp = builder.Configuration["AppSettings:AdminAppUrl"];

    var allowed = new[] { frontend, client, adminApp }
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    opt.AddPolicy("frontend", p =>
    {
        if (allowed.Length == 0)
        {
            
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            p.WithOrigins(allowed).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IOptions<CloudinarySettings>>().Value;
    return new Cloudinary(new Account(config.CloudName, config.ApiKey, config.ApiSecret));
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));


var elasticSettings = builder.Configuration.GetSection("ElasticsearchSettings").Get<ElasticsearchSettings>()
    ?? throw new ArgumentNullException("ElasticsearchSettings missing.");
builder.Services.AddSingleton<IElasticClient>(_ =>
{
    var settings = new ConnectionSettings(new Uri(elasticSettings.Uri))
        .DefaultIndex(elasticSettings.DefaultIndex)
        .EnableDebugMode()
        .PrettyJson()
        .DisableDirectStreaming();

    return new ElasticClient(settings);
});

builder.Services.AddHangfire(config =>
    config.UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateProductCommandHandler).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));



builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<EditProfileDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<OrderCreateDtoValidator>();


builder.Services.AddHttpContextAccessor();
builder.Services.RegisterService(); 

builder.Services.AddAutoMapper(typeof(CategoryMapper), typeof(ProductMappingProfile));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmailNotificationConsumer>();
    x.AddConsumer<NotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        cfg.ReceiveEndpoint("email_notification_queue", e =>
        {
            e.ConfigureConsumer<EmailNotificationConsumer>(context);
        });

        cfg.ReceiveEndpoint("notification_queue", e =>
        {
            e.ConfigureConsumer<NotificationConsumer>(context);
        });
    });
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Online Shopping API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT format: Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddScoped<IPaymentSubscriptionService, StripeSubscriptionService>();
builder.Services.AddScoped<IStoreSellerSubscriptionService, StoreSellerSubscriptionService>();
builder.Services.AddScoped<IStoreSellerSubscriptionCleanupService, StoreSellerSubscriptionCleanupService>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var seedOpt = scope.ServiceProvider.GetRequiredService<IOptions<IdentitySeedSettings>>();
    await DataSeeder.SeedAsync(roleManager, seedOpt);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
}

app.UseHttpsRedirection();
app.UseCors("frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthorization();

RecurringJob.AddOrUpdate<IDatabaseCleanupService>(
    "daily-user-cleanup",
    service => service.CleanExpiredDataAsync(),
    Cron.Daily);

RecurringJob.AddOrUpdate<IProductCleanupService>(
    "daily-product-cleanup",
    service => service.HardDeleteOldSoftDeletedProductsAsync(),
    Cron.Daily);

RecurringJob.AddOrUpdate<IStoreSellerSubscriptionCleanupService>(
    "daily-subscription-cleanup",
    service => service.CleanExpiredSubscriptionsAsync(),
    Cron.Daily);

app.MapControllers();
app.MapHub<NotificationHub>("/hub/notifications");
app.MapHub<MessageHub>("/hubs/message");
app.MapHub<BuynityChatHub>("/hub/support");


app.Run();
