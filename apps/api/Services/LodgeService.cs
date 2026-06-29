using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class LodgeService(AppDbContext context) : ILodgeService
{
    private readonly AppDbContext _context = context;

    public async Task<ServiceResult<IReadOnlyCollection<LodgeSummaryDto>>> GetLodgesAsync(int? limit, CancellationToken cancellationToken)
    {
        if (limit is <= 0 or > 100)
        {
            return ServiceResult<IReadOnlyCollection<LodgeSummaryDto>>.BadRequest("Limit must be between 1 and 100.");
        }

        IQueryable<Lodge> query = _context.Lodges
            .AsNoTracking()
            .OrderBy(l => l.Name);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        var lodges = await query
            .Select(l => new LodgeSummaryDto(l.Id, l.Name, l.Description, l.CountryCode))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<LodgeSummaryDto>>.Success(lodges);
    }

    public async Task<ServiceResult<LodgeDetailDto>> GetLodgeAsync(long id, CancellationToken cancellationToken)
    {
        var lodge = await _context.Lodges
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Description,
                l.CountryCode,
                l.CreatedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (lodge is null)
        {
            return ServiceResult<LodgeDetailDto>.NotFound("Lodge does not exist.");
        }

        var stages = await GetStagesForLodgeAsync(id, cancellationToken);
        var tours = await GetToursForLodgeAsync(id, cancellationToken);

        return ServiceResult<LodgeDetailDto>.Success(new LodgeDetailDto(
            lodge.Id,
            lodge.Name,
            lodge.Description,
            lodge.CountryCode,
            lodge.CreatedAt,
            stages,
            tours));
    }

    private async Task<IReadOnlyCollection<StageSummaryDto>> GetStagesForLodgeAsync(long lodgeId, CancellationToken cancellationToken)
    {
        return await _context.Stages
            .AsNoTracking()
            .Where(s => s.EndLodgeId == lodgeId || s.StartLodgeId == lodgeId)
            .OrderBy(s => s.StartLodge.Name)
            .ThenBy(s => s.EndLodge.Name)
            .Select(s => new StageSummaryDto(
                s.Id,
                new LodgeSummaryDto(
                    s.StartLodge.Id,
                    s.StartLodge.Name,
                    s.StartLodge.Description,
                    s.StartLodge.CountryCode),
                new LodgeSummaryDto(
                    s.EndLodge.Id,
                    s.EndLodge.Name,
                    s.EndLodge.Description,
                    s.EndLodge.CountryCode),
                s.DurationMinutes,
                s.DistanceMeters))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<TourSummaryDto>> GetToursForLodgeAsync(long lodgeId, CancellationToken cancellationToken)
    {
        return await _context.TourVariantStages
            .Where(tvs =>
                tvs.Stage.StartLodgeId == lodgeId ||
                tvs.Stage.EndLodgeId == lodgeId)
            .Select(tvs => new
            {
                tvs.TourVariant.Tour.Id,
                tvs.TourVariant.Tour.Name,
                tvs.TourVariant.Tour.Description,
                VariantCount = tvs.TourVariant.Tour.Variants.Count
            })
            .Distinct()
            .OrderBy(t => t.Name)
            .Select(t => new TourSummaryDto(t.Id, t.Name, t.Description, t.VariantCount))
            .ToListAsync(cancellationToken);
    }

}
