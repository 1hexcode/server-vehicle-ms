using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PartsController(VehiclePartService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] VehiclePartRequestDto dto) => Ok(await service.CreateAsync(dto));

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] VehiclePartRequestDto dto) => Ok(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Disable(Guid id) => Ok(await service.DisableAsync(id));
}
