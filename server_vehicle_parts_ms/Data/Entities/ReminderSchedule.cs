using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class ReminderSchedule : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(80)]
    public string JobKey { get; set; } = "";

    [Required]
    [StringLength(150)]
    public string DisplayName { get; set; } = "";

    [StringLength(500)]
    public string? Description { get; set; }

    public ReminderFrequency Frequency { get; set; } = ReminderFrequency.Daily;

    [Range(0, 23)]
    public int Hour { get; set; } = 8;

    [Range(0, 59)]
    public int Minute { get; set; } = 0;

    // 0 = Sunday … 6 = Saturday. Used when Frequency == Weekly.
    [Range(0, 6)]
    public int? DayOfWeek { get; set; }

    // 1..28 (capped to 28 so monthly schedules still fire in February).
    [Range(1, 28)]
    public int? DayOfMonth { get; set; }

    public bool IsEnabled { get; set; } = true;

    // Optional admin-authored note rendered inside every email this job sends.
    // Stored as plain text; HTML-encoded and line-break-converted at render time.
    [StringLength(2000)]
    public string? CustomMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
