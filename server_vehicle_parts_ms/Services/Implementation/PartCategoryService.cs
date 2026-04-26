using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;
using server_vehicle_parts_ms.Helpers;

namespace server_vehicle_parts_ms.Services.Implementation;

public class PartCategoryService(AppDbContext db, ICacheService cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private static string ItemKey(Guid id) => $"part_category:{id}";
    private static string ListKey(VehicleType? vt) => $"part_category:list:{vt?.ToString() ?? "all"}";

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
        await InvalidateListsAsync();
        return new ApiResponse<PartCategoryDto> { Success = true, Message = "Category created", Data = ToDto(cat) };
    }

    public async Task<ApiResponse<List<PartCategoryDto>>> ListAsync(VehicleType? vehicleType)
    {
        var data = await cache.GetOrSetAsync(ListKey(vehicleType), CacheTtl, async () =>
        {
            var q = db.PartCategories.AsQueryable();
            if (vehicleType.HasValue) q = q.Where(c => c.VehicleType == vehicleType.Value);
            var items = await q.OrderBy(c => c.Name).ToListAsync();
            return items.Select(ToDto).ToList();
        });
        return new ApiResponse<List<PartCategoryDto>> { Success = true, Data = data };
    }

    public async Task<ApiResponse<PartCategoryDto>> GetAsync(Guid id)
    {
        var data = await cache.GetOrSetAsync(ItemKey(id), CacheTtl, async () =>
        {
            var cat = await db.PartCategories.FirstOrDefaultAsync(c => c.Id == id);
            return cat == null ? null : ToDto(cat);
        });
        if (data == null) return new ApiResponse<PartCategoryDto> { Success = false, Message = "Category not found" };
        return new ApiResponse<PartCategoryDto> { Success = true, Data = data };
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
        await cache.RemoveAsync(ItemKey(id));
        await InvalidateListsAsync();
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
        await cache.RemoveAsync(ItemKey(id));
        await InvalidateListsAsync();
        return new ApiResponse<string> { Success = true, Message = "Category deleted" };
    }

    private async Task InvalidateListsAsync()
    {
        await cache.RemoveAsync(ListKey(null));
        foreach (var vt in Enum.GetValues<VehicleType>())
            await cache.RemoveAsync(ListKey(vt));
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
