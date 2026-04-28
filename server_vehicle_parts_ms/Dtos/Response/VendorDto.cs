namespace server_vehicle_parts_ms.Dtos.Response;

public class VendorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string Phone { get; set; }
    public string? Address { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal DueAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
