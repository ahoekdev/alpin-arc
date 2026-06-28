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

        [HttpGet(Name = "getTourVariants")]
        [ProducesResponseType(typeof(IReadOnlyCollection<TourVariantResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyCollection<TourVariantResponseDto>>> GetTourVariants([FromQuery] int? limit, [FromQuery] long? lodgeId, CancellationToken cancellationToken)
        {
            var result = await _tourVariantService.GetTourVariantsAsync(limit, lodgeId, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpGet("{id}", Name = "getTourVariantById")]
        [ProducesResponseType(typeof(TourVariantDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TourVariantDetailResponseDto>> GetTourVariant(long id, CancellationToken cancellationToken)
        {
            var result = await _tourVariantService.GetTourVariantAsync(id, cancellationToken);

            return this.ToActionResult(result);
        }

    }
}
