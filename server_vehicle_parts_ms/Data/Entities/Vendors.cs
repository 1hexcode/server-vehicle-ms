using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class Vendors : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    [StringLength(100)]
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    [Required]
    public string Phone { get; set; }
    public string? Address { get; set; }
    public decimal OpeningBalance { get; set; } = 0m;
    public decimal DueAmount { get; set; } = 0m;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<VendorPayments> Payments { get; set; } = new List<VendorPayments>();
}
