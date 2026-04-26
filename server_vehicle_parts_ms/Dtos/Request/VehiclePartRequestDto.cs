using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class VehiclePartRequestDto
{
    [Required]
    public Guid CategoryId { get; set; }
    [Required]
    [StringLength(150)]
    public string Name { get; set; }
    [Required]
    [StringLength(50)]
    public string Sku { get; set; }
    public string? Description { get; set; }
    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; } = 10;
}
