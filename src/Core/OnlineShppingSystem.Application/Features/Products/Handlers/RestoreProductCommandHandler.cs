using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Events;
using OnlineSohppingSystem.Application.Features.Products.Commands;
using OnlineSohppingSystem.Domain.Enums;
using System.Net;

namespace OnlineSohppingSystem.Application.Features.Products.Handlers;

public class RestoreProductCommandHandler : IRequestHandler<RestoreProductCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly UserManager<AppUser> _userManager;
    private readonly IPublishEndpoint _publishEndpoint;

    public RestoreProductCommandHandler(
        IProductRepository productRepository,
        UserManager<AppUser> userManager,
        IPublishEndpoint publishEndpoint)
    {
        _productRepository = productRepository;
        _userManager = userManager;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<BaseResponse<bool>> Handle(RestoreProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByExpressionAsync(p =>
            p.Id == request.Id && p.UserId == request.UserId && p.Status == ProductStatus.Deleted);

        if (product == null)
            return BaseResponse<bool>.Fail("Product not found or not deleted", HttpStatusCode.NotFound);

        product.Status = ProductStatus.Active;
        product.DeletedAt = null;

        await _productRepository.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        await _publishEndpoint.Publish(new EmailNotificationEvent(
           user.Id,
           user.Email!,
           "Məhsul bərpa olundu",
           $"Məhsulunuz <b>{product.Name}</b> uğurla bərpa olundu.<br/><br/>Əlavə düzəlişlər etmək üçün məhsulu yeniləyə bilərsiniz.<br/><br/>— Buynity Komandası",
           user.FullName,
           user.UserName,
           user.ProfilePicture, 
           true
        ));


        return BaseResponse<bool>.CreateSuccess(true, "Product successfully restored", HttpStatusCode.OK);
    }
}
