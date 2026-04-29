using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class ToggleCustomerStatusDto
{
    [Required]
    public bool IsActive { get; set; }
}
