namespace Api.Dtos;

using System.ComponentModel.DataAnnotations;

public class TourVariantStageRequestDto
{
    [Required]
    public long TourVariantId { get; set; }

    [Required]
    public long StageId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Order { get; set; }
}

public record TourVariantStageResponseDto(
    long Id,
    TourVariantSummaryDto TourVariant,
    StageResponseDto Stage,
    int Order,
    DateTime CreatedAt);
