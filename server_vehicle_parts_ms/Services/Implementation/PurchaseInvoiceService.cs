using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class PurchaseInvoiceService(AppDbContext db, StockMovementService stock)
{
    public async Task<ApiResponse<PurchaseInvoiceDto>> CreateAsync(PurchaseInvoiceRequestDto dto, Guid createdBy)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == dto.VendorId);
        if (vendor == null)
            return new ApiResponse<PurchaseInvoiceDto> { Success = false, Message = "Vendor not found" };

        var partIds = dto.Items.Select(i => i.VehiclePartId).Distinct().ToList();
        var parts = await db.VehicleParts.Where(p => partIds.Contains(p.Id)).ToListAsync();
        if (parts.Count != partIds.Count)
            return new ApiResponse<PurchaseInvoiceDto> { Success = false, Message = "One or more parts not found" };

        var partLookup = parts.ToDictionary(p => p.Id);
        var invoice = new PurchaseInvoices
        {
            InvoiceNumber = $"PUR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            VendorId = vendor.Id,
            ReceivedAt = dto.ReceivedAt ?? DateTimeOffset.UtcNow,
            Tax = dto.Tax,
            Status = PurchaseInvoiceStatus.Posted,
            Notes = dto.Notes,
            CreatedById = createdBy,
        };

        decimal subtotal = 0;
        foreach (var item in dto.Items)
        {
            var part = partLookup[item.VehiclePartId];
            var lineTotal = item.UnitCost * item.Quantity;
            invoice.Items.Add(new PurchaseInvoiceItems
            {
                VehiclePartId = part.Id,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                LineTotal = lineTotal,
            });
            part.StockQuantity += item.Quantity;
            part.CostPrice = item.UnitCost; // last known cost
            subtotal += lineTotal;
        }
        invoice.Subtotal = subtotal;
        invoice.Total = subtotal + dto.Tax;

        db.PurchaseInvoices.Add(invoice);

        foreach (var item in invoice.Items)
            stock.Record(item.VehiclePartId, item.Quantity, StockMovementReason.PurchaseInvoice, invoice.Id);

        await db.SaveChangesAsync();

        return new ApiResponse<PurchaseInvoiceDto>
        {
            Success = true,
            Message = "Purchase invoice posted",
            Data = ToDto(invoice, vendor, partLookup)
        };
    }

    public async Task<ApiResponse<List<PurchaseInvoiceDto>>> ListAsync()
    {
        var invoices = await db.PurchaseInvoices
            .Include(i => i.Vendor)
            .Include(i => i.Items).ThenInclude(li => li.VehiclePart)
            .OrderByDescending(i => i.ReceivedAt)
            .ToListAsync();

        return new ApiResponse<List<PurchaseInvoiceDto>>
        {
            Success = true,
            Data = invoices.Select(i => ToDto(i, i.Vendor, null)).ToList()
        };
    }

    public async Task<ApiResponse<PurchaseInvoiceDto>> GetAsync(Guid id)
    {
        var invoice = await db.PurchaseInvoices
            .Include(i => i.Vendor)
            .Include(i => i.Items).ThenInclude(li => li.VehiclePart)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null)
            return new ApiResponse<PurchaseInvoiceDto> { Success = false, Message = "Invoice not found" };
        return new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = ToDto(invoice, invoice.Vendor, null) };
    }

    private static PurchaseInvoiceDto ToDto(PurchaseInvoices i, Vendors vendor, Dictionary<Guid, VehicleParts>? partLookup)
        => new()
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            VendorId = i.VendorId,
            VendorName = vendor.Name,
            ReceivedAt = i.ReceivedAt,
            Subtotal = i.Subtotal,
            Tax = i.Tax,
            Total = i.Total,
            Status = i.Status.ToString(),
            Notes = i.Notes,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt,
            Items = i.Items.Select(li => new PurchaseInvoiceItemDto
            {
                Id = li.Id,
                VehiclePartId = li.VehiclePartId,
                VehiclePartName = li.VehiclePart?.Name ?? partLookup?[li.VehiclePartId].Name ?? "",
                Sku = li.VehiclePart?.Sku ?? partLookup?[li.VehiclePartId].Sku ?? "",
                Quantity = li.Quantity,
                UnitCost = li.UnitCost,
                LineTotal = li.LineTotal,
            }).ToList()
        };
}
