using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/tour-variants")]
    [ApiController]
    public class TourVariantsController(ITourVariantService tourVariantService) : ControllerBase
    {
        private readonly ITourVariantService _tourVariantService = tourVariantService;

        [HttpGet("{id}")]
        public async Task<ActionResult<TourVariantDetailResponseDto>> GetTourVariant(long id, CancellationToken cancellationToken)
        {
            var result = await _tourVariantService.GetTourVariantAsync(id, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpGet("{id}/stages")]
        public async Task<ActionResult<IReadOnlyCollection<TourVariantStageResponseDto>>> GetTourVariantStages(long id, CancellationToken cancellationToken)
        {
            var result = await _tourVariantService.GetTourVariantStagesAsync(id, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTourVariant(long id, TourVariantRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _tourVariantService.UpdateTourVariantAsync(id, request, cancellationToken);

            return this.ToNoContentActionResult(result);
        }

        [HttpPost]
        public async Task<ActionResult<TourVariantResponseDto>> PostTourVariant(TourVariantRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _tourVariantService.CreateTourVariantAsync(request, cancellationToken);

            if (result.Succeeded)
            {
                return CreatedAtAction(nameof(GetTourVariant), new { id = result.Value.Id }, result.Value);
            }

            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTourVariant(long id, CancellationToken cancellationToken)
        {
            var result = await _tourVariantService.DeleteTourVariantAsync(id, cancellationToken);

            return this.ToNoContentActionResult(result);
        }
    }
}
