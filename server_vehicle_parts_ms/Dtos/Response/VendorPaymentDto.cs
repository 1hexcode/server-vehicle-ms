namespace server_vehicle_parts_ms.Dtos.Response;

public class VendorPaymentDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? ReceiptNo { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
