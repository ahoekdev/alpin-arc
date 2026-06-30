using System.Security.Claims;
using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/me/lodge-favorites")]
[ApiController]
[Authorize]
public sealed class MeLodgeFavoritesController(ILodgeFavoriteService lodgeFavoriteService) : ControllerBase
{
    private readonly ILodgeFavoriteService _lodgeFavoriteService = lodgeFavoriteService;

    [HttpGet(Name = "getMyLodgeFavorites")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LodgeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<LodgeSummaryDto>>> GetFavorites(CancellationToken cancellationToken)
    {
        var favorites = await _lodgeFavoriteService.GetFavoritesAsync(GetUserId(), cancellationToken);
        return Ok(favorites);
    }

    [HttpGet("{lodgeId}", Name = "getMyLodgeFavoriteState")]
    [ProducesResponseType(typeof(LodgeFavoriteStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LodgeFavoriteStateDto>> GetFavoriteState(long lodgeId, CancellationToken cancellationToken)
    {
        var result = await _lodgeFavoriteService.GetFavoriteStateAsync(GetUserId(), lodgeId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{lodgeId}", Name = "addMyLodgeFavorite")]
    [RequireAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFavorite(long lodgeId, CancellationToken cancellationToken)
    {
        var result = await _lodgeFavoriteService.AddFavoriteAsync(GetUserId(), lodgeId, cancellationToken);
        return this.ToNoContentActionResult(result);
    }

    [HttpDelete("{lodgeId}", Name = "removeMyLodgeFavorite")]
    [RequireAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFavorite(long lodgeId, CancellationToken cancellationToken)
    {
        var result = await _lodgeFavoriteService.RemoveFavoriteAsync(GetUserId(), lodgeId, cancellationToken);
        return this.ToNoContentActionResult(result);
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user is missing a name identifier claim.");
    }
}
