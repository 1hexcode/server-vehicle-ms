namespace server_vehicle_parts_ms.Dtos.Response;

public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public short Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
