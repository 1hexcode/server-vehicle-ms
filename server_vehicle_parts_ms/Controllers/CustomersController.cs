using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Helpers;
using server_vehicle_parts_ms.Services.Interface;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/Customers")]
[ApiController]
[Authorize]
public class CustomersController(IUserService userService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateCustomer([FromBody] RegisterUserDto dto)
    {
        var response = await userService.CreateCustomerAsync(dto);
        return Ok(response);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ListCustomers()
    {
        var response = await userService.GetAllCustomersAsync();
        return Ok(response);
    }

    [HttpPost("with-vehicle")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> RegisterWithVehicle([FromBody] RegisterCustomerWithVehicleDto dto)
    {
        var response = await userService.RegisterCustomerWithVehicleAsync(dto);
        return Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleCustomerStatusDto dto)
    {
        var response = await userService.ToggleCustomerStatusAsync(id, dto.IsActive);
        return Ok(response);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterUserDto dto)
    {
        var response = await userService.CreateCustomerAsync(dto);
        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCustomerProfile(Guid id, [FromBody] ProfileUpdateDto dto)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == null || currentUserId.Value != id)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
            {
                Success = false,
                Message = "Access denied. You can only update your own profile details."
            });
        }

        var response = await userService.UpdateCustomerProfileAsync(id, dto);
        return Ok(response);
    }

    [HttpPost("{id:guid}/profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(Guid id, IFormFile? file)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == null || currentUserId.Value != id)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<string>
            {
                Success = false,
                Message = "Access denied. You can only upload a profile picture for your own profile."
            });
        }

        file ??= Request.Form.Files.GetFile("file") ?? Request.Form.Files.GetFile("image") ?? Request.Form.Files.FirstOrDefault();
        if (file == null)
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "No profile picture file was provided in the request."
            });
        }

        var response = await userService.UploadProfilePictureAsync(id, file);
        return Ok(response);
    }
}

