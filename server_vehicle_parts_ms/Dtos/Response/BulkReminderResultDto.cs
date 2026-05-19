namespace server_vehicle_parts_ms.Dtos.Response;

public class BulkReminderResultDto
{
    public List<Guid> Queued { get; set; } = new();
    public List<Guid> Skipped { get; set; } = new();
    public List<Guid> NotFound { get; set; } = new();
}
