using System.ComponentModel.DataAnnotations;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Dtos.Request;

public class PartRequestRequestDto
{
    [Required]
    public Guid VehicleId { get; set; }
    [Required]
    public string Description { get; set; }
}

public class PartRequestStatusUpdateDto
{
    [Required]
    public PartRequestStatus Status { get; set; }
}
