using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class ProfileService(AppDbContext db)
{
    public async Task<ApiResponse<ProfileDto>> GetAsync(Guid userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return new ApiResponse<ProfileDto> { Success = false, Message = "User not found" };
        return new ApiResponse<ProfileDto> { Success = true, Data = ToDto(user) };
    }

    public async Task<ApiResponse<ProfileDto>> UpdateAsync(Guid userId, ProfileUpdateDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return new ApiResponse<ProfileDto> { Success = false, Message = "User not found" };

        if (user.PhoneNumber != dto.PhoneNumber &&
            await db.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != userId))
        {
            return new ApiResponse<ProfileDto> { Success = false, Message = "Phone number already in use" };
        }

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.Address = dto.Address;
        await db.SaveChangesAsync();

        return new ApiResponse<ProfileDto> { Success = true, Message = "Profile updated", Data = ToDto(user) };
    }

    public async Task<ApiResponse<string>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return new ApiResponse<string> { Success = false, Message = "User not found" };

        var hasher = new PasswordHasher<Users>();
        var verify = hasher.VerifyHashedPassword(user, user.Password, dto.CurrentPassword);
        if (verify != PasswordVerificationResult.Success)
            return new ApiResponse<string> { Success = false, Message = "Current password is incorrect" };

        if (dto.NewPassword != dto.NewPasswordVerify)
            return new ApiResponse<string> { Success = false, Message = "New passwords do not match" };

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return new ApiResponse<string> { Success = false, Message = "New password must be at least 6 characters long" };

        if (dto.NewPassword == dto.CurrentPassword)
            return new ApiResponse<string> { Success = false, Message = "New password must differ from current" };

        user.Password = hasher.HashPassword(user, dto.NewPassword);
        await db.SaveChangesAsync();

        return new ApiResponse<string> { Success = true, Message = "Password changed" };
    }

    private static ProfileDto ToDto(Users u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        PhoneNumber = u.PhoneNumber,
        Address = u.Address,
        Role = u.Role.ToString(),
        IsActive = u.isActive,
        CreatedAt = u.CreatedAt
    };
}
