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
public class AppointmentsController(AppointmentService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create([FromBody] AppointmentRequestDto dto)
    {
        var id = User.GetUserId();
        if (id == null) return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid token" });
        return Ok(await service.CreateAsync(id.Value, dto));
    }

    [HttpPost("staff")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateForStaff([FromBody] StaffAppointmentRequestDto dto)
        => Ok(await service.CreateForStaffAsync(dto));

    [HttpGet]
    public async Task<IActionResult> List()
    {
        Guid? filter = User.GetRole() == nameof(UserRoles.Customer) ? User.GetUserId() : null;
        return Ok(await service.ListAsync(filter));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        Guid? filter = User.GetRole() == nameof(UserRoles.Customer) ? User.GetUserId() : null;
        return Ok(await service.GetAsync(id, filter));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] AppointmentStatusUpdateDto dto)
        => Ok(await service.UpdateStatusAsync(id, dto));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var customerId = User.GetUserId();
        if (customerId == null) return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid token" });
        return Ok(await service.CancelOwnAsync(id, customerId.Value));
    }
}
