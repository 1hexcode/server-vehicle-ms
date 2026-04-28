using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public enum PaymentType { Cash, Card, Online }

public class VendorPayments : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid VendorId { get; set; }
    public Vendors Vendor { get; set; } = null!;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public PaymentType Type { get; set; } = PaymentType.Cash;

    public string? AttachmentUrl { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
