using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class VendorAddPartsDto
{
    [Required]
    public Guid PartId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Range(0.0, double.MaxValue)]
    public decimal PricePerUnit { get; set; }
}
