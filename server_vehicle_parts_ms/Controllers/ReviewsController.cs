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
public class ReviewsController(ReviewService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create([FromBody] ReviewRequestDto dto)
    {
        var id = User.GetUserId();
        if (id == null) return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid token" });
        return Ok(await service.CreateAsync(id.Value, dto));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? customerId, [FromQuery] Guid? appointmentId)
    {
        Guid? customerFilter = User.GetRole() == nameof(UserRoles.Customer) ? User.GetUserId() : customerId;
        return Ok(await service.ListAsync(customerFilter, appointmentId));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));
}
