using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class RegisterUserDto
{
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    [Required]
    public string PasswordVerify { get; set; }
    [Required]
    public string FullName { get; set; }
    [Required]
    public string PhoneNumber { get; set; }
    [Required]
    public string Address { get; set; }
}
