using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace api.Controllers
{
    [Route("api/stages")]
    [ApiController]
    public class StagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StagesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StageResponseDto>>> GetStages()
        {
            return await _context.Stages
                .OrderBy(s => s.StartLodge.Name)
                .ThenBy(s => s.EndLodge.Name)
                .Select(s => new StageResponseDto(
                    s.Id,
                    new LodgeSummaryDto(s.StartLodge.Id, s.StartLodge.Name),
                    new LodgeSummaryDto(s.EndLodge.Id, s.EndLodge.Name),
                    s.DurationMinutes,
                    s.DistanceMeters,
                    s.CreatedAt))
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StageResponseDto>> GetStage(long id)
        {
            var stage = await StageResponseQuery()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stage == null)
            {
                return NotFound();
            }

            return stage;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutStage(long id, StageRequestDto request)
        {
            var stage = await _context.Stages.FindAsync(id);

            if (stage == null)
            {
                return NotFound();
            }

            _context.Entry(stage).State = EntityState.Modified;

            stage.StartLodgeId = request.StartLodgeId;
            stage.EndLodgeId = request.EndLodgeId;
            stage.DurationMinutes = request.DurationMinutes;
            stage.DistanceMeters = request.DistanceMeters;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (TryHandleDatabaseException(exception, out var result))
            {
                return result;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<StageResponseDto>> PostStage(StageRequestDto request)
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
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (TryHandleDatabaseException(exception, out var result))
            {
                return result;
            }

            var response = await StageResponseQuery()
                .FirstAsync(savedStage => savedStage.Id == stage.Id);

            return CreatedAtAction(nameof(GetStage), new { id = stage.Id }, response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStage(long id)
        {
            var stage = await _context.Stages.FindAsync(id);

            if (stage == null)
            {
                return NotFound();
            }

            _context.Stages.Remove(stage);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private IQueryable<StageResponseDto> StageResponseQuery()
        {
            return _context.Stages.Select(stage => new StageResponseDto(
                stage.Id,
                new LodgeSummaryDto(stage.StartLodge.Id, stage.StartLodge.Name),
                new LodgeSummaryDto(stage.EndLodge.Id, stage.EndLodge.Name),
                stage.DurationMinutes,
                stage.DistanceMeters,
                stage.CreatedAt));
        }

        private ActionResult HandleDatabaseException(PostgresException exception)
        {
            return exception.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => Conflict("A stage with the same start and end lodges already exists."),
                PostgresErrorCodes.ForeignKeyViolation => BadRequest("Start or end lodge does not exist."),
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
