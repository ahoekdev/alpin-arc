namespace Api.Dtos;

using System.ComponentModel.DataAnnotations;

public class TourVariantRequestDto
{
    [Required]
    public long TourId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = String.Empty;
}

public record TourVariantResponseDto(
    long Id,
    TourSummaryDto Tour,
    string Name,
    DateTime CreatedAt);

public record TourVariantSummaryDto(
    long Id,
    long TourId,
    string Name);
