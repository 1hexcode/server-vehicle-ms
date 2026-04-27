using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Interface;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(IUserService userService): ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterUserDto dto)
    {
        Console.WriteLine($"[UsersController] Registration request received for: {dto.Email}");
        var response = await userService.CreateCustomerAsync(dto);
        return Ok(response);
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        var response = await userService.VerifyOtpAsync(dto);
        return Ok(response);
    }

    [HttpPost("resend-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendOtp([FromBody] string email)
    {
        var response = await userService.ResendOtpAsync(email);
        return Ok(response);
    }

    [HttpGet("/health")]
    [AllowAnonymous]
    public IActionResult HealthCheck()
    {
        var response = new ApiResponse<string>
        {
            Message = "Health Check OK",
            Success = true
        };
        return Ok(response);
    }
}