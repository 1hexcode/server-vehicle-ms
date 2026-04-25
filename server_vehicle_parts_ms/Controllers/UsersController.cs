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
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto userCreateDto)
    {
        var response = await userService.CreateUserAsync(userCreateDto);
        return Ok(response);
    }

    [HttpGet]
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