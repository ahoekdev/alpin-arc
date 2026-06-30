namespace Api.Models;

public sealed class LodgeFavorite
{
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public long LodgeId { get; set; }

    public Lodge Lodge { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}
