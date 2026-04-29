using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class VendorService(AppDbContext db)
{
    public async Task<ApiResponse<VendorDto>> CreateAsync(VendorRequestDto dto)
    {
        var vendor = new Vendors
        {
            Name = dto.Name,
            ContactPerson = dto.ContactPerson,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            OpeningBalance = dto.OpeningBalance,
            DueAmount = dto.OpeningBalance, // DueAmount starts at OpeningBalance
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();
        return new ApiResponse<VendorDto>
        {
            Success = true,
            Message = "Vendor created",
            Data = await ToDtoAsync(vendor)
        };
    }

    public async Task<ApiResponse<List<VendorDto>>> ListAsync()
    {
        var vendors = await db.Vendors
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .ToListAsync();

        var vendorIds = vendors.Select(v => v.Id).ToList();
        var paymentSums = await db.VendorPayments
            .Where(p => vendorIds.Contains(p.VendorId))
            .GroupBy(p => p.VendorId)
            .Select(g => new { VendorId = g.Key, TotalPaid = g.Sum(p => p.Amount) })
            .ToListAsync();

        var paymentMap = paymentSums.ToDictionary(x => x.VendorId, x => x.TotalPaid);

        var dtos = vendors.Select(v => new VendorDto
        {
            Id = v.Id,
            Name = v.Name,
            ContactPerson = v.ContactPerson,
            Email = v.Email,
            Phone = v.Phone,
            Address = v.Address,
            OpeningBalance = v.OpeningBalance,
            DueAmount = v.DueAmount,
            TotalPaid = paymentMap.GetValueOrDefault(v.Id, 0m),
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt
        }).ToList();

        return new ApiResponse<List<VendorDto>>
        {
            Success = true,
            Data = dtos
        };
    }

    public async Task<ApiResponse<VendorDto>> GetAsync(Guid id)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null)
            return new ApiResponse<VendorDto> { Success = false, Message = "Vendor not found" };
        return new ApiResponse<VendorDto> { Success = true, Data = await ToDtoAsync(vendor) };
    }

    public async Task<ApiResponse<VendorDto>> UpdateAsync(Guid id, VendorRequestDto dto)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null)
            return new ApiResponse<VendorDto> { Success = false, Message = "Vendor not found" };

        vendor.Name = dto.Name;
        vendor.ContactPerson = dto.ContactPerson;
        vendor.Email = dto.Email;
        vendor.Phone = dto.Phone;
        vendor.Address = dto.Address;
        await db.SaveChangesAsync();

        return new ApiResponse<VendorDto> { Success = true, Message = "Vendor updated", Data = await ToDtoAsync(vendor) };
    }

    public async Task<ApiResponse<string>> DisableAsync(Guid id)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null)
            return new ApiResponse<string> { Success = false, Message = "Vendor not found" };
        vendor.IsActive = false;
        await db.SaveChangesAsync();
        return new ApiResponse<string> { Success = true, Message = "Vendor disabled" };
    }

    // --- Add Parts (increases DueAmount) ---
    public async Task<ApiResponse<VendorDto>> AddPartsAsync(Guid vendorId, VendorAddPartsDto dto)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
        if (vendor == null)
            return new ApiResponse<VendorDto> { Success = false, Message = "Vendor not found" };

        var part = await db.VehicleParts.FirstOrDefaultAsync(p => p.Id == dto.PartId);
        if (part == null)
            return new ApiResponse<VendorDto> { Success = false, Message = "Part not found" };

        var totalCost = dto.Quantity * dto.PricePerUnit;
        vendor.DueAmount += totalCost;

        // Update the part's stock quantity and cost price
        part.StockQuantity += dto.Quantity;
        part.CostPrice = dto.PricePerUnit;

        await db.SaveChangesAsync();
        return new ApiResponse<VendorDto> { Success = true, Message = $"Added {dto.Quantity} units. Due increased by Rs. {totalCost}", Data = await ToDtoAsync(vendor) };
    }

    // --- Vendor Payments ---
    public async Task<ApiResponse<VendorPaymentDto>> CreatePaymentAsync(Guid vendorId, VendorPaymentRequestDto dto)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
        if (vendor == null)
            return new ApiResponse<VendorPaymentDto> { Success = false, Message = "Vendor not found" };

        if (!Enum.TryParse<PaymentType>(dto.Type, true, out var paymentType))
            return new ApiResponse<VendorPaymentDto> { Success = false, Message = "Invalid payment type. Use Cash, Card, or Online." };

        var payment = new VendorPayments
        {
            VendorId = vendorId,
            Amount = dto.Amount,
            Type = paymentType,
            AttachmentUrl = dto.AttachmentUrl,
            Notes = dto.Notes
        };

        vendor.DueAmount -= dto.Amount;
        db.VendorPayments.Add(payment);
        await db.SaveChangesAsync();

        return new ApiResponse<VendorPaymentDto>
        {
            Success = true,
            Message = $"Payment of Rs. {dto.Amount} recorded",
            Data = ToPaymentDto(payment)
        };
    }

    public async Task<ApiResponse<List<VendorPaymentDto>>> GetPaymentsAsync(Guid vendorId)
    {
        var exists = await db.Vendors.AnyAsync(v => v.Id == vendorId);
        if (!exists)
            return new ApiResponse<List<VendorPaymentDto>> { Success = false, Message = "Vendor not found" };

        var payments = await db.VendorPayments
            .Where(p => p.VendorId == vendorId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return new ApiResponse<List<VendorPaymentDto>>
        {
            Success = true,
            Data = payments.Select(ToPaymentDto).ToList()
        };
    }

    // --- Mapping ---
    private async Task<VendorDto> ToDtoAsync(Vendors v)
    {
        var totalPaid = await db.VendorPayments
            .Where(p => p.VendorId == v.Id)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        return new VendorDto
        {
            Id = v.Id,
            Name = v.Name,
            ContactPerson = v.ContactPerson,
            Email = v.Email,
            Phone = v.Phone,
            Address = v.Address,
            OpeningBalance = v.OpeningBalance,
            DueAmount = v.DueAmount,
            TotalPaid = totalPaid,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt
        };
    }

    private static VendorPaymentDto ToPaymentDto(VendorPayments p) => new()
    {
        Id = p.Id,
        VendorId = p.VendorId,
        Amount = p.Amount,
        Type = p.Type.ToString(),
        AttachmentUrl = p.AttachmentUrl,
        Notes = p.Notes,
        CreatedAt = p.CreatedAt
    };
}
