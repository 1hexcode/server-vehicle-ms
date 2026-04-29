using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;
using server_vehicle_parts_ms.Helpers;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/sales-invoices")]
[ApiController]
[Authorize(Roles = "Admin,Staff")]
public class SalesInvoicesController(SalesInvoiceService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? customerId)
    {
        var response = await service.ListAsync(customerId);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var response = await service.GetAsync(id, null);
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SalesInvoiceRequestDto dto)
    {
        // Assume staff user ID is taken from token
        var createdBy = User.GetUserId();
        if (createdBy == null) return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid token" });
        var response = await service.CreateAsync(dto, createdBy.Value);
        return Ok(response);
    }

    // Void (cancel) invoice
    [HttpPatch("{id:guid}/void")]
    public async Task<IActionResult> Void(Guid id)
    {
        var response = await service.VoidAsync(id);
        return Ok(response);
    }
}
