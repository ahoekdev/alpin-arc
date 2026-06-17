using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    public async Task<ServiceResult> UpdateStageAsync(long id, StageRequestDto request, CancellationToken cancellationToken)
    {
        var stage = await _context.Stages.SingleOrDefaultAsync(stage => stage.Id == id, cancellationToken);

        if (stage is null)
        {
            return ServiceResult.NotFound("Stage does not exist.");
        }

        stage.StartLodgeId = request.StartLodgeId;
        stage.EndLodgeId = request.EndLodgeId;
        stage.DurationMinutes = request.DurationMinutes;
        stage.DistanceMeters = request.DistanceMeters;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapStageWriteException(exception, out var result))
        {
            return result;
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<StageResponseDto>> CreateStageAsync(StageRequestDto request, CancellationToken cancellationToken)
    {
        var stage = new Stage
        {
            StartLodgeId = request.StartLodgeId,
            EndLodgeId = request.EndLodgeId,
            DurationMinutes = request.DurationMinutes,
            DistanceMeters = request.DistanceMeters,
        };

        _context.Stages.Add(stage);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapStageCreateException(exception, out var result))
        {
            return result;
        }

        var response = await ProjectToResponse(_context.Stages.AsNoTracking())
            .FirstAsync(savedStage => savedStage.Id == stage.Id, cancellationToken);

        return ServiceResult<StageResponseDto>.Success(response);
    }

    public async Task<ServiceResult> DeleteStageAsync(long id, CancellationToken cancellationToken)
    {
        var stage = await _context.Stages.SingleOrDefaultAsync(stage => stage.Id == id, cancellationToken);

        if (stage is null)
        {
            return ServiceResult.NotFound("Stage does not exist.");
        }

        _context.Stages.Remove(stage);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapStageDeleteException(exception, out var result))
        {
            return result;
        }

        return ServiceResult.Success();
    }

    private static IQueryable<StageResponseDto> ProjectToResponse(IQueryable<Stage> query)
    {
        return query.Select(stage => new StageResponseDto(
                stage.Id,
                new LodgeSummaryDto(stage.StartLodge.Id, stage.StartLodge.Name),
                new LodgeSummaryDto(stage.EndLodge.Id, stage.EndLodge.Name),
                stage.DurationMinutes,
                stage.DistanceMeters,
                stage.CreatedAt));
    }

    private static bool TryMapStageWriteException(DbUpdateException exception, out ServiceResult result)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            result = postgresException.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => ServiceResult.Conflict("A stage with the same start and end lodges already exists."),
                PostgresErrorCodes.ForeignKeyViolation => ServiceResult.BadRequest("Start or end lodge does not exist."),
                _ => ServiceResult.Success(),
            };

            return postgresException.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation;
        }

        result = ServiceResult.Success();
        return false;
    }

    private static bool TryMapStageCreateException(DbUpdateException exception, out ServiceResult<StageResponseDto> result)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            result = postgresException.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => ServiceResult<StageResponseDto>.Conflict("A stage with the same start and end lodges already exists."),
                PostgresErrorCodes.ForeignKeyViolation => ServiceResult<StageResponseDto>.BadRequest("Start or end lodge does not exist."),
                _ => ServiceResult<StageResponseDto>.BadRequest("Stage could not be saved."),
            };

            return postgresException.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation;
        }

        result = ServiceResult<StageResponseDto>.BadRequest("Stage could not be saved.");
        return false;
    }

    private static bool TryMapStageDeleteException(DbUpdateException exception, out ServiceResult result)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            result = postgresException.SqlState switch
            {
                PostgresErrorCodes.ForeignKeyViolation => ServiceResult.Conflict("Stage is used by a tour variant."),
                _ => ServiceResult.Success(),
            };

            return postgresException.SqlState is PostgresErrorCodes.ForeignKeyViolation;
        }

        result = ServiceResult.Success();
        return false;
    }
}
