using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/me")]
[ApiController]
[Authorize]
public class MeController(ProfileService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!TryGetUserId(out var id, out var unauthorized)) return unauthorized!;
        return Ok(await service.GetAsync(id));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ProfileUpdateDto dto)
    {
        if (!TryGetUserId(out var id, out var unauthorized)) return unauthorized!;
        return Ok(await service.UpdateAsync(id, dto));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!TryGetUserId(out var id, out var unauthorized)) return unauthorized!;
        return Ok(await service.ChangePasswordAsync(id, dto));
    }

    private bool TryGetUserId(out Guid id, out IActionResult? unauthorized)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(claim, out id))
        {
            unauthorized = null;
            return true;
        }
        unauthorized = Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid token" });
        return false;
    }
}
