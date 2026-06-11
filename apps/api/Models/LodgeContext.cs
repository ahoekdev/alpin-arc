using Microsoft.EntityFrameworkCore;

namespace Api.Models;

public class LodgeContext : DbContext
{
    public LodgeContext(DbContextOptions<LodgeContext> options)
        : base(options)
    {
    }

    public DbSet<Lodge> Lodges { get; set; } = null!;
}