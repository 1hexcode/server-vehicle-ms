using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Interface;

namespace server_vehicle_parts_ms.Services.Implementation;

public class LoginService
{
    private readonly AppDbContext _dbContext;
    private readonly TokenService _tokenService;
    private readonly IEmailService _emailService;

    public LoginService(AppDbContext dbContext, TokenService tokenService, IEmailService emailService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _emailService = emailService;
    }
    public async Task<ApiResponse<string>> LoginAsync(LoginDto loginDto)
    {
        Console.WriteLine($"[LoginService] Login attempt for: {loginDto.Email}");
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null)
        {
            Console.WriteLine("[LoginService] User not found.");
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var result = new PasswordHasher<Users>().VerifyHashedPassword(user, user.Password, loginDto.Password);

        if (result != PasswordVerificationResult.Success)
        {
            Console.WriteLine("[LoginService] Password verification failed.");
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        if (!user.isActive)
        {
            Console.WriteLine("[LoginService] Account is disabled.");
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Account is disabled"
            };
        }

        if (!user.IsEmailVerified)
        {
            Console.WriteLine("[LoginService] Email not verified.");
            return new ApiResponse<string>
            {
                Success = false,
                Message = "EMAIL_NOT_VERIFIED",
                Data = user.Email
            };
        }

        var token = _tokenService.CreateToken(user);
        return new ApiResponse<string>
        {
            Success = true,
            Data = token,
            Message = "Login successful"
        };
    }

    public async Task<ApiResponse<string>> VerifyLoginOtpAsync(VerifyOtpDto dto)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.EmailVerificationToken == dto.Otp);

        if (user == null)
        {
            return new ApiResponse<string> { Success = false, Message = "Invalid OTP" };
        }

        if (user.TokenExpiry < DateTime.UtcNow)
        {
            return new ApiResponse<string> { Success = false, Message = "OTP has expired" };
        }

        // Clear OTP after successful use
        user.EmailVerificationToken = null;
        user.TokenExpiry = null;
        await _dbContext.SaveChangesAsync();

        var token = _tokenService.CreateToken(user);

        return new ApiResponse<string>
        {
            Success = true,
            Data = token,
            Message = "Login successful"
        };
    }
}