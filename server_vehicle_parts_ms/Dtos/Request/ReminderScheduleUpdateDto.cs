using System.ComponentModel.DataAnnotations;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Dtos.Request;

public class ReminderScheduleUpdateDto
{
    [Required]
    public ReminderFrequency Frequency { get; set; }

    [Required]
    [Range(0, 23)]
    public int Hour { get; set; }

    [Required]
    [Range(0, 59)]
    public int Minute { get; set; }

    // Required when Frequency == Weekly. 0=Sunday … 6=Saturday.
    [Range(0, 6)]
    public int? DayOfWeek { get; set; }

    // Required when Frequency == Monthly. 1..28 to keep the schedule valid in every month.
    [Range(1, 28)]
    public int? DayOfMonth { get; set; }

    public bool IsEnabled { get; set; } = true;

    [StringLength(2000)]
    public string? CustomMessage { get; set; }
}
