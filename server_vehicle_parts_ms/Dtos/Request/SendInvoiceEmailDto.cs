using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class SendInvoiceEmailDto
{
    // Optional override. When null/blank, the invoice's customer email is used.
    [EmailAddress]
    [StringLength(150)]
    public string? ToEmail { get; set; }
}
