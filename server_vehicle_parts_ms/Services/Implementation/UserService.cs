using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;
using server_vehicle_parts_ms.Services.Interface;

namespace server_vehicle_parts_ms.Services.Implementation;

public class UserService(AppDbContext dbContext, IEmailService emailService, IConfiguration configuration): IUserService
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
            Console.WriteLine(ex.Message);
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
            var otp = new Random().Next(100000, 999999).ToString();
            var user = new Users
            {
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                isActive = true,
                Role = role,
                IsEmailVerified = false, // Must verify now
                EmailVerificationToken = otp,
                TokenExpiry = DateTime.UtcNow.AddMinutes(15)
            };
            user.Password = passwordHasher.HashPassword(user, dto.Password);

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            await emailService.SendEmailAsync(
                user.Email,
                "Verify your account",
                $"Your verification code is: {otp}"
            );

            return new ApiResponse<UserCreateResponseDto>
            {
                Success = true,
                Message = "Registration successful. Please verify your email.",
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
            Console.WriteLine(ex.Message);
            return new ApiResponse<UserCreateResponseDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private static string ResolveBaseUrl(IConfiguration configuration)
    {
        var configuredBaseUrl = configuration["App:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl.TrimEnd('/');
        }

        var configuredUrls = configuration["ASPNETCORE_URLS"]
                             ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                             ?? Environment.GetEnvironmentVariable("DOTNET_URLS");

        if (!string.IsNullOrWhiteSpace(configuredUrls))
        {
            var firstUrl = configuredUrls
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstUrl))
            {
                return firstUrl.TrimEnd('/');
            }
        }

        return "http://localhost:5091";
    }

    public async Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpDto dto)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.EmailVerificationToken == dto.Otp);

        if (user == null)
        {
            return new ApiResponse<string> { Success = false, Message = "Invalid verification code" };
        }

        if (user.TokenExpiry < DateTime.UtcNow)
        {
            return new ApiResponse<string> { Success = false, Message = "Verification code has expired" };
        }

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.TokenExpiry = null;
        await dbContext.SaveChangesAsync();

        return new ApiResponse<string> { Success = true, Message = "Email verified successfully" };
    }

    public async Task<ApiResponse<string>> ResendOtpAsync(string email)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return new ApiResponse<string> { Success = false, Message = "User not found" };

        var otp = new Random().Next(100000, 999999).ToString();
        user.EmailVerificationToken = otp;
        user.TokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await dbContext.SaveChangesAsync();

        await emailService.SendEmailAsync(user.Email, "Verify your account", $"Your verification code is: {otp}");
        return new ApiResponse<string> { Success = true, Message = "Verification code resent" };
    }
}
