using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Helpers;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/sales-invoices")]
[ApiController]
[Authorize]
public class SalesInvoicesController(SalesInvoiceService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] SalesInvoiceRequestDto dto)
    {
        var staffId = User.GetUserId();
        if (staffId == null) return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid token" });
        return Ok(await service.CreateAsync(dto, staffId.Value));
    }

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

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Void(Guid id) => Ok(await service.VoidAsync(id));
}
