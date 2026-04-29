using System.ComponentModel.DataAnnotations;

namespace server_vehicle_parts_ms.Dtos.Request;

public class VendorPaymentRequestDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    public string Type { get; set; } = "Cash"; // Cash, Card, Online

    public string? AttachmentUrl { get; set; }
    
    public string? ReceiptNo { get; set; }

    public string? Notes { get; set; }
}
