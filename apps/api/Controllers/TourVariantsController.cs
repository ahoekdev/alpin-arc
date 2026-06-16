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
    public class TourVariantsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TourVariantsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TourVariantResponseDto>> GetTourVariant(long id, CancellationToken cancellationToken)
        {
            var tourVariant = await TourVariantResponseQuery()
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

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

            return await TourVariantStageResponseQuery()
                .Where(s => s.TourVariant.Id == id)
                .OrderBy(s => s.Order)
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

            var response = await TourVariantResponseQuery()
                .FirstAsync(savedTourVariant => savedTourVariant.Id == tourVariant.Id, cancellationToken);

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

        private IQueryable<TourVariantResponseDto> TourVariantResponseQuery()
        {
            return _context.TourVariants.Select(tourVariant => new TourVariantResponseDto(
                tourVariant.Id,
                new TourSummaryDto(tourVariant.Tour.Id, tourVariant.Tour.Name),
                tourVariant.Name,
                tourVariant.CreatedAt));
        }

        private IQueryable<TourVariantStageResponseDto> TourVariantStageResponseQuery()
        {
            return _context.TourVariantStages.Select(tourVariantStage => new TourVariantStageResponseDto(
                tourVariantStage.Id,
                new TourVariantSummaryDto(
                    tourVariantStage.TourVariant.Id,
                    tourVariantStage.TourVariant.TourId,
                    tourVariantStage.TourVariant.Name),
                new StageResponseDto(
                    tourVariantStage.Stage.Id,
                    new LodgeSummaryDto(tourVariantStage.Stage.StartLodge.Id, tourVariantStage.Stage.StartLodge.Name),
                    new LodgeSummaryDto(tourVariantStage.Stage.EndLodge.Id, tourVariantStage.Stage.EndLodge.Name),
                    tourVariantStage.Stage.DurationMinutes,
                    tourVariantStage.Stage.DistanceMeters,
                    tourVariantStage.Stage.CreatedAt),
                tourVariantStage.Order,
                tourVariantStage.CreatedAt));
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
