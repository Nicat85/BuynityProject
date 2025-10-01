using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Application.Shared;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Favorite;
using System.Net;

namespace OnlineShppingSystem.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;
        public FavoritesController(IFavoriteService favoriteService) => _favoriteService = favoriteService;

        [HttpPost]
        [Authorize(Policy = Permissions.Favorites.Create)]
        public async Task<IActionResult> Add([FromBody] FavoriteCreateDto dto, CancellationToken ct)
        {
            var resp = await _favoriteService.AddAsync(dto, ct);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpDelete("{productId:guid}")]
        [Authorize(Policy = Permissions.Favorites.Delete)]
        public async Task<IActionResult> Remove([FromRoute] Guid productId, CancellationToken ct)
        {
            var resp = await _favoriteService.RemoveAsync(productId, ct);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpGet("my")]
        [Authorize(Policy = Permissions.Favorites.Read)]
        public async Task<IActionResult> GetMy(CancellationToken ct)
        {
            BaseResponse<IReadOnlyList<FavoriteResultDto>> resp = await _favoriteService.GetMyAsync(ct);
            return StatusCode((int)resp.StatusCode, resp);
        }

        
        [HttpGet("is-favorited/{productId:guid}")]
        [Authorize(Policy = Permissions.Favorites.Read)]
        public async Task<IActionResult> IsFavorited([FromRoute] Guid productId, CancellationToken ct)
        {
            var resp = await _favoriteService.IsFavoritedAsync(productId, ct);
            if (resp.IsSuccess && resp.StatusCode == HttpStatusCode.OK) return Ok(resp);
            return StatusCode((int)resp.StatusCode, resp);
        }

        
        [HttpGet("is-favorited/detail/{productId:guid}")]
        [Authorize(Policy = Permissions.Favorites.Read)]
        public async Task<IActionResult> IsFavoritedDetail([FromRoute] Guid productId, CancellationToken ct)
        {
            var resp = await _favoriteService.GetFavoritedDetailAsync(productId, ct);
            return StatusCode((int)resp.StatusCode, resp);
        }

        [HttpGet("count")]
        [Authorize(Policy = Permissions.Favorites.Read)]
        public async Task<IActionResult> CountMy(CancellationToken ct)
        {
            var resp = await _favoriteService.CountMyAsync(ct);
            return StatusCode((int)resp.StatusCode, resp);
        }
    }
}
