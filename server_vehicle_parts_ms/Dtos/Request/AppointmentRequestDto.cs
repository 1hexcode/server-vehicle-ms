using System.ComponentModel.DataAnnotations;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Dtos.Request;

public class AppointmentRequestDto
{
    [Required]
    public Guid VehicleId { get; set; }
    [Required]
    [StringLength(150)]
    public string ServiceType { get; set; }
    [Required]
    public DateTimeOffset RequestedAt { get; set; }
    public string? Notes { get; set; }
}

public class AppointmentStatusUpdateDto
{
    [Required]
    public AppointmentStatus Status { get; set; }
    public Guid? AssignedStaffUserId { get; set; }
    public string? Notes { get; set; }
}
