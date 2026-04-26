using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class SalesInvoiceRequestDto
{
    [Required]
    public Guid CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    [Range(0, double.MaxValue)]
    public decimal Tax { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    [Required]
    [MinLength(1)]
    public List<SalesInvoiceLineRequestDto> Lines { get; set; } = new();
}

public class SalesInvoiceLineRequestDto
{
    [Required]
    public Guid PartId { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
