using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;
using server_vehicle_parts_ms.Helpers;

namespace server_vehicle_parts_ms.Controllers;

[Route("api/uploads")]
[ApiController]
[Authorize(Roles = "Admin,Staff")]
public class UploadsController(IImageUploadService uploader, ILogger<UploadsController> logger) : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"
    };

    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequestDto dto)
    {
        var file = dto.File;
        if (file == null || file.Length == 0)
            return Ok(new ApiResponse<ImageUploadResultDto> { Success = false, Message = "No file uploaded" });
        if (file.Length > MaxBytes)
            return Ok(new ApiResponse<ImageUploadResultDto> { Success = false, Message = $"File too large (max {MaxBytes / 1024 / 1024} MB)" });
        if (!AllowedContentTypes.Contains(file.ContentType))
            return Ok(new ApiResponse<ImageUploadResultDto> { Success = false, Message = $"Unsupported content type '{file.ContentType}'. Allowed: jpeg, png, webp, gif" });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await uploader.UploadAsync(stream, file.FileName, dto.Folder);
            return Ok(new ApiResponse<ImageUploadResultDto>
            {
                Success = true,
                Message = "Image uploaded",
                Data = new ImageUploadResultDto { Url = result.Url, PublicId = result.PublicId }
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Image upload rejected");
            return Ok(new ApiResponse<ImageUploadResultDto> { Success = false, Message = ex.Message });
        }
    }
}
