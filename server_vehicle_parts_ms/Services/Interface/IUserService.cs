using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Interface;

public interface IUserService
{
    public Task<ApiResponse<UserCreateResponseDto>> CreateStaffAsync(RegisterUserDto dto);
    public Task<ApiResponse<UserCreateResponseDto>> CreateCustomerAsync(RegisterUserDto dto);
    public Task<ApiResponse<string>> DisableStaffAsync(Guid id);
    public Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpDto dto);
    public Task<ApiResponse<string>> ResendOtpAsync(string email);
}

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}