using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class SettingsService(AppDbContext db)
{
    public async Task<ApiResponse<SystemSettingDto>> GetAsync()
    {
        var settings = await db.SystemSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SystemSetting
            {
                Id = Guid.NewGuid(),
                SystemName = "VehicleHub Parts MS",
                ContactEmail = "support@vehiclehub.com",
                ContactPhone = "+977-9876543210",
                Address = "Kathmandu, Nepal",
                Currency = "Rs.",
                TaxRate = 13.0m
            };
            db.SystemSettings.Add(settings);
            await db.SaveChangesAsync();
        }

        return new ApiResponse<SystemSettingDto>
        {
            Success = true,
            Data = ToDto(settings)
        };
    }

    public async Task<ApiResponse<SystemSettingDto>> UpdateAsync(SystemSettingRequestDto dto)
    {
        var settings = await db.SystemSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SystemSetting { Id = Guid.NewGuid() };
            db.SystemSettings.Add(settings);
        }

        settings.SystemName = dto.SystemName;
        settings.ContactEmail = dto.ContactEmail;
        settings.ContactPhone = dto.ContactPhone;
        settings.Address = dto.Address;
        settings.Currency = dto.Currency;
        settings.TaxRate = dto.TaxRate;

        await db.SaveChangesAsync();

        return new ApiResponse<SystemSettingDto>
        {
            Success = true,
            Message = "Settings updated successfully",
            Data = ToDto(settings)
        };
    }

    private static SystemSettingDto ToDto(SystemSetting s) => new()
    {
        Id = s.Id,
        SystemName = s.SystemName,
        ContactEmail = s.ContactEmail,
        ContactPhone = s.ContactPhone,
        Address = s.Address,
        Currency = s.Currency,
        TaxRate = s.TaxRate,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
