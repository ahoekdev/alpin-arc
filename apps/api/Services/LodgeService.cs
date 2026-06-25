using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class LodgeService(AppDbContext context) : ILodgeService
{
    private readonly AppDbContext _context = context;

    public async Task<ServiceResult<IReadOnlyCollection<LodgeResponseDto>>> GetLodgesAsync(int? limit, CancellationToken cancellationToken)
    {
        if (limit is <= 0 or > 100)
        {
            return ServiceResult<IReadOnlyCollection<LodgeResponseDto>>.BadRequest("Limit must be between 1 and 100.");
        }

        IQueryable<Lodge> query = _context.Lodges
            .AsNoTracking()
            .OrderBy(lodge => lodge.Name);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        var lodges = await query
            .Select(lodge => new LodgeResponseDto(lodge.Id, lodge.Name, lodge.CreatedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyCollection<LodgeResponseDto>>.Success(lodges);
    }

    public async Task<ServiceResult<LodgeResponseDto>> GetLodgeAsync(long id, CancellationToken cancellationToken)
    {
        var lodge = await _context.Lodges
            .AsNoTracking()
            .Where(lodge => lodge.Id == id)
            .Select(lodge => new LodgeResponseDto(lodge.Id, lodge.Name, lodge.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        if (lodge is null)
        {
            return ServiceResult<LodgeResponseDto>.NotFound("Lodge does not exist.");
        }

        return ServiceResult<LodgeResponseDto>.Success(lodge);
    }

}
