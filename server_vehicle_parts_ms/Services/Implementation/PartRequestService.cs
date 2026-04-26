using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class PartRequestService(AppDbContext db, NotificationService notifications)
{
    public async Task<ApiResponse<PartRequestDto>> CreateAsync(Guid customerId, PartRequestRequestDto dto)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId);
        if (vehicle == null)
            return new ApiResponse<PartRequestDto> { Success = false, Message = "Vehicle not found" };
        if (vehicle.CustomerId != customerId)
            return new ApiResponse<PartRequestDto> { Success = false, Message = "Vehicle does not belong to you" };

        var pr = new PartRequests
        {
            CustomerId = customerId,
            VehicleId = vehicle.Id,
            Description = dto.Description,
            Status = PartRequestStatus.Requested,
        };
        db.PartRequests.Add(pr);
        await db.SaveChangesAsync();
        return new ApiResponse<PartRequestDto> { Success = true, Message = "Part request raised", Data = await LoadDtoAsync(pr.Id) };
    }

    public async Task<ApiResponse<List<PartRequestDto>>> ListAsync(Guid? customerFilter)
    {
        var q = db.PartRequests
            .Include(p => p.Customer)
            .Include(p => p.Vehicle)
            .Include(p => p.HandledBy)
            .AsQueryable();
        if (customerFilter.HasValue) q = q.Where(p => p.CustomerId == customerFilter.Value);
        var items = await q.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return new ApiResponse<List<PartRequestDto>> { Success = true, Data = items.Select(ToDto).ToList() };
    }

    public async Task<ApiResponse<PartRequestDto>> GetAsync(Guid id, Guid? customerFilter)
    {
        var pr = await db.PartRequests
            .Include(p => p.Customer).Include(p => p.Vehicle).Include(p => p.HandledBy)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pr == null) return new ApiResponse<PartRequestDto> { Success = false, Message = "Part request not found" };
        if (customerFilter.HasValue && pr.CustomerId != customerFilter.Value)
            return new ApiResponse<PartRequestDto> { Success = false, Message = "Forbidden" };
        return new ApiResponse<PartRequestDto> { Success = true, Data = ToDto(pr) };
    }

    public async Task<ApiResponse<PartRequestDto>> UpdateStatusAsync(Guid id, Guid adminUserId, PartRequestStatusUpdateDto dto)
    {
        var pr = await db.PartRequests
            .Include(p => p.Customer).Include(p => p.Vehicle).Include(p => p.HandledBy)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pr == null) return new ApiResponse<PartRequestDto> { Success = false, Message = "Part request not found" };

        pr.Status = dto.Status;
        pr.HandledByUserId = adminUserId;
        pr.UpdatedAt = DateTimeOffset.UtcNow;

        await notifications.EnqueueAsync(
            pr.CustomerId,
            $"Part request {dto.Status}",
            $"Your part request status changed to {dto.Status}.",
            NotificationType.Generic);

        await db.SaveChangesAsync();
        return new ApiResponse<PartRequestDto> { Success = true, Message = "Status updated", Data = ToDto(pr) };
    }

    private async Task<PartRequestDto> LoadDtoAsync(Guid id)
    {
        var pr = await db.PartRequests
            .Include(p => p.Customer).Include(p => p.Vehicle).Include(p => p.HandledBy)
            .FirstAsync(p => p.Id == id);
        return ToDto(pr);
    }

    public static PartRequestDto ToDto(PartRequests p) => new()
    {
        Id = p.Id,
        CustomerId = p.CustomerId,
        CustomerName = p.Customer?.FullName ?? "",
        VehicleId = p.VehicleId,
        VehicleNumber = p.Vehicle?.VehicleNumber ?? "",
        Description = p.Description,
        Status = p.Status.ToString(),
        HandledByUserId = p.HandledByUserId,
        HandledByName = p.HandledBy?.FullName,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
