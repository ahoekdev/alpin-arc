namespace Api.Models;

public class TourVariantStage
{
    public long Id { get; set; }

    public long TourVariantId { get; set; }
    public TourVariant TourVariant { get; set; } = null!;

    public long StageId { get; set; }
    public Stage Stage { get; set; } = null!;

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }
}
