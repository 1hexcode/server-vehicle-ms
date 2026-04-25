using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;
using server_vehicle_parts_ms.Services.Interface;

namespace server_vehicle_parts_ms.Services.Implementation;

public class UserService(AppDbContext dbContext): IUserService
{
    public async Task<ApiResponse<UserCreateResponseDto>> CreateUserAsync(UserCreateDto userCreateDto)
    {
        try
        {
            if (await dbContext.Users.AnyAsync(u => u.Email == userCreateDto.Email))
            {
                return new ApiResponse<UserCreateResponseDto>
                {
                    Success = false,
                    Message = "Email already exists"
                };
            }
            // Validate password match
            if (userCreateDto.Password != userCreateDto.PasswordVerify)
            {
                return new ApiResponse<UserCreateResponseDto>
                {
                    Success = false,
                    Message = "Passwords do not match",
                    Errors = new List<string> { "Password and Confirm Password must be the same" }
                };
            }
            
            // Validate strength
            if (string.IsNullOrWhiteSpace(userCreateDto.Password) || userCreateDto.Password.Length < 6)
            {
                return new ApiResponse<UserCreateResponseDto>
                {
                    Success = false,
                    Message = "Password must be at least 6 characters long"
                };
            }
            var passwordHasher = new PasswordHasher<Users>();
            
            Users user = new Users
            {
                Email = userCreateDto.Email,
                Password = userCreateDto.Password,
                FullName = userCreateDto.FullName,
                PhoneNumber = userCreateDto.PhoneNumber,
                Address = userCreateDto.Address,
                isActive = true,
                Role = userCreateDto.Role
            };
            
            // Hash password
            user.Password = passwordHasher.HashPassword(user, userCreateDto.Password);
            
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            
            var response = new UserCreateResponseDto()
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = user.Role.ToString()
            };
            
            return new ApiResponse<UserCreateResponseDto>
            {
                Success = true,
                Data = response,
                Message = "User created successfully"
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
}