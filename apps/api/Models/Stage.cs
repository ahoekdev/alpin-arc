namespace Api.Models;

public class Stage
{
    public long Id { get; set; }

    public long StartLodgeId { get; set; }
    public Lodge StartLodge { get; set; } = null!;

    public long EndLodgeId { get; set; }
    public Lodge EndLodge { get; set; } = null!;

    public int DurationMinutes { get; set; }
    public int DistanceMeters { get; set; }

    public DateTime CreatedAt { get; set; }
}
