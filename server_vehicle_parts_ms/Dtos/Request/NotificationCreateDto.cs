using System.ComponentModel.DataAnnotations;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Dtos.Request;

public class NotificationCreateDto
{
    [Required]
    public Guid UserId { get; set; }
    [Required]
    [StringLength(150)]
    public string Title { get; set; }
    public string? Body { get; set; }
    public NotificationType Type { get; set; } = NotificationType.Generic;
}
