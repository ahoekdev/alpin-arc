using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Services;

public class TourService(AppDbContext context) : ITourService
{
    private const string DuplicateTourNameMessage = "A tour with the same name already exists.";

    private readonly AppDbContext _context = context;

    public async Task<ServiceResult<IReadOnlyCollection<TourResponseDto>>> GetToursAsync(int? limit, CancellationToken cancellationToken)
    {
        if (limit is <= 0 or > 100)
        {
            return ServiceResult<IReadOnlyCollection<TourResponseDto>>.BadRequest("Limit must be between 1 and 100.");
        }

        IQueryable<Tour> query = _context.Tours
            .AsNoTracking()
            .OrderBy(tour => tour.Name);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        var tours = await query
            .Select(tour => new TourResponseDto(
                tour.Id,
                tour.Name,
                tour.Description,
                tour.Variants.Count,
                tour.CreatedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<TourResponseDto>>.Success(tours);
    }

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

    public async Task<ServiceResult<IReadOnlyCollection<TourVariantResponseDto>>> GetTourVariantsAsync(long id, CancellationToken cancellationToken)
    {
        var tourExists = await _context.Tours
            .AsNoTracking()
            .AnyAsync(tour => tour.Id == id, cancellationToken);

        if (!tourExists)
        {
            return ServiceResult<IReadOnlyCollection<TourVariantResponseDto>>.NotFound("Tour does not exist.");
        }

        var variants = await _context.TourVariants
            .AsNoTracking()
            .Where(variant => variant.TourId == id)
            .OrderBy(variant => variant.Name)
            .Select(variant => new TourVariantResponseDto(
                variant.Id,
                new TourSummaryDto(variant.Tour.Id, variant.Tour.Name, variant.Tour.Description),
                variant.Name,
                variant.Description,
                variant.IsPrimary,
                variant.CreatedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<TourVariantResponseDto>>.Success(variants);
    }

    public async Task<ServiceResult> UpdateTourAsync(long id, TourRequestDto request, CancellationToken cancellationToken)
    {
        var tour = await _context.Tours.SingleOrDefaultAsync(tour => tour.Id == id, cancellationToken);

        if (tour is null)
        {
            return ServiceResult.NotFound("Tour does not exist.");
        }

        tour.Name = request.Name;
        tour.Description = request.Description;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapTourWriteException(exception, out var result))
        {
            return result;
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<TourResponseDto>> CreateTourAsync(TourRequestDto request, CancellationToken cancellationToken)
    {
        var tour = new Tour
        {
            Name = request.Name,
            Description = request.Description,
        };

        _context.Tours.Add(tour);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapTourCreateException(exception, out var result))
        {
            return result;
        }

        var response = await _context.Tours
            .AsNoTracking()
            .Where(savedTour => savedTour.Id == tour.Id)
            .Select(savedTour => new TourResponseDto(
                savedTour.Id,
                savedTour.Name,
                savedTour.Description,
                savedTour.Variants.Count,
                savedTour.CreatedAt))
            .FirstAsync(cancellationToken);

        return ServiceResult<TourResponseDto>.Success(response);
    }

    public async Task<ServiceResult> DeleteTourAsync(long id, CancellationToken cancellationToken)
    {
        var tour = await _context.Tours.SingleOrDefaultAsync(tour => tour.Id == id, cancellationToken);

        if (tour is null)
        {
            return ServiceResult.NotFound("Tour does not exist.");
        }

        _context.Tours.Remove(tour);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    private static bool TryMapTourWriteException(DbUpdateException exception, out ServiceResult result)
    {
        if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            result = ServiceResult.Conflict(DuplicateTourNameMessage);
            return true;
        }

        result = ServiceResult.Success();
        return false;
    }

    private static bool TryMapTourCreateException(DbUpdateException exception, out ServiceResult<TourResponseDto> result)
    {
        if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            result = ServiceResult<TourResponseDto>.Conflict(DuplicateTourNameMessage);
            return true;
        }

        result = ServiceResult<TourResponseDto>.BadRequest("Tour could not be saved.");
        return false;
    }
}
