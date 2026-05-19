using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/hot-deals")]
[ApiController]
public class HotDealsController(HotDealService service) : ControllerBase
{
    // Public homepage list - only currently-active deals.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ListActive() => Ok(await service.ListActiveAsync());

    // Admin/staff dashboard list - includes expired and disabled rows.
    [HttpGet("all")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ListAll() => Ok(await service.ListAllAsync());

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create([FromBody] HotDealRequestDto dto) => Ok(await service.CreateAsync(dto));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Update(Guid id, [FromBody] HotDealRequestDto dto)
        => Ok(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Delete(Guid id) => Ok(await service.DeleteAsync(id));
}
