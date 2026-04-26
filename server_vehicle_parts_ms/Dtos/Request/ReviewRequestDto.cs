using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class ReviewRequestDto
{
    public Guid? AppointmentId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    [Range(1, 5)]
    public short Rating { get; set; }
    public string? Comment { get; set; }
}
