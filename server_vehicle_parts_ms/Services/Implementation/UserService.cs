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

    public async Task<ApiResponse<IEnumerable<UserCreateResponseDto>>> GetAllStaffAsync()
    {
        var staff = await dbContext.Users
            .Where(u => u.Role == UserRoles.Staff)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserCreateResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                Role = u.Role.ToString(),
                IsActive = u.IsActive
            })
            .ToListAsync();

        return new ApiResponse<IEnumerable<UserCreateResponseDto>>
        {
            Success = true,
            Data = staff
        };
    }

    public async Task<ApiResponse<IEnumerable<UserCreateResponseDto>>> GetAllCustomersAsync()
    {
        var customers = await dbContext.Users
            .Where(u => u.Role == UserRoles.Customer)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserCreateResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                Role = u.Role.ToString(),
                IsActive = u.IsActive
            })
            .ToListAsync();

        return new ApiResponse<IEnumerable<UserCreateResponseDto>>
        {
            Success = true,
            Data = customers
        };
    }

    public async Task<ApiResponse<UserCreateResponseDto>> UpdateStaffAsync(Guid id, UpdateStaffDto dto)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null || user.Role != UserRoles.Staff)
            {
                return new ApiResponse<UserCreateResponseDto> { Success = false, Message = "Staff not found" };
            }

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.Address = dto.Address;
            user.IsActive = dto.IsActive;

            await dbContext.SaveChangesAsync();

            return new ApiResponse<UserCreateResponseDto>
            {
                Success = true,
                Message = "Staff updated successfully",
                Data = new UserCreateResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive
                }
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserCreateResponseDto>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

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

            user.IsActive = false;
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

    public async Task<ApiResponse<string>> ToggleUserStatusAsync(Guid id, bool isActive)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return new ApiResponse<string> { Success = false, Message = "User not found" };

            if (user.IsActive == isActive)
            {
                return new ApiResponse<string>
                {
                    Success = true,
                    Message = isActive ? "User is already active" : "User is already inactive",
                    Data = user.Id.ToString()
                };
            }

            user.IsActive = isActive;
            await dbContext.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Success = true,
                Message = isActive ? "User activated successfully" : "User deactivated successfully",
                Data = user.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ToggleUserStatus failure");
            return new ApiResponse<string> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<string>> ToggleCustomerStatusAsync(Guid id, bool isActive)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRoles.Customer);
            if (user == null)
                return new ApiResponse<string> { Success = false, Message = "Customer not found" };

            user.IsActive = isActive;
            await dbContext.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Success = true,
                Message = isActive ? "Customer activated successfully" : "Customer disabled successfully",
                Data = user.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ToggleCustomerStatus failure");
            return new ApiResponse<string> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<UserCreateResponseDto>> RegisterCustomerWithVehicleAsync(RegisterCustomerWithVehicleDto dto)
    {
        try
        {
            if (await dbContext.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return new ApiResponse<UserCreateResponseDto> { Success = false, Message = "Email already exists" };
            }

            var passwordHasher = new PasswordHasher<Users>();
            var user = new Users
            {
                Email       = dto.Email,
                FullName    = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Address     = dto.Address,
                // Admin-registered customers are trusted - no email verification needed
                IsActive        = true,
                IsEmailVerified = true,
                Role = UserRoles.Customer
            };
            user.Password = passwordHasher.HashPassword(user, dto.Password ?? "Customer@123");

            dbContext.Users.Add(user);
            
            var vehicle = new Vehicles
            {
                CustomerId = user.Id,
                VehicleNumber = dto.VehicleNumber,
                Type = dto.VehicleType,
                Make = dto.Make,
                Model = dto.Model,
                Year = dto.Year,
                Color = dto.Color
            };
            dbContext.Vehicles.Add(vehicle);

            await dbContext.SaveChangesAsync();

            jobs.Enqueue<EmailJobs>(j => j.SendWelcomeEmailAsync(user.Id, CancellationToken.None));

            return new ApiResponse<UserCreateResponseDto>
            {
                Success = true,
                Message = "Customer and vehicle registered successfully",
                Data = new UserCreateResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register customer with vehicle");
            return new ApiResponse<UserCreateResponseDto> { Success = false, Message = ex.Message };
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
            if (await dbContext.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
            {
                return new ApiResponse<UserCreateResponseDto>
                {
                    Success = false,
                    Message = "Phone number already exists"
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
                Email       = dto.Email,
                FullName    = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Address     = dto.Address,
                // Admin-created accounts are trusted - bypass email verification
                IsActive        = true,
                IsEmailVerified = true,
                Role = role
            };
            user.Password = passwordHasher.HashPassword(user, dto.Password);

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

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
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive
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
