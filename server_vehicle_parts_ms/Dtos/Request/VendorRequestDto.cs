using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class VendorRequestDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    [StringLength(100)]
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    [Required]
    public string Phone { get; set; }
    public string? Address { get; set; }
}
