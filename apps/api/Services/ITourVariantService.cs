using Api.Dtos;

namespace Api.Services;

public interface ITourVariantService
{
    Task<ServiceResult<IReadOnlyCollection<TourVariantResponseDto>>> GetTourVariantsAsync(int? limit, long? lodgeId, CancellationToken cancellationToken);
    Task<ServiceResult<TourVariantDetailResponseDto>> GetTourVariantAsync(long id, CancellationToken cancellationToken);
}
