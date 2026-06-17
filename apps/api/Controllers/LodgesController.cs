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

        // GET: api/Lodges
        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<LodgeResponseDto>>> GetLodges([FromQuery] int? limit, CancellationToken cancellationToken)
        {
            var result = await _lodgeService.GetLodgesAsync(limit, cancellationToken);

            return this.ToActionResult(result);
        }

        // GET: api/Lodges/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LodgeResponseDto>> GetLodge(long id, CancellationToken cancellationToken)
        {
            var result = await _lodgeService.GetLodgeAsync(id, cancellationToken);

            return this.ToActionResult(result);
        }

        // PUT: api/Lodges/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLodge(long id, LodgeRequestDto request, CancellationToken cancellationToken)
        {
            if (request.Id.HasValue && request.Id.Value != id)
            {
                return BadRequest();
            }

            var result = await _lodgeService.UpdateLodgeAsync(id, request, cancellationToken);

            return this.ToNoContentActionResult(result);
        }

        // POST: api/Lodges
        [HttpPost]
        public async Task<ActionResult<LodgeResponseDto>> PostLodge(LodgeRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _lodgeService.CreateLodgeAsync(request, cancellationToken);

            if (result.Succeeded)
            {
                return CreatedAtAction(nameof(GetLodge), new { id = result.Value.Id }, result.Value);
            }

            return this.ToActionResult(result);
        }

        // DELETE: api/Lodges/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLodge(long id, CancellationToken cancellationToken)
        {
            var result = await _lodgeService.DeleteLodgeAsync(id, cancellationToken);

            return this.ToNoContentActionResult(result);
        }
    }
}
