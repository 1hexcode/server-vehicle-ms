using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Helpers;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class VehiclesController(VehicleService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create([FromBody] VehicleRequestDto dto)
    {
        var id = User.GetUserId();
        if (id == null) return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid token" });
        return Ok(await service.CreateAsync(id.Value, dto));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? customerId)
    {
        var role = User.GetRole();
        Guid? filter = role == nameof(UserRoles.Customer) ? User.GetUserId() : customerId;
        return Ok(await service.ListAsync(filter));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var role = User.GetRole();
        Guid? filter = role == nameof(UserRoles.Customer) ? User.GetUserId() : null;
        return Ok(await service.GetAsync(id, filter));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Update(Guid id, [FromBody] VehicleRequestDto dto)
    {
        var owner = User.GetUserId();
        if (owner == null) return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid token" });
        return Ok(await service.UpdateAsync(id, owner.Value, dto));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var owner = User.GetUserId();
        if (owner == null) return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid token" });
        return Ok(await service.DeleteAsync(id, owner.Value));
    }
}
