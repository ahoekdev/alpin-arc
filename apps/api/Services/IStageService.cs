using Api.Dtos;

namespace Api.Services;

public interface IStageService
{
    Task<ServiceResult<IReadOnlyCollection<StageResponseDto>>> GetStagesAsync(int? limit, CancellationToken cancellationToken);
    Task<ServiceResult<StageResponseDto>> GetStageAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateStageAsync(long id, StageRequestDto request, CancellationToken cancellationToken);
    Task<ServiceResult<StageResponseDto>> CreateStageAsync(StageRequestDto request, CancellationToken cancellationToken);
    Task<ServiceResult> DeleteStageAsync(long id, CancellationToken cancellationToken);
}
