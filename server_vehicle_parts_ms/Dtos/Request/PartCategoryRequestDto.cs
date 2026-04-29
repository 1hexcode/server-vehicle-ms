using System.ComponentModel.DataAnnotations;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Dtos.Request;

public class PartCategoryRequestDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    [Required]
    public VehicleType VehicleType { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}
