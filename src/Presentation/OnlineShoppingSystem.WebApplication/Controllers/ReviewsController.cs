using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Shared;                    
using OnlineShppingSystem.Application.Shared.Settings;          
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Review;
using OnlineSohppingSystem.Application.Shared;
using System.Security.Claims;

namespace OnlineShppingSystem.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService) => _reviewService = reviewService;

    private Guid TryGetUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    
    private bool HasFullAccess()
        => User.HasClaim("Permission", Permissions.Review.FullAccess);


    [HttpPost]
    [Authorize(Policy = Permissions.Review.Create)]
    public async Task<ActionResult<BaseResponse<ReviewResultDto>>> Create([FromBody] ReviewCreateDto dto, CancellationToken ct)
    {
        var userId = TryGetUserId();
        if (userId == Guid.Empty) return Unauthorized(BaseResponse<ReviewResultDto>.Fail("Unauthorized"));
        var res = await _reviewService.CreateAsync(userId, dto, ct);
        return StatusCode((int)res.StatusCode, res);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.Review.Update)]
    public async Task<ActionResult<BaseResponse<ReviewResultDto>>> Update([FromRoute] Guid id, [FromBody] ReviewUpdateDto dto, CancellationToken ct)
    {
        var userId = TryGetUserId();
        if (userId == Guid.Empty) return Unauthorized(BaseResponse<ReviewResultDto>.Fail("Unauthorized"));
        var res = await _reviewService.UpdateAsync(id, userId, HasFullAccess(), dto, ct);
        return StatusCode((int)res.StatusCode, res);
    }


    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.Review.Delete)]
    public async Task<ActionResult<BaseResponse<bool>>> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var userId = TryGetUserId();
        if (userId == Guid.Empty) return Unauthorized(BaseResponse<bool>.Fail("Unauthorized"));
        var res = await _reviewService.DeleteAsync(id, userId, HasFullAccess(), ct);
        return StatusCode((int)res.StatusCode, res);
    }


    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<ReviewResultDto>>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var res = await _reviewService.GetByIdAsync(id, ct);
        return StatusCode((int)res.StatusCode, res);
    }


    [HttpGet("product/{productId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<PagedResponse<ReviewResultDto>>>> GetByProduct(
        [FromRoute] Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var res = await _reviewService.GetByProductAsync(productId, page, pageSize, ct);
        return StatusCode((int)res.StatusCode, res);
    }


    [HttpGet("my")]
    [Authorize(Policy = Permissions.Review.ReadMy)]
    public async Task<ActionResult<BaseResponse<List<ReviewResultDto>>>> GetMy(CancellationToken ct)
    {
        var userId = TryGetUserId();
        if (userId == Guid.Empty) return Unauthorized(BaseResponse<List<ReviewResultDto>>.Fail("Unauthorized"));
        var res = await _reviewService.GetMyAsync(userId, ct);
        return StatusCode((int)res.StatusCode, res);
    }


    [HttpGet("summary/{productId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<BaseResponse<ReviewSummaryDto>>> GetSummary([FromRoute] Guid productId, CancellationToken ct)
    {
        var res = await _reviewService.GetSummaryAsync(productId, ct);
        return StatusCode((int)res.StatusCode, res);
    }
}
