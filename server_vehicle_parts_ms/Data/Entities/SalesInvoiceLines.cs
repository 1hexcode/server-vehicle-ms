using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class SalesInvoiceLines : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public SalesInvoices SalesInvoice { get; set; }
    public Guid PartId { get; set; }
    public VehicleParts Part { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
