using Api.Dtos;

namespace Api.Services;

public interface ILodgeFavoriteService
{
    Task<IReadOnlyCollection<LodgeSummaryDto>> GetFavoritesAsync(string userId, CancellationToken cancellationToken);

    Task<ServiceResult<LodgeFavoriteStateDto>> GetFavoriteStateAsync(string userId, long lodgeId, CancellationToken cancellationToken);

    Task<ServiceResult> AddFavoriteAsync(string userId, long lodgeId, CancellationToken cancellationToken);

    Task<ServiceResult> RemoveFavoriteAsync(string userId, long lodgeId, CancellationToken cancellationToken);
}
