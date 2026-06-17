using Api.Dtos;

namespace Api.Services;

public interface ITourVariantStageService
{
    Task<ServiceResult> UpdateTourVariantStageAsync(long id, TourVariantStageRequestDto request, CancellationToken cancellationToken);
    Task<ServiceResult<TourVariantStageResponseDto>> CreateTourVariantStageAsync(TourVariantStageRequestDto request, CancellationToken cancellationToken);
    Task<ServiceResult> DeleteTourVariantStageAsync(long id, CancellationToken cancellationToken);
}
