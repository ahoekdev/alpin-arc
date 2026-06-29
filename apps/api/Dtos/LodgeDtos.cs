namespace Api.Dtos;

using Api.Validation;
using System.ComponentModel.DataAnnotations;

public class LodgeRequestDto
{
    public long? Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = String.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = String.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    [SupportedCountryCode]
    public string CountryCode { get; set; } = String.Empty;
}

public record LodgeDetailDto(
    long Id,
    string Name,
    string Description,
    string CountryCode,
    DateTime CreatedAt,
    IReadOnlyCollection<StageSummaryDto> Stages,
    IReadOnlyCollection<TourSummaryDto> Tours);


public record LodgeSummaryDto(long Id, string Name, string Description, string CountryCode);
