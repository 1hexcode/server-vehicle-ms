using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class BulkInvoiceReminderDto
{
    [Required]
    [MinLength(1)]
    public List<Guid> InvoiceIds { get; set; } = new();
}
