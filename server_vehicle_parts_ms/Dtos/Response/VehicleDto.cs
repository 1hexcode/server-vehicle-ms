namespace server_vehicle_parts_ms.Dtos.Response;

public class VehicleDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string VehicleNumber { get; set; }
    public string Type { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? Color { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
