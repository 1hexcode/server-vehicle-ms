using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/stock-movements")]
[ApiController]
[Authorize(Roles = "Admin")]
public class StockMovementsController(StockMovementService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? partId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to)
        => Ok(await service.ListAsync(partId, from, to));
}
