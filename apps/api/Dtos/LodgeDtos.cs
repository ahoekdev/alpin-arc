namespace Api.Dtos;

using System.ComponentModel.DataAnnotations;

public class LodgeRequestDto
{
    public long? Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = String.Empty;
}

public record LodgeResponseDto(
    long Id,
    string Name,
    DateTime CreatedAt);

public record LodgeSummaryDto(long Id, string Name);
