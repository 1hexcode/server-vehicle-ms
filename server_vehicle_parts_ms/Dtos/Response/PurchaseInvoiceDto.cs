namespace server_vehicle_parts_ms.Dtos.Response;

public class PurchaseInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<PurchaseInvoiceItemDto> Items { get; set; } = new();
}

public class PurchaseInvoiceItemDto
{
    public Guid Id { get; set; }
    public Guid VehiclePartId { get; set; }
    public string VehiclePartName { get; set; }
    public string Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}
