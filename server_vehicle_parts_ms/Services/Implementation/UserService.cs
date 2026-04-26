using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;
using server_vehicle_parts_ms.Helpers;
using server_vehicle_parts_ms.Services.Interface;

namespace server_vehicle_parts_ms.Services.Implementation;

public class UserService(AppDbContext dbContext, IBackgroundJobClient jobs, ILogger<UserService> logger): IUserService
{
    public Task<ApiResponse<UserCreateResponseDto>> CreateStaffAsync(RegisterUserDto dto)
        => CreateWithRoleAsync(dto, UserRoles.Staff);

    public Task<ApiResponse<UserCreateResponseDto>> CreateCustomerAsync(RegisterUserDto dto)
        => CreateWithRoleAsync(dto, UserRoles.Customer);

    public async Task<ApiResponse<string>> DisableStaffAsync(Guid id)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return new ApiResponse<string> { Success = false, Message = "Staff not found" };
            }
            if (user.Role != UserRoles.Staff)
            {
                return new ApiResponse<string> { Success = false, Message = "User is not staff" };
            }

            user.isActive = false;
            await dbContext.SaveChangesAsync();

            return new ApiResponse<string> { Success = true, Message = "Staff disabled" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UserService failure");
            return new ApiResponse<string>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private async Task<ApiResponse<UserCreateResponseDto>> CreateWithRoleAsync(RegisterUserDto dto, UserRoles role)
    {
        try
        {
            if (await dbContext.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return new ApiResponse<UserCreateResponseDto>
                {
                    Success = false,
                    Message = "Email already exists"
                };
            }
            if (dto.Password != dto.PasswordVerify)
            {
                return new ApiResponse<UserCreateResponseDto>
                {
                    Success = false,
                    Message = "Passwords do not match",
                    Errors = new List<string> { "Password and Confirm Password must be the same" }
                };
            }
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            {
                return new ApiResponse<UserCreateResponseDto>
                {
                    Success = false,
                    Message = "Password must be at least 6 characters long"
                };
            }

            var passwordHasher = new PasswordHasher<Users>();
            var user = new Users
            {
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                isActive = true,
                Role = role
            };
            user.Password = passwordHasher.HashPassword(user, dto.Password);

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            if (role == UserRoles.Customer)
            {
                jobs.Enqueue<EmailJobs>(j => j.SendWelcomeEmailAsync(user.Id, CancellationToken.None));
            }

            return new ApiResponse<UserCreateResponseDto>
            {
                Success = true,
                Message = $"{role} created successfully",
                Data = new UserCreateResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Role = user.Role.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UserService failure");
            return new ApiResponse<UserCreateResponseDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
