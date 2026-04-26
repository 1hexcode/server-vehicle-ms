using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class SalesInvoices : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    [StringLength(40)]
    public string InvoiceNumber { get; set; }
    public Guid CustomerId { get; set; }
    public Users Customer { get; set; }
    public Guid? VehicleId { get; set; }
    public Vehicles? Vehicle { get; set; }
    public Guid CreatedByUserId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public SalesInvoiceStatus Status { get; set; } = SalesInvoiceStatus.Draft;
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<SalesInvoiceLines> Lines { get; set; } = new();
}
