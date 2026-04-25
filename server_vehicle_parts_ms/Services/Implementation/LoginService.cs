using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;


namespace server_vehicle_parts_ms.Services.Implementation;

public class LoginService
{
    private readonly AppDbContext _dbContext;
    private readonly TokenService _tokenService;
    
    public LoginService(AppDbContext dbContext,TokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }
    public async Task<ApiResponse<string>> LoginAsync(LoginDto loginDto)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null)
        {
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }
        
        var result = new PasswordHasher<Users>().VerifyHashedPassword(user, user.Password, loginDto.Password);

        if (result != PasswordVerificationResult.Success)
        {
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        var token = _tokenService.CreateToken(user);

        return new ApiResponse<string>()
        {
            Success = true,
            Data = token,
            Message = "Login successful"
        };
    }
}