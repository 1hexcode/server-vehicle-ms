using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class PartCategories : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    public VehicleType VehicleType { get; set; }
    public Guid? ParentId { get; set; }
    public PartCategories? Parent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
