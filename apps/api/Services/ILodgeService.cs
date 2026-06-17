using Api.Dtos;

namespace Api.Services;

public interface ILodgeService
{
    Task<ServiceResult<IReadOnlyCollection<LodgeResponseDto>>> GetLodgesAsync(int? limit, CancellationToken cancellationToken);
    Task<ServiceResult<LodgeResponseDto>> GetLodgeAsync(long id, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateLodgeAsync(long id, LodgeRequestDto request, CancellationToken cancellationToken);
    Task<ServiceResult<LodgeResponseDto>> CreateLodgeAsync(LodgeRequestDto request, CancellationToken cancellationToken);
    Task<ServiceResult> DeleteLodgeAsync(long id, CancellationToken cancellationToken);
}
