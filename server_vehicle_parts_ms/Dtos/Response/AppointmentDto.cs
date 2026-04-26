namespace server_vehicle_parts_ms.Dtos.Response;

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleNumber { get; set; }
    public string ServiceType { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string Status { get; set; }
    public string? Notes { get; set; }
    public Guid? AssignedStaffUserId { get; set; }
    public string? AssignedStaffName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
