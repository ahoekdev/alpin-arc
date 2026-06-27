using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Services;

public class TourVariantStageService(AppDbContext context) : ITourVariantStageService
{
    private const string DuplicateTourVariantStageOrderMessage = "A tour variant stage with the same order already exists for this tour variant.";
    private const string MissingTourVariantOrStageMessage = "Tour variant or stage does not exist.";

    private readonly AppDbContext _context = context;

    public async Task<ServiceResult> UpdateTourVariantStageAsync(long id, TourVariantStageRequestDto request, CancellationToken cancellationToken)
    {
        var tourVariantStage = await _context.TourVariantStages.FindAsync([id], cancellationToken);

        if (tourVariantStage is null)
        {
            return ServiceResult.NotFound("Tour variant stage does not exist.");
        }

        tourVariantStage.TourVariantId = request.TourVariantId;
        tourVariantStage.StageId = request.StageId;
        tourVariantStage.Order = request.Order;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapTourVariantStageWriteException(exception, out var result))
        {
            return result;
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<TourVariantStageResponseDto>> CreateTourVariantStageAsync(TourVariantStageRequestDto request, CancellationToken cancellationToken)
    {
        var tourVariantStage = new TourVariantStage
        {
            TourVariantId = request.TourVariantId,
            StageId = request.StageId,
            Order = request.Order,
        };

        _context.TourVariantStages.Add(tourVariantStage);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapTourVariantStageCreateException(exception, out var result))
        {
            return result;
        }

        var response = await _context.TourVariantStages
            .Where(savedTourVariantStage => savedTourVariantStage.Id == tourVariantStage.Id)
            .Select(savedTourVariantStage => new TourVariantStageResponseDto(
                savedTourVariantStage.Id,
                new TourVariantSummaryDto(
                    savedTourVariantStage.TourVariant.Id,
                    savedTourVariantStage.TourVariant.TourId,
                    savedTourVariantStage.TourVariant.Name,
                    savedTourVariantStage.TourVariant.IsPrimary),
                new StageResponseDto(
                    savedTourVariantStage.Stage.Id,
                    new LodgeSummaryDto(savedTourVariantStage.Stage.StartLodge.Id, savedTourVariantStage.Stage.StartLodge.Name),
                    new LodgeSummaryDto(savedTourVariantStage.Stage.EndLodge.Id, savedTourVariantStage.Stage.EndLodge.Name),
                    savedTourVariantStage.Stage.DurationMinutes,
                    savedTourVariantStage.Stage.DistanceMeters,
                    savedTourVariantStage.Stage.CreatedAt),
                savedTourVariantStage.Order,
                savedTourVariantStage.CreatedAt))
            .FirstAsync(cancellationToken);

        return ServiceResult<TourVariantStageResponseDto>.Success(response);
    }

    public async Task<ServiceResult> DeleteTourVariantStageAsync(long id, CancellationToken cancellationToken)
    {
        var tourVariantStage = await _context.TourVariantStages.FindAsync([id], cancellationToken);

        if (tourVariantStage is null)
        {
            return ServiceResult.NotFound("Tour variant stage does not exist.");
        }

        _context.TourVariantStages.Remove(tourVariantStage);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    private static bool TryMapTourVariantStageWriteException(DbUpdateException exception, out ServiceResult result)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            result = postgresException.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => ServiceResult.Conflict(DuplicateTourVariantStageOrderMessage),
                PostgresErrorCodes.ForeignKeyViolation => ServiceResult.BadRequest(MissingTourVariantOrStageMessage),
                _ => ServiceResult.Success(),
            };

            return postgresException.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation;
        }

        result = ServiceResult.Success();
        return false;
    }

    private static bool TryMapTourVariantStageCreateException(DbUpdateException exception, out ServiceResult<TourVariantStageResponseDto> result)
    {
        if (exception.InnerException is PostgresException postgresException)
        {
            result = postgresException.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => ServiceResult<TourVariantStageResponseDto>.Conflict(DuplicateTourVariantStageOrderMessage),
                PostgresErrorCodes.ForeignKeyViolation => ServiceResult<TourVariantStageResponseDto>.BadRequest(MissingTourVariantOrStageMessage),
                _ => ServiceResult<TourVariantStageResponseDto>.BadRequest("Tour variant stage could not be saved."),
            };

            return postgresException.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation;
        }

        result = ServiceResult<TourVariantStageResponseDto>.BadRequest("Tour variant stage could not be saved.");
        return false;
    }
}
