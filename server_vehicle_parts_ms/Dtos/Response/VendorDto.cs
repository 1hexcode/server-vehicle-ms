namespace server_vehicle_parts_ms.Dtos.Response;

public class VendorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
