using MassTransit;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.Events;
using OnlineSohppingSystem.Domain.Enums;


namespace OnlineShoppingSystem.Persistence.Services;
public class ProductCleanupService : IProductCleanupService
{
    private readonly IProductRepository _productRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public ProductCleanupService(IProductRepository productRepository, IPublishEndpoint publishEndpoint)
    {
        _productRepository = productRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<int> CleanupOldSoftDeletedProductsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30);

        var oldSoftDeletedProducts = await _productRepository.GetAll(true)
            .Include(p => p.User)
            .Where(p => p.IsDeleted && p.DeletedAt != null && p.DeletedAt <= cutoffDate)
            .ToListAsync();

        foreach (var product in oldSoftDeletedProducts)
        {
            
            if (product.User != null)
            {
                await _publishEndpoint.Publish(new EmailNotificationEvent(
                    product.User.Id,
                    product.User.Email!,
                    "Məhsul tam silindi",
                    $@"
                    Məhsulunuz <b>{product.Name}</b> silinmə tarixindən 30 gün keçdiyi üçün sistemdən tam silindi.<br/><br/>
                    Artıq bu məhsulu geri qaytarmaq mümkün deyil.<br/><br/>
                    — Buynity Komandası",
                    product.User.FullName,
                    product.User.UserName,
                    product.User.ProfilePicture,
                    UseHtmlTemplate: true
                ));
            }

            _productRepository.HardDelete(product);
        }

        await _productRepository.SaveChangesAsync();
        return oldSoftDeletedProducts.Count;
    }



    public async Task HardDeleteOldSoftDeletedProductsAsync()
    {
        await CleanupOldSoftDeletedProductsAsync();
    }

    public async Task SendPreDeletionReminderAsync()
    {
        var notifyDate = DateTime.UtcNow.AddDays(-27);

        var products = await _productRepository.GetAll(true)
            .Include(p => p.User)
            .Where(p =>
                p.Status == ProductStatus.Deleted &&
                p.DeletedAt != null &&
                p.DeletedAt.Value.Date == notifyDate.Date)
            .ToListAsync();

        foreach (var product in products)
        {
            if (product.User == null)
                continue;

            await _publishEndpoint.Publish(new EmailNotificationEvent(
                product.User.Id,
                product.User.Email!,
                "Məhsul silinmək üzrədir",
                $@"
                    <b>{product.Name}</b> adlı məhsulunuzun silinməsinə <b>3 gün qaldı</b>.<br/><br/>
                    Əgər fikrinizi dəyişsəniz, məhsulu <b>{product.DeletedAt.Value.AddDays(30):dd.MM.yyyy}</b> tarixindən əvvəl bərpa edə bilərsiniz.<br/><br/>
                    Əks halda məhsul sistemdən tam silinəcək.<br/><br/>
                    — Buynity Komandası",
                product.User.FullName,
                product.User.UserName,
                product.User.ProfilePicture,
                UseHtmlTemplate: true
            ));
        }
    }
}
