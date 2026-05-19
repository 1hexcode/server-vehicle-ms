using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class Vehicles : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Users Customer { get; set; }
    [Required]
    [StringLength(50)]
    public string VehicleNumber { get; set; }
    public VehicleType Type { get; set; }
    [StringLength(50)]
    public string? Make { get; set; }
    [StringLength(50)]
    public string? Model { get; set; }
    public int? Year { get; set; }
    [StringLength(30)]
    public string? Color { get; set; }
    [StringLength(500)]
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
