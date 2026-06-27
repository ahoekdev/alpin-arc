namespace Api.Models;

using System.ComponentModel.DataAnnotations;

public class Tour
{
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = String.Empty;

    [Required]
    public string Description { get; set; } = String.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<TourVariant> Variants { get; set; } = [];
}
