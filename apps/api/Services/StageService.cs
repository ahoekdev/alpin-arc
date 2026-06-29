using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class StageService(AppDbContext context) : IStageService
{
    private readonly AppDbContext _context = context;

    public async Task<ServiceResult<IReadOnlyCollection<StageResponseDto>>> GetStagesAsync(int? limit, CancellationToken cancellationToken)
    {
        if (limit is <= 0 or > 100)
        {
            return ServiceResult<IReadOnlyCollection<StageResponseDto>>.BadRequest("Limit must be between 1 and 100.");
        }

        IQueryable<Stage> query = _context.Stages
            .AsNoTracking()
            .OrderBy(stage => stage.StartLodge.Name)
            .ThenBy(stage => stage.EndLodge.Name);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        var stages = await ProjectToResponse(query).ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<StageResponseDto>>.Success(stages);
    }

    public async Task<ServiceResult<StageResponseDto>> GetStageAsync(long id, CancellationToken cancellationToken)
    {
        var stage = await ProjectToResponse(_context.Stages
            .AsNoTracking()
            .Where(stage => stage.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

        if (stage is null)
        {
            return ServiceResult<StageResponseDto>.NotFound("Stage does not exist.");
        }

        return ServiceResult<StageResponseDto>.Success(stage);
    }

    private static IQueryable<StageResponseDto> ProjectToResponse(IQueryable<Stage> query)
    {
        return query.Select(stage => new StageResponseDto(
                stage.Id,
                new LodgeSummaryDto(
                    stage.StartLodge.Id,
                    stage.StartLodge.Name,
                    stage.StartLodge.Description,
                    stage.StartLodge.CountryCode),
                new LodgeSummaryDto(
                    stage.EndLodge.Id,
                    stage.EndLodge.Name,
                    stage.EndLodge.Description,
                    stage.EndLodge.CountryCode),
                stage.DurationMinutes,
                stage.DistanceMeters,
                stage.CreatedAt));
    }
}
