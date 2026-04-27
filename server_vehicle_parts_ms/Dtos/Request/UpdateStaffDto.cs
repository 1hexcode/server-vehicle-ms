using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class UpdateStaffDto
{
    [Required]
    public string FullName { get; set; }
    [Required]
    public string PhoneNumber { get; set; }
    [Required]
    public string Address { get; set; }
    public bool IsActive { get; set; }
}
