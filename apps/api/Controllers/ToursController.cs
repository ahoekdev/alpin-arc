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

        [HttpGet("{id}", Name = "getTourById")]
        [ProducesResponseType(typeof(TourDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TourDetailResponseDto>> GetTour(long id, CancellationToken cancellationToken)
        {
            var result = await _tourService.GetTourAsync(id, cancellationToken);

            return this.ToActionResult(result);
        }
    }
}
