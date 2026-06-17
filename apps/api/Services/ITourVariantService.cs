using Api.Dtos;

namespace Api.Services;

public interface ITourVariantService
{
    Task<ServiceResult<TourVariantDetailResponseDto>> GetTourVariantAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<IReadOnlyCollection<TourVariantStageResponseDto>>> GetTourVariantStagesAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateTourVariantAsync(long id, TourVariantRequestDto request, CancellationToken cancellationToken);
    Task<ServiceResult<TourVariantResponseDto>> CreateTourVariantAsync(TourVariantRequestDto request, CancellationToken cancellationToken);
    Task<ServiceResult> DeleteTourVariantAsync(long id, CancellationToken cancellationToken);
}
