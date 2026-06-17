using Api.Dtos;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/tours")]
    [ApiController]
    public class ToursController(ITourService tourService) : ControllerBase
    {
        private readonly ITourService _tourService = tourService;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<TourResponseDto>>> GetTours([FromQuery] int? limit, CancellationToken cancellationToken)
        {
            var result = await _tourService.GetToursAsync(limit, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TourDetailResponseDto>> GetTour(long id, CancellationToken cancellationToken)
        {
            var result = await _tourService.GetTourAsync(id, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpGet("{id}/variants")]
        public async Task<ActionResult<IReadOnlyCollection<TourVariantResponseDto>>> GetTourVariants(long id, CancellationToken cancellationToken)
        {
            var result = await _tourService.GetTourVariantsAsync(id, cancellationToken);

            return this.ToActionResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTour(long id, TourRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _tourService.UpdateTourAsync(id, request, cancellationToken);

            return this.ToNoContentActionResult(result);
        }

        [HttpPost]
        public async Task<ActionResult<TourResponseDto>> PostTour(TourRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _tourService.CreateTourAsync(request, cancellationToken);

            if (result.Succeeded)
            {
                return CreatedAtAction(nameof(GetTour), new { id = result.Value.Id }, result.Value);
            }

            return this.ToActionResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(long id, CancellationToken cancellationToken)
        {
            var result = await _tourService.DeleteTourAsync(id, cancellationToken);

            return this.ToNoContentActionResult(result);
        }
    }
}
