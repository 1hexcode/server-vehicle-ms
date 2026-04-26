using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/purchase-invoices")]
[ApiController]
[Authorize(Roles = "Admin")]
public class PurchaseInvoicesController(PurchaseInvoiceService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PurchaseInvoiceRequestDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid token" });
        return Ok(await service.CreateAsync(dto, userId));
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));
}
