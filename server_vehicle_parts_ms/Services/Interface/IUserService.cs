using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Interface;

public interface IUserService
{
    public Task<ApiResponse<UserCreateResponseDto>> CreateUserAsync(UserCreateDto userCreateDto);
    // public Task<ApiResponse<Users>> UpdateUserAsync(UserCreateDto userCreateDto);
    // public Task<ApiResponse<Users>> DeleteUserAsync(UserCreateDto userCreateDto);
    // public Task<ApiResponse<Users>> GetUsersAsync(UserCreateDto userCreateDto);
    // public Task<ApiResponse<Users>> GeUserByIdAsync(UserCreateDto userCreateDto);
}