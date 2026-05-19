using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class HotDeals : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }

    public Guid PartId { get; set; }
    public VehicleParts Part { get; set; } = null!;

    // Price after the discount. Must be > 0 and < Part.UnitPrice. Discount percent is derived.
    [Range(0.01, double.MaxValue)]
    public decimal DealPrice { get; set; }

    public DateTimeOffset StartsAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset EndsAt { get; set; }

    // Manual disable independent of the time window.
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
