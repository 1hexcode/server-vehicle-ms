namespace server_vehicle_parts_ms.Dtos.Response;

public class FinancialReportDto
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalPurchases { get; set; }
    public List<VendorSpendDto> ByVendor { get; set; } = new();
}

public class VendorSpendDto
{
    public Guid VendorId { get; set; }
    public string VendorName { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Total { get; set; }
}

public class InventoryReportDto
{
    public int TotalParts { get; set; }
    public int TotalUnitsInStock { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public List<VehiclePartDto> LowStockParts { get; set; } = new();
}
