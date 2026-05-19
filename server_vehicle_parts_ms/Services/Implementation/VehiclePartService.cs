using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class VehiclePartService(AppDbContext db)
{
    public async Task<ApiResponse<VehiclePartDto>> CreateAsync(VehiclePartRequestDto dto)
    {
        if (!await db.PartCategories.AnyAsync(c => c.Id == dto.CategoryId))
            return new ApiResponse<VehiclePartDto> { Success = false, Message = "Category not found" };
        if (await db.VehicleParts.AnyAsync(p => p.Sku == dto.Sku))
            return new ApiResponse<VehiclePartDto> { Success = false, Message = "SKU already exists" };

        var part = new VehicleParts
        {
            CategoryId = dto.CategoryId,
            VendorId = dto.VendorId,
            Name = dto.Name,
            Sku = dto.Sku,
            Description = dto.Description,
            CostPrice = dto.CostPrice,
            UnitPrice = dto.UnitPrice,
            StockQuantity = dto.StockQuantity,
            ReorderLevel = dto.ReorderLevel,
            IsActive = dto.IsActive,
            ImageUrl = dto.ImageUrl
        };
        db.VehicleParts.Add(part);

        // Add the total cost of initial stock to the vendor's due amount
        if (dto.StockQuantity > 0)
        {
            var vendor = await db.Vendors.FindAsync(dto.VendorId);
            if (vendor != null)
            {
                vendor.DueAmount += (dto.CostPrice * dto.StockQuantity);
            }
        }

        await db.SaveChangesAsync();
        return new ApiResponse<VehiclePartDto> { Success = true, Message = "Part created", Data = await LoadDtoAsync(part.Id) };
    }

    public async Task<ApiResponse<List<VehiclePartDto>>> ListAsync()
    {
        var parts = await db.VehicleParts
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .OrderBy(p => p.Name)
            .Select(p => new VehiclePartDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                VendorId = p.VendorId,
                VendorName = p.Vendor.Name,
                Name = p.Name,
                Sku = p.Sku,
                Description = p.Description,
                CostPrice = p.CostPrice,
                UnitPrice = p.UnitPrice,
                StockQuantity = p.StockQuantity,
                ReorderLevel = p.ReorderLevel,
                IsActive = p.IsActive,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();
            
        return new ApiResponse<List<VehiclePartDto>> { Success = true, Data = parts };
    }

    public async Task<ApiResponse<VehiclePartDto>> GetAsync(Guid id)
    {
        var part = await db.VehicleParts
            .Include(p => p.Category)
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (part == null) return new ApiResponse<VehiclePartDto> { Success = false, Message = "Part not found" };
        return new ApiResponse<VehiclePartDto> { Success = true, Data = ToDto(part) };
    }

    public async Task<ApiResponse<VehiclePartDto>> UpdateAsync(Guid id, VehiclePartRequestDto dto)
    {
        var part = await db.VehicleParts.FirstOrDefaultAsync(p => p.Id == id);
        if (part == null) return new ApiResponse<VehiclePartDto> { Success = false, Message = "Part not found" };

        if (part.CategoryId != dto.CategoryId &&
            !await db.PartCategories.AnyAsync(c => c.Id == dto.CategoryId))
            return new ApiResponse<VehiclePartDto> { Success = false, Message = "Category not found" };
        
        if (dto.VendorId.HasValue && !await db.Vendors.AnyAsync(v => v.Id == dto.VendorId.Value))
            return new ApiResponse<VehiclePartDto> { Success = false, Message = "Vendor not found" };

        if (part.Sku != dto.Sku && await db.VehicleParts.AnyAsync(p => p.Sku == dto.Sku))
            return new ApiResponse<VehiclePartDto> { Success = false, Message = "SKU already exists" };

        part.CategoryId = dto.CategoryId;
        part.VendorId = dto.VendorId;
        part.Name = dto.Name;
        part.Sku = dto.Sku;
        part.Description = dto.Description;
        part.CostPrice = dto.CostPrice;
        part.UnitPrice = dto.UnitPrice;
        part.StockQuantity = dto.StockQuantity;
        part.ReorderLevel = dto.ReorderLevel;
        part.IsActive = dto.IsActive;
        part.ImageUrl = dto.ImageUrl;
        await db.SaveChangesAsync();

        return new ApiResponse<VehiclePartDto> { Success = true, Message = "Part updated", Data = await LoadDtoAsync(part.Id) };
    }

    public async Task<ApiResponse<string>> DisableAsync(Guid id)
    {
        var part = await db.VehicleParts.FirstOrDefaultAsync(p => p.Id == id);
        if (part == null) return new ApiResponse<string> { Success = false, Message = "Part not found" };
        part.IsActive = false;
        await db.SaveChangesAsync();
        return new ApiResponse<string> { Success = true, Message = "Part disabled" };
    }

    private async Task<VehiclePartDto> LoadDtoAsync(Guid id)
    {
        var p = await db.VehicleParts
            .Include(x => x.Category)
            .Include(x => x.Vendor)
            .FirstAsync(x => x.Id == id);
        return ToDto(p);
    }

    public static VehiclePartDto ToDto(VehicleParts p) => new()
    {
        Id = p.Id,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name,
        VendorId = p.VendorId,
        VendorName = p.Vendor?.Name,
        Name = p.Name,
        Sku = p.Sku,
        Description = p.Description,
        CostPrice = p.CostPrice,
        UnitPrice = p.UnitPrice,
        StockQuantity = p.StockQuantity,
        ReorderLevel = p.ReorderLevel,
        IsActive = p.IsActive,
        ImageUrl = p.ImageUrl,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
