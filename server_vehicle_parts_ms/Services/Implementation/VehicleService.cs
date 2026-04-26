using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class VehicleService(AppDbContext db)
{
    public async Task<ApiResponse<VehicleDto>> CreateAsync(Guid customerId, VehicleRequestDto dto)
    {
        if (await db.Vehicles.AnyAsync(v => v.VehicleNumber == dto.VehicleNumber))
            return new ApiResponse<VehicleDto> { Success = false, Message = "Vehicle number already registered" };

        var vehicle = new Vehicles
        {
            CustomerId = customerId,
            VehicleNumber = dto.VehicleNumber,
            Type = dto.Type,
            Make = dto.Make,
            Model = dto.Model,
            Year = dto.Year,
            Color = dto.Color
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return new ApiResponse<VehicleDto> { Success = true, Message = "Vehicle registered", Data = ToDto(vehicle) };
    }

    public async Task<ApiResponse<List<VehicleDto>>> ListAsync(Guid? ownerFilter)
    {
        var q = db.Vehicles.AsQueryable();
        if (ownerFilter.HasValue) q = q.Where(v => v.CustomerId == ownerFilter.Value);
        var items = await q.OrderByDescending(v => v.CreatedAt).ToListAsync();
        return new ApiResponse<List<VehicleDto>> { Success = true, Data = items.Select(ToDto).ToList() };
    }

    public async Task<ApiResponse<VehicleDto>> GetAsync(Guid id, Guid? ownerFilter)
    {
        var v = await db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return new ApiResponse<VehicleDto> { Success = false, Message = "Vehicle not found" };
        if (ownerFilter.HasValue && v.CustomerId != ownerFilter.Value)
            return new ApiResponse<VehicleDto> { Success = false, Message = "Forbidden" };
        return new ApiResponse<VehicleDto> { Success = true, Data = ToDto(v) };
    }

    public async Task<ApiResponse<VehicleDto>> UpdateAsync(Guid id, Guid ownerId, VehicleRequestDto dto)
    {
        var v = await db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return new ApiResponse<VehicleDto> { Success = false, Message = "Vehicle not found" };
        if (v.CustomerId != ownerId)
            return new ApiResponse<VehicleDto> { Success = false, Message = "Forbidden" };

        if (v.VehicleNumber != dto.VehicleNumber &&
            await db.Vehicles.AnyAsync(x => x.VehicleNumber == dto.VehicleNumber))
            return new ApiResponse<VehicleDto> { Success = false, Message = "Vehicle number already registered" };

        v.VehicleNumber = dto.VehicleNumber;
        v.Type = dto.Type;
        v.Make = dto.Make;
        v.Model = dto.Model;
        v.Year = dto.Year;
        v.Color = dto.Color;
        await db.SaveChangesAsync();
        return new ApiResponse<VehicleDto> { Success = true, Message = "Vehicle updated", Data = ToDto(v) };
    }

    public async Task<ApiResponse<string>> DeleteAsync(Guid id, Guid ownerId)
    {
        var v = await db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return new ApiResponse<string> { Success = false, Message = "Vehicle not found" };
        if (v.CustomerId != ownerId)
            return new ApiResponse<string> { Success = false, Message = "Forbidden" };

        db.Vehicles.Remove(v);
        await db.SaveChangesAsync();
        return new ApiResponse<string> { Success = true, Message = "Vehicle removed" };
    }

    public static VehicleDto ToDto(Vehicles v) => new()
    {
        Id = v.Id,
        CustomerId = v.CustomerId,
        VehicleNumber = v.VehicleNumber,
        Type = v.Type.ToString(),
        Make = v.Make,
        Model = v.Model,
        Year = v.Year,
        Color = v.Color,
        CreatedAt = v.CreatedAt
    };
}
