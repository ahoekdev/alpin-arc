namespace Api.Dtos;

using System.ComponentModel.DataAnnotations;

public class TourVariantRequestDto
{
    [Required]
    public long TourId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = String.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = String.Empty;

    public bool IsPrimary { get; set; }
}

public record TourVariantResponseDto(
    long Id,
    TourSummaryDto Tour,
    string Name,
    string Description,
    bool IsPrimary,
    DateTime CreatedAt);

public record TourVariantSummaryDto(
    long Id,
    long TourId,
    string Name,
    string Description,
    bool IsPrimary);

public record TourVariantDetailResponseDto(
    long Id,
    TourSummaryDto Tour,
    string Name,
    string Description,
    bool IsPrimary,
    DateTime CreatedAt,
    IReadOnlyCollection<TourVariantStageResponseDto> Stages);
