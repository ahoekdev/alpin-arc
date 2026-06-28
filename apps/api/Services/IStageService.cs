using Api.Dtos;

namespace Api.Services;

public interface IStageService
{
    Task<ServiceResult<IReadOnlyCollection<StageResponseDto>>> GetStagesAsync(int? limit, CancellationToken cancellationToken);
    Task<ServiceResult<StageResponseDto>> GetStageAsync(long id, CancellationToken cancellationToken);
}
