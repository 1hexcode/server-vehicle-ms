using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class Notifications : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Users User { get; set; }
    [Required]
    [StringLength(150)]
    public string Title { get; set; }
    public string? Body { get; set; }
    public NotificationType Type { get; set; } = NotificationType.Generic;
    public bool IsRead { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
