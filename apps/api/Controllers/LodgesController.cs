using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/lodges")]
    [ApiController]
    public class LodgesController(ILodgeService lodgeService) : ControllerBase
    {
        private readonly ILodgeService _lodgeService = lodgeService;

        [HttpGet(Name = "getLodges")]
        public async Task<ActionResult<IReadOnlyCollection<LodgeSummaryDto>>> GetLodges([FromQuery] int? limit, CancellationToken cancellationToken)
        {
            var result = await _lodgeService.GetLodgesAsync(limit, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpGet("{id}", Name = "getLodgeById")]
        [ProducesResponseType(typeof(LodgeDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LodgeDetailDto>> GetLodge(long id, CancellationToken cancellationToken)
        {
            var result = await _lodgeService.GetLodgeAsync(id, cancellationToken);

            return this.ToActionResult(result);
        }

    }
}
