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
    public async Task<IActionResult> ListCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var response = await userService.GetAllCustomersAsync(page, pageSize);
        return Ok(response);
    }

    // Case-insensitive contains match. Any combination of params can be supplied;
    // they're AND-combined. Returns an empty items array when nothing matches.
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? name,
        [FromQuery] string? phone,
        [FromQuery] string? vehicleNo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await userService.SearchCustomersAsync(name, phone, vehicleNo, page, pageSize));

    [HttpPost("with-vehicle")]
    public async Task<IActionResult> RegisterWithVehicle([FromBody] RegisterCustomerWithVehicleDto dto)
    {
        var response = await userService.RegisterCustomerWithVehicleAsync(dto);
        return Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleCustomerStatusDto dto)
    {
        var response = await userService.ToggleCustomerStatusAsync(id, dto.IsActive);
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

