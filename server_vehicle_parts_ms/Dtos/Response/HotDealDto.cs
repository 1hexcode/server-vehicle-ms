namespace server_vehicle_parts_ms.Dtos.Response;

public class HotDealDto
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = "";
    public string Sku { get; set; } = "";
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }

    public decimal OriginalPrice { get; set; }
    public decimal DealPrice { get; set; }
    public int DiscountPercent { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentlyActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
