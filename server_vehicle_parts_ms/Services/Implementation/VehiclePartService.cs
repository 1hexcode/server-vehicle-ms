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
            Name = dto.Name,
            Sku = dto.Sku,
            Description = dto.Description,
            CostPrice = dto.CostPrice,
            UnitPrice = dto.UnitPrice,
            StockQuantity = dto.StockQuantity,
            ReorderLevel = dto.ReorderLevel,
        };
        db.VehicleParts.Add(part);
        await db.SaveChangesAsync();
        return new ApiResponse<VehiclePartDto> { Success = true, Message = "Part created", Data = await LoadDtoAsync(part.Id) };
    }

    public async Task<ApiResponse<List<VehiclePartDto>>> ListAsync()
    {
        var parts = await db.VehicleParts.Include(p => p.Category).OrderBy(p => p.Name).ToListAsync();
        return new ApiResponse<List<VehiclePartDto>> { Success = true, Data = parts.Select(ToDto).ToList() };
    }

    public async Task<ApiResponse<VehiclePartDto>> GetAsync(Guid id)
    {
        var part = await db.VehicleParts.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
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
        if (part.Sku != dto.Sku && await db.VehicleParts.AnyAsync(p => p.Sku == dto.Sku))
            return new ApiResponse<VehiclePartDto> { Success = false, Message = "SKU already exists" };

        part.CategoryId = dto.CategoryId;
        part.Name = dto.Name;
        part.Sku = dto.Sku;
        part.Description = dto.Description;
        part.CostPrice = dto.CostPrice;
        part.UnitPrice = dto.UnitPrice;
        part.StockQuantity = dto.StockQuantity;
        part.ReorderLevel = dto.ReorderLevel;
        await db.SaveChangesAsync();

        return new ApiResponse<VehiclePartDto> { Success = true, Message = "Part updated", Data = await LoadDtoAsync(part.Id) };
    }

    public async Task<ApiResponse<string>> DisableAsync(Guid id)
    {
        var part = await db.VehicleParts.FirstOrDefaultAsync(p => p.Id == id);
        if (part == null) return new ApiResponse<string> { Success = false, Message = "Part not found" };
        part.isActive = false;
        await db.SaveChangesAsync();
        return new ApiResponse<string> { Success = true, Message = "Part disabled" };
    }

    private async Task<VehiclePartDto> LoadDtoAsync(Guid id)
    {
        var p = await db.VehicleParts.Include(x => x.Category).FirstAsync(x => x.Id == id);
        return ToDto(p);
    }

    public static VehiclePartDto ToDto(VehicleParts p) => new()
    {
        Id = p.Id,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name,
        Name = p.Name,
        Sku = p.Sku,
        Description = p.Description,
        CostPrice = p.CostPrice,
        UnitPrice = p.UnitPrice,
        StockQuantity = p.StockQuantity,
        ReorderLevel = p.ReorderLevel,
        IsActive = p.isActive,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
