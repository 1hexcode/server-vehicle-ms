using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class PartRequests : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Users Customer { get; set; }
    public Guid VehicleId { get; set; }
    public Vehicles Vehicle { get; set; }
    [Required]
    public string Description { get; set; }
    public PartRequestStatus Status { get; set; } = PartRequestStatus.Requested;
    public Guid? HandledByUserId { get; set; }
    public Users? HandledBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
