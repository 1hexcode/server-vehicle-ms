using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Interface;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Staff")]
public class CustomersController(IUserService userService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterUserDto dto)
    {
        Console.WriteLine($"[CustomersController] Registration request received for: {dto.Email}");
        var response = await userService.CreateCustomerAsync(dto);
        return Ok(response);
    }
}
