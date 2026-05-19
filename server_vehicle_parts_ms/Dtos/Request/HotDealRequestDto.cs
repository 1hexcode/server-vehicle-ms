using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class HotDealRequestDto
{
    [Required]
    public Guid PartId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal DealPrice { get; set; }

    [Required]
    public DateTimeOffset StartsAt { get; set; }

    [Required]
    public DateTimeOffset EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}
