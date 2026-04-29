using System.ComponentModel.DataAnnotations;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Dtos.Request;

public class RegisterCustomerWithVehicleDto
{
    // Customer Details
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    public string? Password { get; set; }

    // Vehicle Details
    [Required]
    public string VehicleNumber { get; set; } = string.Empty;

    [Required]
    [Required]
    public VehicleType VehicleType { get; set; }

    public string? Make { get; set; }

    public string? Model { get; set; }

    public int? Year { get; set; }

    public string? Color { get; set; }
}
