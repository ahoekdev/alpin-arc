using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace api.Controllers
{
    [Route("api/tours")]
    [ApiController]
    public class ToursController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TourResponseDto>>> GetTours(CancellationToken cancellationToken)
        {
            return await _context.Tours
                .OrderBy(t => t.Name)
                .Select(t => new TourResponseDto(t.Id, t.Name, t.CreatedAt))
                .ToListAsync(cancellationToken);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TourDetailResponseDto>> GetTour(long id, CancellationToken cancellationToken)
        {
            var tour = await _context.Tours
                .Where(t => t.Id == id)
                .Select(t => new TourDetailResponseDto(
                    t.Id,
                    t.Name,
                    t.CreatedAt,
                    t.Variants
                        .OrderBy(v => v.Name)
                        .Select(v => new TourVariantSummaryDto(v.Id, v.TourId, v.Name))
                        .ToList()))
                .FirstOrDefaultAsync(cancellationToken);

            if (tour == null)
            {
                return NotFound();
            }

            return tour;
        }

        [HttpGet("{id}/variants")]
        public async Task<ActionResult<IEnumerable<TourVariantResponseDto>>> GetTourVariants(long id, CancellationToken cancellationToken)
        {
            var tourExists = await _context.Tours.AnyAsync(t => t.Id == id, cancellationToken);

            if (!tourExists)
            {
                return NotFound();
            }

            return await _context.TourVariants
                .Where(v => v.TourId == id)
                .OrderBy(v => v.Name)
                .Select(v => new TourVariantResponseDto(
                    v.Id,
                    new TourSummaryDto(v.Tour.Id, v.Tour.Name),
                    v.Name,
                    v.CreatedAt))
                .ToListAsync(cancellationToken);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTour(long id, TourRequestDto request, CancellationToken cancellationToken)
        {
            var tour = await _context.Tours.FindAsync([id], cancellationToken);

            if (tour == null)
            {
                return NotFound();
            }

            tour.Name = request.Name;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (TryHandleDatabaseException(exception, out var result))
            {
                return result;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<TourResponseDto>> PostTour(TourRequestDto request, CancellationToken cancellationToken)
        {
            var tour = new Tour
            {
                Name = request.Name,
            };

            _context.Tours.Add(tour);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (TryHandleDatabaseException(exception, out var result))
            {
                return result;
            }

            var response = await _context.Tours
                .Where(savedTour => savedTour.Id == tour.Id)
                .Select(savedTour => new TourResponseDto(savedTour.Id, savedTour.Name, savedTour.CreatedAt))
                .FirstAsync(cancellationToken);

            return CreatedAtAction(nameof(GetTour), new { id = tour.Id }, response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(long id, CancellationToken cancellationToken)
        {
            var tour = await _context.Tours.FindAsync([id], cancellationToken);

            if (tour == null)
            {
                return NotFound();
            }

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        private ActionResult HandleDatabaseException(PostgresException exception)
        {
            return exception.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => Conflict("A tour with the same name already exists."),
                _ => throw exception,
            };
        }

        private bool TryHandleDatabaseException(DbUpdateException exception, out ActionResult result)
        {
            if (exception.InnerException is PostgresException postgresException)
            {
                result = HandleDatabaseException(postgresException);
                return true;
            }

            result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return false;
        }
    }
}
