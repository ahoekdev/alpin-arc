using Api.Dtos;

namespace Api.Services;

public interface ILodgeService
{
    Task<ServiceResult<IReadOnlyCollection<LodgeSummaryDto>>> GetLodgesAsync(int? limit, CancellationToken cancellationToken);
    Task<ServiceResult<LodgeDetailDto>> GetLodgeAsync(long id, CancellationToken cancellationToken);
}
