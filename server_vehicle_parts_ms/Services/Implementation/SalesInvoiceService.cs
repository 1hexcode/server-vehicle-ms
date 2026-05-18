using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class SalesInvoiceService(AppDbContext db, StockMovementService stock, NotificationService notifications)
{
    private const decimal LoyaltyThreshold = 5000m;
    private const decimal LoyaltyRate = 0.10m;

    public async Task<ApiResponse<SalesInvoiceDto>> CreateAsync(SalesInvoiceRequestDto dto, Guid createdBy)
    {
        var customer = await db.Users.FirstOrDefaultAsync(u => u.Id == dto.CustomerId);
        if (customer == null || customer.Role != UserRoles.Customer)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Customer not found" };

        if (dto.VehicleId.HasValue)
        {
            var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId.Value);
            if (vehicle == null)
                return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Vehicle not found" };
            if (vehicle.CustomerId != customer.Id)
                return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Vehicle does not belong to that customer" };
        }

        var partIds = dto.Lines.Select(l => l.PartId).Distinct().ToList();
        var parts = await db.VehicleParts.Where(p => partIds.Contains(p.Id)).ToListAsync();
        if (parts.Count != partIds.Count)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "One or more parts not found" };

        var partLookup = parts.ToDictionary(p => p.Id);

        // pre-check stock availability (sum requested per part across lines)
        var requested = dto.Lines.GroupBy(l => l.PartId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        foreach (var (pid, needed) in requested)
        {
            if (partLookup[pid].StockQuantity < needed)
                return new ApiResponse<SalesInvoiceDto>
                {
                    Success = false,
                    Message = $"Insufficient stock for part {partLookup[pid].Sku} (have {partLookup[pid].StockQuantity}, need {needed})"
                };
        }

        var invoice = new SalesInvoices
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.Id,
            VehicleId = dto.VehicleId,
            CreatedByUserId = createdBy,
            Status = SalesInvoiceStatus.Issued,
            IssuedAt = DateTimeOffset.UtcNow,
            DueAt = dto.DueAt,
            TaxRate = dto.TaxRate,
            DiscountRate = dto.DiscountRate,
            ServiceCharge = dto.ServiceCharge,
        };

        decimal subtotal = 0;
        foreach (var line in dto.Lines)
        {
            var part = partLookup[line.PartId];
            var lineTotal = line.UnitPrice * line.Quantity;
            invoice.Lines.Add(new SalesInvoiceLines
            {
                PartId = part.Id,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = lineTotal,
            });
            part.StockQuantity -= line.Quantity;

            if (part.StockQuantity <= part.ReorderLevel)
            {
                await notifications.NotifyAdminsAsync(
                    "Low Stock Alert",
                    $"Part {part.Name} ({part.Sku}) is low on stock. Current quantity: {part.StockQuantity}",
                    NotificationType.LowStock
                );
            }

            subtotal += lineTotal;
        }

        invoice.Subtotal = subtotal;
        invoice.Discount = Math.Round(subtotal * (invoice.DiscountRate / 100m), 2);
        invoice.Tax = Math.Round((subtotal + invoice.ServiceCharge) * (invoice.TaxRate / 100m), 2);
        invoice.Total = subtotal - invoice.Discount + invoice.ServiceCharge + invoice.Tax;

        db.SalesInvoices.Add(invoice);

        foreach (var line in invoice.Lines)
            stock.Record(line.PartId, -line.Quantity, StockMovementReason.SalesInvoice, invoice.Id);

        await db.SaveChangesAsync();
        return new ApiResponse<SalesInvoiceDto> { Success = true, Message = "Invoice issued", Data = await LoadDtoAsync(invoice.Id) };
    }

    public async Task<ApiResponse<List<SalesInvoiceDto>>> ListAsync(Guid? customerFilter)
    {
        var q = db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Vehicle)
            .Include(i => i.Lines).ThenInclude(l => l.Part)
            .AsQueryable();
        if (customerFilter.HasValue) q = q.Where(i => i.CustomerId == customerFilter.Value);
        var items = await q.OrderByDescending(i => i.IssuedAt).ToListAsync();
        return new ApiResponse<List<SalesInvoiceDto>> { Success = true, Data = items.Select(ToDto).ToList() };
    }

    public async Task<ApiResponse<SalesInvoiceDto>> GetAsync(Guid id, Guid? customerFilter)
    {
        var invoice = await db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Vehicle)
            .Include(i => i.Lines).ThenInclude(l => l.Part)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Invoice not found" };
        if (customerFilter.HasValue && invoice.CustomerId != customerFilter.Value)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Forbidden" };
        return new ApiResponse<SalesInvoiceDto> { Success = true, Data = ToDto(invoice) };
    }

    public async Task<ApiResponse<SalesInvoiceDto>> VoidAsync(Guid id)
    {
        var invoice = await db.SalesInvoices
            .Include(i => i.Customer).Include(i => i.Vehicle)
            .Include(i => i.Lines).ThenInclude(l => l.Part)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Invoice not found" };
        if (invoice.Status == SalesInvoiceStatus.Void)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Invoice already voided" };

        invoice.Status = SalesInvoiceStatus.Void;
        foreach (var line in invoice.Lines)
        {
            line.Part.StockQuantity += line.Quantity;
            stock.Record(line.PartId, line.Quantity, StockMovementReason.Void, invoice.Id);
        }

        await db.SaveChangesAsync();
        return new ApiResponse<SalesInvoiceDto> { Success = true, Message = "Invoice voided, stock restored", Data = ToDto(invoice) };
    }

    private async Task<SalesInvoiceDto> LoadDtoAsync(Guid id)
    {
        var invoice = await db.SalesInvoices
            .Include(i => i.Customer).Include(i => i.Vehicle)
            .Include(i => i.Lines).ThenInclude(l => l.Part)
            .FirstAsync(i => i.Id == id);
        return ToDto(invoice);
    }

    private static SalesInvoiceDto ToDto(SalesInvoices i) => new()
    {
        Id = i.Id,
        InvoiceNumber = i.InvoiceNumber,
        CustomerId = i.CustomerId,
        CustomerName = i.Customer?.FullName ?? "",
        VehicleId = i.VehicleId,
        VehicleNumber = i.Vehicle?.VehicleNumber,
        CreatedByUserId = i.CreatedByUserId,
        Subtotal = i.Subtotal,
        Discount = i.Discount,
        DiscountRate = i.DiscountRate,
        ServiceCharge = i.ServiceCharge,
        Tax = i.Tax,
        TaxRate = i.TaxRate,
        Total = i.Total,
        Status = i.Status.ToString(),
        IssuedAt = i.IssuedAt,
        DueAt = i.DueAt,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt,
        Lines = i.Lines.Select(l => new SalesInvoiceLineDto
        {
            Id = l.Id,
            PartId = l.PartId,
            PartName = l.Part?.Name ?? "",
            Sku = l.Part?.Sku ?? "",
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            LineTotal = l.LineTotal,
        }).ToList()
    };
}
