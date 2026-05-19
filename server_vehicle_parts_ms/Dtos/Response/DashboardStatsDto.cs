namespace server_vehicle_parts_ms.Dtos.Response;

public class DashboardStatsDto
{
    public int ActiveStaff { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalParts { get; set; }
    public int LowStockAlerts { get; set; }
}
