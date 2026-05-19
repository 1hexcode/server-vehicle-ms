using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Data.Entities;

public class Users : IHasTimestamps
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    [Required]
    [StringLength(50)]
    public string FullName { get; set; }
    [Required]
    public UserRoles Role { get; set; } = UserRoles.Customer;
    [Required]
    public string PhoneNumber { get; set; }
    [Required]
    public string Address { get; set; }
    public bool IsActive { get; set; } = true;

    // Lifetime loyalty balance. Earned on sales-invoice creation (1 pt per Rs. 100 of subtotal),
    // reversed on void. Only meaningful for Customer-role users.
    public int LoyaltyPoints { get; set; } = 0;

    // ── Email verification ────────────────────────────────────────────────
    /// <summary>True once the user has clicked the verification link.</summary>
    public bool IsEmailVerified { get; set; } = false;
    /// <summary>Crypto-random token stored plain-text; cleared after use.</summary>
    public string? EmailVerificationToken { get; set; }
    /// <summary>UTC expiry for the token (24 h window).</summary>
    public DateTimeOffset? EmailVerificationTokenExpiry { get; set; }
    // ─────────────────────────────────────────────────────────────────────

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}