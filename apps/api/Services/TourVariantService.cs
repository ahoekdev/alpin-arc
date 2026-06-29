using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class TourVariantService(AppDbContext context) : ITourVariantService
{
    private readonly AppDbContext _context = context;

    public async Task<ServiceResult<IReadOnlyCollection<TourVariantResponseDto>>> GetTourVariantsAsync(
        int? limit,
        long? lodgeId,
        CancellationToken cancellationToken)
    {
        if (limit is <= 0 or > 100)
        {
            return ServiceResult<IReadOnlyCollection<TourVariantResponseDto>>.BadRequest("Limit must be between 1 and 100.");
        }

        if (lodgeId.HasValue)
        {
            var lodgeExists = await _context.Lodges
                .AsNoTracking()
                .AnyAsync(lodge => lodge.Id == lodgeId.Value, cancellationToken);

            if (!lodgeExists)
            {
                return ServiceResult<IReadOnlyCollection<TourVariantResponseDto>>.NotFound("Lodge does not exist.");
            }
        }

        IQueryable<TourVariant> query = _context.TourVariants
            .AsNoTracking();

        if (lodgeId.HasValue)
        {
            query = query.Where(variant =>
                variant.Stages.Any(stage =>
                    stage.Stage.StartLodgeId == lodgeId.Value ||
                    stage.Stage.EndLodgeId == lodgeId.Value));
        }

        query = query
            .OrderBy(variant => variant.Tour.Name)
            .ThenBy(variant => variant.Name);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        var variants = await query
            .Select(variant => new TourVariantResponseDto(
                variant.Id,
                new TourSummaryDto(
                    variant.Tour.Id,
                    variant.Tour.Name,
                    variant.Tour.Description,
                    variant.Tour.Variants.Count),
                variant.Name,
                variant.Description,
                variant.IsPrimary,
                variant.Stages.Sum(stage => (int?)stage.Stage.DistanceMeters) ?? 0,
                variant.Stages.Sum(stage => (int?)stage.Stage.DurationMinutes) ?? 0,
                variant.Stages.Count,
                variant.CreatedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<TourVariantResponseDto>>.Success(variants);
    }

    public async Task<ServiceResult<TourVariantDetailResponseDto>> GetTourVariantAsync(long id, CancellationToken cancellationToken)
    {
        var tourVariant = await _context.TourVariants
            .AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new TourVariantDetailResponseDto(
                v.Id,
                new TourSummaryDto(v.Tour.Id, v.Tour.Name, v.Tour.Description, v.Tour.Variants.Count),
                v.Name,
                v.Description,
                v.IsPrimary,
                v.CreatedAt,
                v.Stages
                    .OrderBy(s => s.Order)
                    .Select(s => new TourVariantStageResponseDto(
                        s.Id,
                        new TourVariantSummaryDto(
                            s.TourVariant.Id,
                            s.TourVariant.TourId,
                            s.TourVariant.Name,
                            s.TourVariant.Description,
                            s.TourVariant.IsPrimary),
                        new StageResponseDto(
                            s.Stage.Id,
                            new LodgeSummaryDto(
                                s.Stage.StartLodge.Id,
                                s.Stage.StartLodge.Name,
                                s.Stage.StartLodge.Description,
                                s.Stage.StartLodge.CountryCode),
                            new LodgeSummaryDto(
                                s.Stage.EndLodge.Id,
                                s.Stage.EndLodge.Name,
                                s.Stage.EndLodge.Description,
                                s.Stage.EndLodge.CountryCode),
                            s.Stage.DurationMinutes,
                            s.Stage.DistanceMeters,
                            s.Stage.CreatedAt),
                        s.Order,
                        s.CreatedAt))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (tourVariant is null)
        {
            return ServiceResult<TourVariantDetailResponseDto>.NotFound("Tour variant does not exist.");
        }

        return ServiceResult<TourVariantDetailResponseDto>.Success(tourVariant);
    }
}
