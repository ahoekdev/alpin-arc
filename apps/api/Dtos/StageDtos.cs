namespace Api.Dtos;

using System.ComponentModel.DataAnnotations;

public class StageRequestDto
{
    [Required]
    public long StartLodgeId { get; set; }

    [Required]
    public long EndLodgeId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int DurationMinutes { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int DistanceMeters { get; set; }
}

public record StageResponseDto(
    long Id,
    LodgeSummaryDto StartLodge,
    LodgeSummaryDto EndLodge,
    int DurationMinutes,
    int DistanceMeters,
    DateTime CreatedAt);
