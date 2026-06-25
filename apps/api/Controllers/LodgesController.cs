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

        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<LodgeResponseDto>>> GetLodges([FromQuery] int? limit, CancellationToken cancellationToken)
        {
            var result = await _lodgeService.GetLodgesAsync(limit, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LodgeResponseDto>> GetLodge(long id, CancellationToken cancellationToken)
        {
            var result = await _lodgeService.GetLodgeAsync(id, cancellationToken);

            return this.ToActionResult(result);
        }

    }
}
