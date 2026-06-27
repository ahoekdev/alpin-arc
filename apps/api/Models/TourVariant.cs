namespace Api.Models;

using System.ComponentModel.DataAnnotations;

public class TourVariant
{
    public long Id { get; set; }

    public long TourId { get; set; }
    public Tour Tour { get; set; } = null!;

    [Required]
    public string Name { get; set; } = String.Empty;

    [Required]
    public string Description { get; set; } = String.Empty;

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<TourVariantStage> Stages { get; set; } = [];
}
