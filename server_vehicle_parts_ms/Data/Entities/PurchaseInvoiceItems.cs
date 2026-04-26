using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class PurchaseInvoiceItems : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    public Guid PurchaseInvoiceId { get; set; }
    public PurchaseInvoices PurchaseInvoice { get; set; }
    public Guid VehiclePartId { get; set; }
    public VehicleParts VehiclePart { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
