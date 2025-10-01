using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Application.Shared;
using OnlineShppingSystem.Application.Shared.Helpers;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Domain.Entities;
using System.Net;
using System.Security.Claims;

namespace OnlineShoppingSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<SellerFollower> _followRepository;

    public FollowController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _followRepository = _unitOfWork.Repository<SellerFollower>();
    }

    [HttpPost("{sellerId}")]
    [Authorize(Policy = Permissions.Follows.Create)]
    public async Task<IActionResult> Follow(Guid sellerId)
    {
        var buyerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(buyerIdClaim))
            return Unauthorized(BaseResponse<string>.Fail("Token etibarsız və ya istifadəçi tapılmadı", HttpStatusCode.Unauthorized));

        var buyerId = Guid.Parse(buyerIdClaim);

        var exists = await _followRepository.AnyAsync(x => x.BuyerId == buyerId && x.SellerId == sellerId);
        if (exists)
            return BadRequest(BaseResponse<string>.Fail("Artıq izləyirsiniz", HttpStatusCode.BadRequest));

        var follow = new SellerFollower
        {
            BuyerId = buyerId,
            SellerId = sellerId
        };

        await _followRepository.AddAsync(follow);
        await _unitOfWork.SaveChangesAsync();

        return Ok(BaseResponse<string>.CreateSuccess("Satıcı izlənməyə başlandı"));
    }

    [HttpDelete("{sellerId}")]
    [Authorize(Policy = Permissions.Follows.Delete)]
    public async Task<IActionResult> Unfollow(Guid sellerId)
    {
        var buyerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(buyerIdClaim))
            return Unauthorized(BaseResponse<string>.Fail("Token etibarsız və ya istifadəçi tapılmadı", HttpStatusCode.Unauthorized));

        var buyerId = Guid.Parse(buyerIdClaim);

        var entity = await _followRepository.GetByExpressionAsync(x => x.BuyerId == buyerId && x.SellerId == sellerId);

        if (entity == null)
            return NotFound(BaseResponse<string>.Fail("Tapılmadı", HttpStatusCode.NotFound));

        _followRepository.HardDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return Ok(BaseResponse<string>.CreateSuccess("İzləmə dayandırıldı"));
    }
}
