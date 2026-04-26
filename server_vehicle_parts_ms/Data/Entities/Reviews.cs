using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class Reviews : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Users Customer { get; set; }
    public Guid? AppointmentId { get; set; }
    public Appointments? Appointment { get; set; }
    public Guid? SalesInvoiceId { get; set; } // placeholder until sales invoices land
    [Range(1, 5)]
    public short Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
