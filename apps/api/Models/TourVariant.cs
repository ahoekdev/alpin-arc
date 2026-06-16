namespace Api.Models;

using System.ComponentModel.DataAnnotations;

public class TourVariant
{
    public long Id { get; set; }

    public long TourId { get; set; }
    public Tour Tour { get; set; } = null!;

    [Required]
    public string Name { get; set; } = String.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<TourVariantStage> Stages { get; set; } = [];
}
