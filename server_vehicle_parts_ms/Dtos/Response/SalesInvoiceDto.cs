namespace server_vehicle_parts_ms.Dtos.Response;

public class SalesInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehicleNumber { get; set; }
    public Guid CreatedByUserId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<SalesInvoiceLineDto> Lines { get; set; } = new();
}

public class SalesInvoiceLineDto
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; }
    public string Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
