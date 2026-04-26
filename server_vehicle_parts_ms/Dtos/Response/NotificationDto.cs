namespace server_vehicle_parts_ms.Dtos.Response;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; }
    public string? Body { get; set; }
    public string Type { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
