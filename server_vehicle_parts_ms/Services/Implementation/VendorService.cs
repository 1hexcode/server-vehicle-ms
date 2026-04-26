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
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();
        return new ApiResponse<VendorDto>
        {
            Success = true,
            Message = "Vendor created",
            Data = ToDto(vendor)
        };
    }

    public async Task<ApiResponse<List<VendorDto>>> ListAsync()
    {
        var vendors = await db.Vendors.OrderBy(v => v.Name).ToListAsync();
        return new ApiResponse<List<VendorDto>>
        {
            Success = true,
            Data = vendors.Select(ToDto).ToList()
        };
    }

    public async Task<ApiResponse<VendorDto>> GetAsync(Guid id)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null)
            return new ApiResponse<VendorDto> { Success = false, Message = "Vendor not found" };
        return new ApiResponse<VendorDto> { Success = true, Data = ToDto(vendor) };
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

        return new ApiResponse<VendorDto> { Success = true, Message = "Vendor updated", Data = ToDto(vendor) };
    }

    public async Task<ApiResponse<string>> DisableAsync(Guid id)
    {
        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null)
            return new ApiResponse<string> { Success = false, Message = "Vendor not found" };
        vendor.isActive = false;
        await db.SaveChangesAsync();
        return new ApiResponse<string> { Success = true, Message = "Vendor disabled" };
    }

    private static VendorDto ToDto(Vendors v) => new()
    {
        Id = v.Id,
        Name = v.Name,
        ContactPerson = v.ContactPerson,
        Email = v.Email,
        Phone = v.Phone,
        Address = v.Address,
        IsActive = v.isActive,
        CreatedAt = v.CreatedAt
    };
}
