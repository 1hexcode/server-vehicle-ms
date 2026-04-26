using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class Appointments : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Users Customer { get; set; }
    public Guid VehicleId { get; set; }
    public Vehicles Vehicle { get; set; }
    [Required]
    [StringLength(150)]
    public string ServiceType { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public Guid? AssignedStaffUserId { get; set; }
    public Users? AssignedStaff { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
