using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class LodgeFavoriteService(AppDbContext dbContext) : ILodgeFavoriteService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyCollection<LodgeSummaryDto>> GetFavoritesAsync(string userId, CancellationToken cancellationToken)
    {
        return await _dbContext.LodgeFavorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .OrderBy(favorite => favorite.Lodge.Name)
            .Select(favorite => new LodgeSummaryDto(
                favorite.Lodge.Id,
                favorite.Lodge.Name,
                favorite.Lodge.Description,
                favorite.Lodge.CountryCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<LodgeFavoriteStateDto>> GetFavoriteStateAsync(string userId, long lodgeId, CancellationToken cancellationToken)
    {
        var lodgeExists = await _dbContext.Lodges
            .AsNoTracking()
            .AnyAsync(lodge => lodge.Id == lodgeId, cancellationToken);

        if (!lodgeExists)
        {
            return ServiceResult<LodgeFavoriteStateDto>.NotFound("Lodge was not found.");
        }

        var isFavorite = await _dbContext.LodgeFavorites
            .AsNoTracking()
            .AnyAsync(favorite => favorite.UserId == userId && favorite.LodgeId == lodgeId, cancellationToken);

        return ServiceResult<LodgeFavoriteStateDto>.Success(new LodgeFavoriteStateDto(lodgeId, isFavorite));
    }

    public async Task<ServiceResult> AddFavoriteAsync(string userId, long lodgeId, CancellationToken cancellationToken)
    {
        var lodgeExists = await _dbContext.Lodges
            .AsNoTracking()
            .AnyAsync(lodge => lodge.Id == lodgeId, cancellationToken);

        if (!lodgeExists)
        {
            return ServiceResult.NotFound("Lodge was not found.");
        }

        var exists = await _dbContext.LodgeFavorites
            .AnyAsync(favorite => favorite.UserId == userId && favorite.LodgeId == lodgeId, cancellationToken);

        if (!exists)
        {
            _dbContext.LodgeFavorites.Add(new LodgeFavorite
            {
                UserId = userId,
                LodgeId = lodgeId,
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveFavoriteAsync(string userId, long lodgeId, CancellationToken cancellationToken)
    {
        var favorite = await _dbContext.LodgeFavorites
            .SingleOrDefaultAsync(favorite => favorite.UserId == userId && favorite.LodgeId == lodgeId, cancellationToken);

        if (favorite is not null)
        {
            _dbContext.LodgeFavorites.Remove(favorite);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Success();
    }
}
