using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/tour-variant-stages")]
    [ApiController]
    public class TourVariantStagesController(ITourVariantStageService tourVariantStageService) : ControllerBase
    {
        private readonly ITourVariantStageService _tourVariantStageService = tourVariantStageService;

        [HttpPut("{id}", Name = "updateTourVariantStage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutTourVariantStage(long id, TourVariantStageRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _tourVariantStageService.UpdateTourVariantStageAsync(id, request, cancellationToken);

            return this.ToNoContentActionResult(result);
        }

        [HttpPost(Name = "createTourVariantStage")]
        public async Task<ActionResult<TourVariantStageResponseDto>> PostTourVariantStage(TourVariantStageRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _tourVariantStageService.CreateTourVariantStageAsync(request, cancellationToken);

            if (result.Succeeded)
            {
                return Created($"/api/tour-variants/{request.TourVariantId}/stages", result.Value);
            }

            return this.ToActionResult(result);
        }

        [HttpDelete("{id}", Name = "deleteTourVariantStage")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTourVariantStage(long id, CancellationToken cancellationToken)
        {
            var result = await _tourVariantStageService.DeleteTourVariantStageAsync(id, cancellationToken);

            return this.ToNoContentActionResult(result);
        }
    }
}
