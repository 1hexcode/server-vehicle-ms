using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/reminder-schedules")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ReminderSchedulesController(ReminderScheduleService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpGet("{jobKey}")]
    public async Task<IActionResult> Get(string jobKey) => Ok(await service.GetAsync(jobKey));

    [HttpPut("{jobKey}")]
    public async Task<IActionResult> Update(string jobKey, [FromBody] ReminderScheduleUpdateDto dto)
        => Ok(await service.UpdateAsync(jobKey, dto));

    [HttpPost("{jobKey}/run")]
    public async Task<IActionResult> RunNow(string jobKey) => Ok(await service.RunNowAsync(jobKey));
}
