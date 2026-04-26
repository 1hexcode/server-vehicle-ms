using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class PurchaseInvoiceRequestDto
{
    [Required]
    public Guid VendorId { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    [Range(0, double.MaxValue)]
    public decimal Tax { get; set; }
    public string? Notes { get; set; }
    [Required]
    [MinLength(1)]
    public List<PurchaseInvoiceItemRequestDto> Items { get; set; } = new();
}

public class PurchaseInvoiceItemRequestDto
{
    [Required]
    public Guid VehiclePartId { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }
}
