using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Interface;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/Customers")]
[ApiController]
[Authorize(Roles = "Admin,Staff")]
public class CustomersController(IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] RegisterUserDto dto)
    {
        var response = await userService.CreateCustomerAsync(dto);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> ListCustomers()
    {
        var response = await userService.GetAllCustomersAsync();
        return Ok(response);
    }

    [HttpPost("with-vehicle")]
    public async Task<IActionResult> RegisterWithVehicle([FromBody] RegisterCustomerWithVehicleDto dto)
    {
        var response = await userService.RegisterCustomerWithVehicleAsync(dto);
        return Ok(response);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterUserDto dto)
    {
        var response = await userService.CreateCustomerAsync(dto);
        return Ok(response);
    }
}
