using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace api.Controllers
{
    [Route("api/tour-variants")]
    [ApiController]
    public class TourVariantsController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpGet("{id}")]
        public async Task<ActionResult<TourVariantDetailResponseDto>> GetTourVariant(long id, CancellationToken cancellationToken)
        {
            var tourVariant = await _context.TourVariants
                .Where(v => v.Id == id)
                .Select(v => new TourVariantDetailResponseDto(
                    v.Id,
                    new TourSummaryDto(v.Tour.Id, v.Tour.Name),
                    v.Name,
                    v.CreatedAt,
                    v.Stages
                        .OrderBy(s => s.Order)
                        .Select(s => new TourVariantStageResponseDto(
                            s.Id,
                            new TourVariantSummaryDto(
                                s.TourVariant.Id,
                                s.TourVariant.TourId,
                                s.TourVariant.Name),
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

            if (tourVariant == null)
            {
                return NotFound();
            }

            return tourVariant;
        }

        [HttpGet("{id}/stages")]
        public async Task<ActionResult<IEnumerable<TourVariantStageResponseDto>>> GetTourVariantStages(long id, CancellationToken cancellationToken)
        {
            var tourVariantExists = await _context.TourVariants.AnyAsync(v => v.Id == id, cancellationToken);

            if (!tourVariantExists)
            {
                return NotFound();
            }

            return await _context.TourVariantStages
                .Where(s => s.TourVariantId == id)
                .OrderBy(s => s.Order)
                .Select(s => new TourVariantStageResponseDto(
                    s.Id,
                    new TourVariantSummaryDto(
                        s.TourVariant.Id,
                        s.TourVariant.TourId,
                        s.TourVariant.Name),
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
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTourVariant(long id, TourVariantRequestDto request, CancellationToken cancellationToken)
        {
            var tourVariant = await _context.TourVariants.FindAsync([id], cancellationToken);

            if (tourVariant == null)
            {
                return NotFound();
            }

            tourVariant.TourId = request.TourId;
            tourVariant.Name = request.Name;

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
        public async Task<ActionResult<TourVariantResponseDto>> PostTourVariant(TourVariantRequestDto request, CancellationToken cancellationToken)
        {
            var tourVariant = new TourVariant
            {
                TourId = request.TourId,
                Name = request.Name,
            };

            _context.TourVariants.Add(tourVariant);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (TryHandleDatabaseException(exception, out var result))
            {
                return result;
            }

            var response = await _context.TourVariants
                .Where(savedTourVariant => savedTourVariant.Id == tourVariant.Id)
                .Select(savedTourVariant => new TourVariantResponseDto(
                    savedTourVariant.Id,
                    new TourSummaryDto(savedTourVariant.Tour.Id, savedTourVariant.Tour.Name),
                    savedTourVariant.Name,
                    savedTourVariant.CreatedAt))
                .FirstAsync(cancellationToken);

            return CreatedAtAction(nameof(GetTourVariant), new { id = tourVariant.Id }, response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTourVariant(long id, CancellationToken cancellationToken)
        {
            var tourVariant = await _context.TourVariants.FindAsync([id], cancellationToken);

            if (tourVariant == null)
            {
                return NotFound();
            }

            _context.TourVariants.Remove(tourVariant);
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        private ActionResult HandleDatabaseException(PostgresException exception)
        {
            return exception.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => Conflict("A tour variant with the same name already exists for this tour."),
                PostgresErrorCodes.ForeignKeyViolation => BadRequest("Tour does not exist."),
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
