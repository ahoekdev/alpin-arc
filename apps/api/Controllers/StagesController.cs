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

        [HttpGet(Name = "getStages")]
        public async Task<ActionResult<IReadOnlyCollection<StageResponseDto>>> GetStages([FromQuery] int? limit, CancellationToken cancellationToken)
        {
            var result = await _stageService.GetStagesAsync(limit, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpGet("{id}", Name = "getStageById")]
        [ProducesResponseType(typeof(StageResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StageResponseDto>> GetStage(long id, CancellationToken cancellationToken)
        {
            var result = await _stageService.GetStageAsync(id, cancellationToken);

            return this.ToActionResult(result);
        }

    }
}
