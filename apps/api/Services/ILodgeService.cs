using Api.Dtos;

namespace Api.Services;

public interface ILodgeService
{
    Task<ServiceResult<IReadOnlyCollection<LodgeResponseDto>>> GetLodgesAsync(int? limit, CancellationToken cancellationToken);
    Task<ServiceResult<LodgeResponseDto>> GetLodgeAsync(long id, CancellationToken cancellationToken);
}
