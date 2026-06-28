using Api.Dtos;

namespace Api.Services;

public interface ITourService
{
    Task<ServiceResult<TourDetailResponseDto>> GetTourAsync(long id, CancellationToken cancellationToken);
}
