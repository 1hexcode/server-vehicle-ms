using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class HotDealService(AppDbContext db)
{
    // Public listing - only deals that are within their time window and not manually disabled.
    public async Task<ApiResponse<List<HotDealDto>>> ListActiveAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var deals = await db.HotDeals
            .Include(d => d.Part).ThenInclude(p => p.Category)
            .Where(d => d.IsActive && d.StartsAt <= now && d.EndsAt > now && d.Part.IsActive)
            .OrderBy(d => d.EndsAt)
            .ToListAsync();
        return new ApiResponse<List<HotDealDto>> { Success = true, Data = deals.Select(ToDto).ToList() };
    }

    // Admin/staff listing - everything, including expired and disabled rows.
    public async Task<ApiResponse<List<HotDealDto>>> ListAllAsync()
    {
        var deals = await db.HotDeals
            .Include(d => d.Part).ThenInclude(p => p.Category)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        return new ApiResponse<List<HotDealDto>> { Success = true, Data = deals.Select(ToDto).ToList() };
    }

    public async Task<ApiResponse<HotDealDto>> GetAsync(Guid id)
    {
        var deal = await db.HotDeals
            .Include(d => d.Part).ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (deal == null) return new ApiResponse<HotDealDto> { Success = false, Message = "Hot deal not found" };
        return new ApiResponse<HotDealDto> { Success = true, Data = ToDto(deal) };
    }

    public async Task<ApiResponse<HotDealDto>> CreateAsync(HotDealRequestDto dto)
    {
        var err = await ValidateAsync(dto, null);
        if (err != null) return new ApiResponse<HotDealDto> { Success = false, Message = err };

        var deal = new HotDeals
        {
            PartId = dto.PartId,
            DealPrice = dto.DealPrice,
            StartsAt = dto.StartsAt,
            EndsAt = dto.EndsAt,
            IsActive = dto.IsActive
        };
        db.HotDeals.Add(deal);
        await db.SaveChangesAsync();
        return new ApiResponse<HotDealDto> { Success = true, Message = "Hot deal created", Data = await LoadDtoAsync(deal.Id) };
    }

    public async Task<ApiResponse<HotDealDto>> UpdateAsync(Guid id, HotDealRequestDto dto)
    {
        var deal = await db.HotDeals.FirstOrDefaultAsync(d => d.Id == id);
        if (deal == null) return new ApiResponse<HotDealDto> { Success = false, Message = "Hot deal not found" };

        var err = await ValidateAsync(dto, id);
        if (err != null) return new ApiResponse<HotDealDto> { Success = false, Message = err };

        deal.PartId = dto.PartId;
        deal.DealPrice = dto.DealPrice;
        deal.StartsAt = dto.StartsAt;
        deal.EndsAt = dto.EndsAt;
        deal.IsActive = dto.IsActive;
        await db.SaveChangesAsync();
        return new ApiResponse<HotDealDto> { Success = true, Message = "Hot deal updated", Data = await LoadDtoAsync(deal.Id) };
    }

    public async Task<ApiResponse<string>> DeleteAsync(Guid id)
    {
        var deal = await db.HotDeals.FirstOrDefaultAsync(d => d.Id == id);
        if (deal == null) return new ApiResponse<string> { Success = false, Message = "Hot deal not found" };
        db.HotDeals.Remove(deal);
        await db.SaveChangesAsync();
        return new ApiResponse<string> { Success = true, Message = "Hot deal deleted" };
    }

    private async Task<string?> ValidateAsync(HotDealRequestDto dto, Guid? existingDealId)
    {
        if (dto.EndsAt <= dto.StartsAt) return "EndsAt must be after StartsAt";

        var part = await db.VehicleParts.FirstOrDefaultAsync(p => p.Id == dto.PartId);
        if (part == null) return "Part not found";
        if (dto.DealPrice >= part.UnitPrice)
            return $"Deal price ({dto.DealPrice}) must be less than the part's unit price ({part.UnitPrice})";

        // One active deal per part - prevents two overlapping promos competing on the homepage.
        var conflictingDeal = await db.HotDeals
            .Where(d => d.PartId == dto.PartId && d.IsActive && d.EndsAt > DateTimeOffset.UtcNow)
            .Where(d => existingDealId == null || d.Id != existingDealId)
            .AnyAsync();
        if (conflictingDeal) return "Another active hot deal already exists for this part";

        return null;
    }

    private async Task<HotDealDto> LoadDtoAsync(Guid id)
    {
        var deal = await db.HotDeals
            .Include(d => d.Part).ThenInclude(p => p.Category)
            .FirstAsync(d => d.Id == id);
        return ToDto(deal);
    }

    private static HotDealDto ToDto(HotDeals d)
    {
        var now = DateTimeOffset.UtcNow;
        var original = d.Part?.UnitPrice ?? 0m;
        var percent = original > 0 ? (int)Math.Round((1 - (d.DealPrice / original)) * 100) : 0;
        return new HotDealDto
        {
            Id = d.Id,
            PartId = d.PartId,
            PartName = d.Part?.Name ?? "",
            Sku = d.Part?.Sku ?? "",
            CategoryName = d.Part?.Category?.Name,
            ImageUrl = d.Part?.ImageUrl,
            OriginalPrice = original,
            DealPrice = d.DealPrice,
            DiscountPercent = percent,
            StartsAt = d.StartsAt,
            EndsAt = d.EndsAt,
            IsActive = d.IsActive,
            IsCurrentlyActive = d.IsActive && d.StartsAt <= now && d.EndsAt > now,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        };
    }
}
