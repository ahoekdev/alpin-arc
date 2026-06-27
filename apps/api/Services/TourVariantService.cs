using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Services;

public class TourVariantService(AppDbContext context) : ITourVariantService
{
    private const string DuplicateTourVariantNameMessage = "A tour variant with the same name already exists for this tour.";
    private const string MissingTourMessage = "Tour does not exist.";

    private readonly AppDbContext _context = context;

    public async Task<ServiceResult<TourVariantDetailResponseDto>> GetTourVariantAsync(long id, CancellationToken cancellationToken)
    {
        var tourVariant = await _context.TourVariants
            .AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new TourVariantDetailResponseDto(
                v.Id,
                new TourSummaryDto(v.Tour.Id, v.Tour.Name),
                v.Name,
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
                            s.TourVariant.IsPrimary),
                        new StageResponseDto(
                            s.Stage.Id,
                            new LodgeSummaryDto(s.Stage.StartLodge.Id, s.Stage.StartLodge.Name),
                            new LodgeSummaryDto(s.Stage.EndLodge.Id, s.Stage.EndLodge.Name),
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

    public async Task<ServiceResult<IReadOnlyCollection<TourVariantStageResponseDto>>> GetTourVariantStagesAsync(long id, CancellationToken cancellationToken)
    {
        var tourVariantExists = await _context.TourVariants
            .AsNoTracking()
            .AnyAsync(v => v.Id == id, cancellationToken);

        if (!tourVariantExists)
        {
            return ServiceResult<IReadOnlyCollection<TourVariantStageResponseDto>>.NotFound("Tour variant does not exist.");
        }

        var stages = await _context.TourVariantStages
            .AsNoTracking()
            .Where(s => s.TourVariantId == id)
            .OrderBy(s => s.Order)
            .Select(s => new TourVariantStageResponseDto(
                s.Id,
                new TourVariantSummaryDto(
                    s.TourVariant.Id,
                    s.TourVariant.TourId,
                    s.TourVariant.Name,
                    s.TourVariant.IsPrimary),
                new StageResponseDto(
                    s.Stage.Id,
                    new LodgeSummaryDto(s.Stage.StartLodge.Id, s.Stage.StartLodge.Name),
                    new LodgeSummaryDto(s.Stage.EndLodge.Id, s.Stage.EndLodge.Name),
                    s.Stage.DurationMinutes,
                    s.Stage.DistanceMeters,
                    s.Stage.CreatedAt),
                s.Order,
                s.CreatedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<TourVariantStageResponseDto>>.Success(stages);
    }

    public async Task<ServiceResult> UpdateTourVariantAsync(long id, TourVariantRequestDto request, CancellationToken cancellationToken)
    {
        var tourVariant = await _context.TourVariants.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (tourVariant is null)
        {
            return ServiceResult.NotFound("Tour variant does not exist.");
        }

        tourVariant.TourId = request.TourId;
        tourVariant.Name = request.Name;
        tourVariant.IsPrimary = request.IsPrimary;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapTourVariantWriteException(exception, out var result))
        {
            return result;
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<TourVariantResponseDto>> CreateTourVariantAsync(TourVariantRequestDto request, CancellationToken cancellationToken)
    {
        var tourVariant = new TourVariant
        {
            TourId = request.TourId,
            Name = request.Name,
            IsPrimary = request.IsPrimary,
        };

        _context.TourVariants.Add(tourVariant);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapTourVariantCreateException(exception, out var result))
        {
            return result;
        }

        var response = await _context.TourVariants
            .AsNoTracking()
            .Where(savedTourVariant => savedTourVariant.Id == tourVariant.Id)
            .Select(savedTourVariant => new TourVariantResponseDto(
                savedTourVariant.Id,
                new TourSummaryDto(savedTourVariant.Tour.Id, savedTourVariant.Tour.Name),
                savedTourVariant.Name,
                savedTourVariant.IsPrimary,
                savedTourVariant.CreatedAt))
            .FirstAsync(cancellationToken);

        return ServiceResult<TourVariantResponseDto>.Success(response);
    }

    public async Task<ServiceResult> DeleteTourVariantAsync(long id, CancellationToken cancellationToken)
    {
        var tourVariant = await _context.TourVariants.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (tourVariant is null)
        {
            return ServiceResult.NotFound("Tour variant does not exist.");
        }

        _context.TourVariants.Remove(tourVariant);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    private static bool TryMapTourVariantWriteException(DbUpdateException exception, out ServiceResult result)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            result = postgresException.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => ServiceResult.Conflict(DuplicateTourVariantNameMessage),
                PostgresErrorCodes.ForeignKeyViolation => ServiceResult.BadRequest(MissingTourMessage),
                _ => ServiceResult.Success(),
            };

            return postgresException.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation;
        }

        result = ServiceResult.Success();
        return false;
    }

    private static bool TryMapTourVariantCreateException(DbUpdateException exception, out ServiceResult<TourVariantResponseDto> result)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            result = postgresException.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => ServiceResult<TourVariantResponseDto>.Conflict(DuplicateTourVariantNameMessage),
                PostgresErrorCodes.ForeignKeyViolation => ServiceResult<TourVariantResponseDto>.BadRequest(MissingTourMessage),
                _ => ServiceResult<TourVariantResponseDto>.BadRequest("Tour variant could not be saved."),
            };

            return postgresException.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation;
        }

        result = ServiceResult<TourVariantResponseDto>.BadRequest("Tour variant could not be saved.");
        return false;
    }
}
