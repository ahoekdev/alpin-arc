using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/stages")]
    [ApiController]
    public class StagesController(IStageService stageService) : ControllerBase
    {
        private readonly IStageService _stageService = stageService;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<StageResponseDto>>> GetStages([FromQuery] int? limit, CancellationToken cancellationToken)
        {
            var result = await _stageService.GetStagesAsync(limit, cancellationToken);

            return MapResult(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StageResponseDto>> GetStage(long id, CancellationToken cancellationToken)
        {
            var result = await _stageService.GetStageAsync(id, cancellationToken);

            return MapResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutStage(long id, StageRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _stageService.UpdateStageAsync(id, request, cancellationToken);

            return MapResult(result);
        }

        [HttpPost]
        public async Task<ActionResult<StageResponseDto>> PostStage(StageRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _stageService.CreateStageAsync(request, cancellationToken);

            if (result.Succeeded)
            {
                return CreatedAtAction(nameof(GetStage), new { id = result.Value.Id }, result.Value);
            }

            return MapResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStage(long id, CancellationToken cancellationToken)
        {
            var result = await _stageService.DeleteStageAsync(id, cancellationToken);

            return MapResult(result);
        }

        private ActionResult<T> MapResult<T>(ServiceResult<T> result)
            where T : notnull
        {
            if (result.Succeeded)
            {
                return result.Value;
            }

            return MapError(result.Error);
        }

        private IActionResult MapResult(ServiceResult result)
        {
            if (result.Succeeded)
            {
                return NoContent();
            }

            return MapError(result.Error);
        }

        private ActionResult MapError(ServiceError? error)
        {
            return error?.Type switch
            {
                ServiceErrorType.NotFound => NotFound(),
                ServiceErrorType.BadRequest => BadRequest(error.Message),
                ServiceErrorType.Conflict => Conflict(error.Message),
                _ => StatusCode(500),
            };
        }
    }
}
