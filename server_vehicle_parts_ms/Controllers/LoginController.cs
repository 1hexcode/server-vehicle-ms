using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("auth-strict")]
public class LoginController(LoginService loginService): ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var response = await loginService.LoginAsync(loginDto);
        return Ok(response);
    }
}