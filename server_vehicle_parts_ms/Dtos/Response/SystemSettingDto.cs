namespace server_vehicle_parts_ms.Dtos.Response;

public class SystemSettingDto
{
    public Guid Id { get; set; }
    public string SystemName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string Currency { get; set; }
    public decimal TaxRate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
