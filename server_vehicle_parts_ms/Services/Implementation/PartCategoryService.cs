using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class PartCategoryService(AppDbContext db)
{
    public async Task<ApiResponse<PartCategoryDto>> CreateAsync(PartCategoryRequestDto dto)
    {
        if (dto.ParentId.HasValue && !await db.PartCategories.AnyAsync(c => c.Id == dto.ParentId.Value))
            return new ApiResponse<PartCategoryDto> { Success = false, Message = "Parent category not found" };

        var cat = new PartCategories
        {
            Name = dto.Name,
            VehicleType = dto.VehicleType,
            ParentId = dto.ParentId
        };
        db.PartCategories.Add(cat);
        await db.SaveChangesAsync();
        return new ApiResponse<PartCategoryDto> { Success = true, Message = "Category created", Data = ToDto(cat) };
    }

    public async Task<ApiResponse<List<PartCategoryDto>>> ListAsync(VehicleType? vehicleType)
    {
        var q = db.PartCategories.AsQueryable();
        if (vehicleType.HasValue) q = q.Where(c => c.VehicleType == vehicleType.Value);
        var items = await q.OrderBy(c => c.Name).ToListAsync();
        return new ApiResponse<List<PartCategoryDto>> { Success = true, Data = items.Select(ToDto).ToList() };
    }

    public async Task<ApiResponse<PartCategoryDto>> GetAsync(Guid id)
    {
        var cat = await db.PartCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return new ApiResponse<PartCategoryDto> { Success = false, Message = "Category not found" };
        return new ApiResponse<PartCategoryDto> { Success = true, Data = ToDto(cat) };
    }

    public async Task<ApiResponse<PartCategoryDto>> UpdateAsync(Guid id, PartCategoryRequestDto dto)
    {
        var cat = await db.PartCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return new ApiResponse<PartCategoryDto> { Success = false, Message = "Category not found" };

        if (dto.ParentId == cat.Id)
            return new ApiResponse<PartCategoryDto> { Success = false, Message = "A category cannot be its own parent" };
        if (dto.ParentId.HasValue && !await db.PartCategories.AnyAsync(c => c.Id == dto.ParentId.Value))
            return new ApiResponse<PartCategoryDto> { Success = false, Message = "Parent category not found" };

        cat.Name = dto.Name;
        cat.VehicleType = dto.VehicleType;
        cat.ParentId = dto.ParentId;
        await db.SaveChangesAsync();
        return new ApiResponse<PartCategoryDto> { Success = true, Message = "Category updated", Data = ToDto(cat) };
    }

    public async Task<ApiResponse<string>> DeleteAsync(Guid id)
    {
        var cat = await db.PartCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat == null) return new ApiResponse<string> { Success = false, Message = "Category not found" };
        if (await db.VehicleParts.AnyAsync(p => p.CategoryId == id))
            return new ApiResponse<string> { Success = false, Message = "Cannot delete category in use by parts" };
        if (await db.PartCategories.AnyAsync(c => c.ParentId == id))
            return new ApiResponse<string> { Success = false, Message = "Cannot delete category with sub-categories" };
        db.PartCategories.Remove(cat);
        await db.SaveChangesAsync();
        return new ApiResponse<string> { Success = true, Message = "Category deleted" };
    }

    private static PartCategoryDto ToDto(PartCategories c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        VehicleType = c.VehicleType.ToString(),
        ParentId = c.ParentId,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
