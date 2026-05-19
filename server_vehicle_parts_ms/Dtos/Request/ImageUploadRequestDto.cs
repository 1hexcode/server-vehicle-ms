namespace server_vehicle_parts_ms.Dtos.Request;

public class ImageUploadRequestDto
{
    public IFormFile File { get; set; } = null!;
    public string? Folder { get; set; }
}
