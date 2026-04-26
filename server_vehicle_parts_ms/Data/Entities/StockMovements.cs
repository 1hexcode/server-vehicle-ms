using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class StockMovements
{
    [Key]
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public VehicleParts Part { get; set; }
    public int DeltaQty { get; set; }
    public StockMovementReason Reason { get; set; }
    public Guid? RefInvoiceId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
