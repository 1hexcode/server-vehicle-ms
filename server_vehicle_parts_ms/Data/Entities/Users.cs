using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class Users : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    [Required]
    [StringLength(50)]
    public string FullName { get; set; }
    [Required]
    public UserRoles Role { get; set; } = UserRoles.Customer;
    [Required]
    public string PhoneNumber { get; set; }
    [Required]
    public string Address { get; set; }
    public bool isActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}