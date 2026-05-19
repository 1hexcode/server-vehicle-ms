using System.ComponentModel.DataAnnotations;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Dtos.Request;

public class VehicleRequestDto
{
    [Required]
    [StringLength(50)]
    public string VehicleNumber { get; set; }
    [Required]
    public VehicleType Type { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? Color { get; set; }
    [StringLength(500)]
    public string? ImageUrl { get; set; }
}
