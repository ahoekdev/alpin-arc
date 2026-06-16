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
    public class TourVariantStagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TourVariantStagesController(AppDbContext context)
        {
            _context = context;
        }

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

            var response = await TourVariantStageResponseQuery()
                .FirstAsync(savedTourVariantStage => savedTourVariantStage.Id == tourVariantStage.Id, cancellationToken);

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
