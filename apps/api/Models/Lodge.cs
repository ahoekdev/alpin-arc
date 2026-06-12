namespace Api.Models;

using System.ComponentModel.DataAnnotations;

public class Lodge
{
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = String.Empty;

    public DateTime CreatedAt { get; set; }
}
