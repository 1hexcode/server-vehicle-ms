namespace server_vehicle_parts_ms.Dtos.Response;

public class ReminderScheduleDto
{
    public Guid Id { get; set; }
    public string JobKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }
    public string Frequency { get; set; } = "";
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public bool IsEnabled { get; set; }
    public string? CustomMessage { get; set; }
    public string Cron { get; set; } = "";
    public string TimeZone { get; set; } = "UTC";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
