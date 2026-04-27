using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Interface;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class StaffController(IUserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterStaff([FromBody] RegisterUserDto dto)
    {
        var response = await userService.CreateStaffAsync(dto);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetStaff()
    {
        var response = await userService.GetAllStaffAsync();
        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStaff(Guid id, [FromBody] UpdateStaffDto dto)
    {
        var response = await userService.UpdateStaffAsync(id, dto);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DisableStaff(Guid id)
    {
        var response = await userService.DisableStaffAsync(id);
        return Ok(response);
    }
}
