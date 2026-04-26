namespace server_vehicle_parts_ms.Dtos.Response;

public class PartRequestDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleNumber { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public Guid? HandledByUserId { get; set; }
    public string? HandledByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
