namespace server_vehicle_parts_ms.Dtos.Response;

public class ProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
    public int LoyaltyPoints { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
