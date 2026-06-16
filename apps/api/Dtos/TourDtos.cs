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

public record TourSummaryDto(long Id, string Name);
