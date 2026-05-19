using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class SystemSetting : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string SystemName { get; set; } = "VehicleHub Parts MS";

    [StringLength(100)]
    public string? ContactEmail { get; set; } = "support@vehiclehub.com";

    [StringLength(20)]
    public string? ContactPhone { get; set; } = "+977-9876543210";

    [StringLength(200)]
    public string? Address { get; set; } = "Kathmandu, Nepal";

    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "Rs.";

    [Required]
    public decimal TaxRate { get; set; } = 13.0m;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
