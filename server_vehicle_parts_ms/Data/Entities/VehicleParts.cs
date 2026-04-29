using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class VehicleParts : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public PartCategories Category { get; set; }
    public Guid? VendorId { get; set; }
    public Vendors? Vendor { get; set; }
    [Required]
    [StringLength(150)]
    public string Name { get; set; }
    [Required]
    [StringLength(50)]
    public string Sku { get; set; }
    public string? Description { get; set; }
    public decimal CostPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
