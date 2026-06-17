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

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTourVariantStage(long id, TourVariantStageRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _tourVariantStageService.UpdateTourVariantStageAsync(id, request, cancellationToken);

            return MapResult(result);
        }

        [HttpPost]
        public async Task<ActionResult<TourVariantStageResponseDto>> PostTourVariantStage(TourVariantStageRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _tourVariantStageService.CreateTourVariantStageAsync(request, cancellationToken);

            if (result.Succeeded)
            {
                return Created($"/api/tour-variants/{request.TourVariantId}/stages", result.Value);
            }

            return MapResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTourVariantStage(long id, CancellationToken cancellationToken)
        {
            var result = await _tourVariantStageService.DeleteTourVariantStageAsync(id, cancellationToken);

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
