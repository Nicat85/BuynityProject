using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nest; 
using OnlineShoppingSystem.Application.Abstracts.Repositories; 
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Events;
using OnlineSohppingSystem.Application.Features.Products.Commands;
using OnlineSohppingSystem.Application.Models.Elasticsearch;
using OnlineSohppingSystem.Domain.Enums;
using System.Net;
using System.Threading;
using System.Threading.Tasks;


using IFavoriteRepository =
    OnlineShppingSystem.Application.Abstracts.Repositories.IRepository<OnlineShppingSystem.Domain.Entities.Favorite>;

namespace OnlineSohppingSystem.Application.Features.Products.Handlers
{
    public class SoftDeleteProductCommandHandler
        : IRequestHandler<SoftDeleteProductCommand, BaseResponse<bool>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IFavoriteRepository _favoriteRepository; 
        private readonly IElasticClient _elasticClient;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPublishEndpoint _publishEndpoint;

        public SoftDeleteProductCommandHandler(
            IProductRepository productRepository,
            IFavoriteRepository favoriteRepository,     
            IElasticClient elasticClient,
            UserManager<AppUser> userManager,
            IPublishEndpoint publishEndpoint)
        {
            _productRepository = productRepository;
            _favoriteRepository = favoriteRepository;
            _elasticClient = elasticClient;
            _userManager = userManager;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<BaseResponse<bool>> Handle(
            SoftDeleteProductCommand request,
            CancellationToken cancellationToken)
        {
            
            var product = await _productRepository.GetByExpressionAsync(p =>
                p.Id == request.Id &&
                p.UserId == request.UserId &&
                p.Status != ProductStatus.Deleted);

            if (product == null)
                return BaseResponse<bool>.Fail("Product not found", HttpStatusCode.NotFound);

           
            product.IsDeleted = true;
            product.Status = ProductStatus.Deleted;
            product.DeletedAt = DateTime.UtcNow;

            await _productRepository.SaveChangesAsync();

           
            var favs = await _favoriteRepository
                .GetAll(isTracking: true)
                .Where(f => f.ProductId == product.Id)
                .ToListAsync(cancellationToken);

            foreach (var f in favs)
                _favoriteRepository.SoftDelete(f);

            await _favoriteRepository.SaveChangesAsync();

           
            await _elasticClient.DeleteAsync<ProductIndexModel>(request.Id, ct => ct, cancellationToken);

            
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());

            await _publishEndpoint.Publish(new EmailNotificationEvent(
                user.Id,
                user.Email!,
                "Məhsul silindi",
                $"Məhsulunuz <b>{product.Name}</b> {DateTime.UtcNow:dd.MM.yyyy HH:mm} tarixində silindi.<br/><br/>" +
                "30 gün ərzində bərpa edə bilərsiniz. Əks halda məhsul avtomatik silinəcək.<br/><br/>— Buynity Komandası",
                user.FullName,
                user.UserName,
                user.ProfilePicture,
                UseHtmlTemplate: true
            ), cancellationToken);

            return BaseResponse<bool>.CreateSuccess(true, "Product successfully deleted", HttpStatusCode.OK);
        }
    }
}
