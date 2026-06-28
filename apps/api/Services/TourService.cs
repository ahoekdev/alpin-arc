using Api.Data;
using Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class TourService(AppDbContext context) : ITourService
{
    private readonly AppDbContext _context = context;

    public async Task<ServiceResult<TourDetailResponseDto>> GetTourAsync(long id, CancellationToken cancellationToken)
    {
        var tour = await _context.Tours
            .AsNoTracking()
            .Where(tour => tour.Id == id)
            .Select(tour => new TourDetailResponseDto(
                tour.Id,
                tour.Name,
                tour.Description,
                tour.CreatedAt,
                tour.Variants
                    .OrderBy(variant => variant.Name)
                    .Select(variant => new TourDetailVariantDto(
                        variant.Id,
                        variant.TourId,
                        variant.Name,
                        variant.Description,
                        variant.IsPrimary,
                        variant.Stages.Sum(stage => stage.Stage.DistanceMeters),
                        variant.Stages.Sum(stage => stage.Stage.DurationMinutes),
                        variant.Stages.Count))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (tour is null)
        {
            return ServiceResult<TourDetailResponseDto>.NotFound("Tour does not exist.");
        }

        return ServiceResult<TourDetailResponseDto>.Success(tour);
    }
}
