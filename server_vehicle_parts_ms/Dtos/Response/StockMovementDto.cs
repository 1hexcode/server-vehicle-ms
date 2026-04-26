namespace server_vehicle_parts_ms.Dtos.Response;

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string? PartName { get; set; }
    public string? Sku { get; set; }
    public int DeltaQty { get; set; }
    public string Reason { get; set; }
    public Guid? RefInvoiceId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
