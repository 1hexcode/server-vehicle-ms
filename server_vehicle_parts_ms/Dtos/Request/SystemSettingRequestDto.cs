using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class SystemSettingRequestDto
{
    [Required]
    [StringLength(100)]
    public string SystemName { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? ContactEmail { get; set; }

    [StringLength(20)]
    public string? ContactPhone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [Required]
    [StringLength(10)]
    public string Currency { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal TaxRate { get; set; }
}
