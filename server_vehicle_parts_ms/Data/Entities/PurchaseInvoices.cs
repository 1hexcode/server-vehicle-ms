using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class PurchaseInvoices : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    [StringLength(40)]
    public string InvoiceNumber { get; set; }
    public Guid VendorId { get; set; }
    public Vendors Vendor { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Posted;
    public string? Notes { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<PurchaseInvoiceItems> Items { get; set; } = new();
}
