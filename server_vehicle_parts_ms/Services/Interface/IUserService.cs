using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Interface;

public interface IUserService
{
    public Task<ApiResponse<UserCreateResponseDto>> CreateStaffAsync(RegisterUserDto dto);
    public Task<ApiResponse<UserCreateResponseDto>> CreateCustomerAsync(RegisterUserDto dto);
    public Task<ApiResponse<IEnumerable<UserCreateResponseDto>>> GetAllStaffAsync();
    public Task<ApiResponse<UserCreateResponseDto>> UpdateStaffAsync(Guid id, UpdateStaffDto dto);
    public Task<ApiResponse<string>> DisableStaffAsync(Guid id);
}
