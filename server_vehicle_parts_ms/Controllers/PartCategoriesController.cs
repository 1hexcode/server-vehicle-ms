using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/part-categories")]
[ApiController]
[Authorize]
public class PartCategoriesController(PartCategoryService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] PartCategoryRequestDto dto) => Ok(await service.CreateAsync(dto));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] VehicleType? vehicleType) => Ok(await service.ListAsync(vehicleType));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PartCategoryRequestDto dto)
        => Ok(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id) => Ok(await service.DeleteAsync(id));
}
