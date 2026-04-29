using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class StaffAppointmentRequestDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid VehicleId { get; set; }

    [Required]
    [StringLength(150)]
    public string ServiceType { get; set; }

    public DateTimeOffset RequestedAt { get; set; }

    public string? Notes { get; set; }
}
