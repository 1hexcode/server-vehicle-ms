using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace server_vehicle_parts_ms.Services.Interface;

public class ImageUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
}

public interface IImageUploadService
{
    Task<ImageUploadResult> UploadAsync(IFormFile file, string folder);
    Task<bool> DeleteAsync(string publicId);
}
