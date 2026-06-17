using Api.Dtos;

namespace Api.Services;

public interface ITourService
{
    Task<ServiceResult<IReadOnlyCollection<TourResponseDto>>> GetToursAsync(int? limit, CancellationToken cancellationToken);
    Task<ServiceResult<TourDetailResponseDto>> GetTourAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult<IReadOnlyCollection<TourVariantResponseDto>>> GetTourVariantsAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateTourAsync(long id, TourRequestDto request, CancellationToken cancellationToken);
    Task<ServiceResult<TourResponseDto>> CreateTourAsync(TourRequestDto request, CancellationToken cancellationToken);
    Task<ServiceResult> DeleteTourAsync(long id, CancellationToken cancellationToken);
}
