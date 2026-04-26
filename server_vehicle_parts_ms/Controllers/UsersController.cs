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
        var response = await userService.CreateCustomerAsync(dto);
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