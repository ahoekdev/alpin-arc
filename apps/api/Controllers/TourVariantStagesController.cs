using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace api.Controllers
{
    [Route("api/tour-variant-stages")]
    [ApiController]
    public class TourVariantStagesController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTourVariantStage(long id, TourVariantStageRequestDto request, CancellationToken cancellationToken)
        {
            var tourVariantStage = await _context.TourVariantStages.FindAsync([id], cancellationToken);

            if (tourVariantStage == null)
            {
                return NotFound();
            }

            tourVariantStage.TourVariantId = request.TourVariantId;
            tourVariantStage.StageId = request.StageId;
            tourVariantStage.Order = request.Order;

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
        public async Task<ActionResult<TourVariantStageResponseDto>> PostTourVariantStage(TourVariantStageRequestDto request, CancellationToken cancellationToken)
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
            catch (DbUpdateException exception) when (TryHandleDatabaseException(exception, out var result))
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
                        savedTourVariantStage.TourVariant.Name),
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

            return Created($"/api/tour-variants/{tourVariantStage.TourVariantId}/stages", response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTourVariantStage(long id, CancellationToken cancellationToken)
        {
            var tourVariantStage = await _context.TourVariantStages.FindAsync([id], cancellationToken);

            if (tourVariantStage == null)
            {
                return NotFound();
            }

            _context.TourVariantStages.Remove(tourVariantStage);
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        private ActionResult HandleDatabaseException(PostgresException exception)
        {
            return exception.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => Conflict("A tour variant stage with the same order already exists for this tour variant."),
                PostgresErrorCodes.ForeignKeyViolation => BadRequest("Tour variant or stage does not exist."),
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
