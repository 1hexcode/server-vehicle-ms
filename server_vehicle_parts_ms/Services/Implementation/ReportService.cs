using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class ReportService(AppDbContext db)
{
    public async Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync()
    {
        var activeStaff = await db.Users.CountAsync(u => u.Role == UserRoles.Staff && u.IsActive);
        var totalRevenue = await db.SalesInvoices
            .Where(s => s.Status != SalesInvoiceStatus.Void)
            .SumAsync(s => s.Total);
        var totalParts = await db.VehicleParts.CountAsync(p => p.IsActive);
        var lowStockAlerts = await db.VehicleParts.CountAsync(p => p.IsActive && p.StockQuantity <= p.ReorderLevel);

        return new ApiResponse<DashboardStatsDto>
        {
            Success = true,
            Data = new DashboardStatsDto
            {
                ActiveStaff = activeStaff,
                TotalRevenue = totalRevenue,
                TotalParts = totalParts,
                LowStockAlerts = lowStockAlerts
            }
        };
    }

    public async Task<ApiResponse<FinancialReportDto>> FinancialAsync(DateTimeOffset? from, DateTimeOffset? to)
    {
        var fromDate = from ?? DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = to ?? DateTimeOffset.UtcNow;

        var invoices = await db.PurchaseInvoices
            .Include(i => i.Vendor)
            .Where(i => i.ReceivedAt >= fromDate && i.ReceivedAt <= toDate)
            .ToListAsync();

        var byVendor = invoices
            .GroupBy(i => new { i.VendorId, i.Vendor.Name })
            .Select(g => new VendorSpendDto
            {
                VendorId = g.Key.VendorId,
                VendorName = g.Key.Name,
                InvoiceCount = g.Count(),
                Total = g.Sum(x => x.Total)
            })
            .OrderByDescending(v => v.Total)
            .ToList();

        return new ApiResponse<FinancialReportDto>
        {
            Success = true,
            Data = new FinancialReportDto
            {
                From = fromDate,
                To = toDate,
                InvoiceCount = invoices.Count,
                TotalPurchases = invoices.Sum(i => i.Total),
                ByVendor = byVendor
            }
        };
    }

    public async Task<ApiResponse<InventoryReportDto>> InventoryAsync()
    {
        var parts = await db.VehicleParts.Include(p => p.Category).Where(p => p.IsActive).ToListAsync();
        var lowStock = parts
            .Where(p => p.StockQuantity <= p.ReorderLevel)
            .OrderBy(p => p.StockQuantity)
            .Select(VehiclePartService.ToDto)
            .ToList();

        return new ApiResponse<InventoryReportDto>
        {
            Success = true,
            Data = new InventoryReportDto
            {
                TotalParts = parts.Count,
                TotalUnitsInStock = parts.Sum(p => p.StockQuantity),
                TotalInventoryValue = parts.Sum(p => p.UnitPrice * p.StockQuantity),
                LowStockParts = lowStock
            }
        };
    }
}
