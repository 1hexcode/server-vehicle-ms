using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ReportsController(ReportService service) : ControllerBase
{
    [HttpGet("financial")]
    public async Task<IActionResult> Financial([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
        => Ok(await service.FinancialAsync(from, to));

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory() => Ok(await service.InventoryAsync());
}
