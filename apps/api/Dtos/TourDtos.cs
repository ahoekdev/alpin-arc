namespace Api.Dtos;

using System.ComponentModel.DataAnnotations;

public class TourRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = String.Empty;
}

public record TourResponseDto(
    long Id,
    string Name,
    DateTime CreatedAt);

public record TourDetailResponseDto(
    long Id,
    string Name,
    DateTime CreatedAt,
    IReadOnlyCollection<TourDetailVariantDto> Variants);

public record TourDetailVariantDto(
    long Id,
    long TourId,
    string Name,
    int TotalDistanceMeters,
    int TotalDurationMinutes,
    int StageCount);

public record TourSummaryDto(long Id, string Name);
