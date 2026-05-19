using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace server_vehicle_parts_ms.Helpers;

public class CloudinarySettings
{
    public string CloudName { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    public string Folder { get; set; } = "vehicle-parts-ms";
}

public record ImageUploadResult(string Url, string PublicId);

public interface IImageUploadService
{
    Task<ImageUploadResult> UploadAsync(Stream stream, string fileName, string? folderSuffix = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(string publicId, CancellationToken ct = default);
}

public class CloudinaryImageUploadService : IImageUploadService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;
    private readonly ILogger<CloudinaryImageUploadService> _logger;

    public CloudinaryImageUploadService(CloudinarySettings settings, ILogger<CloudinaryImageUploadService> logger)
    {
        _settings = settings;
        _logger = logger;
        _cloudinary = new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
        _cloudinary.Api.Secure = true;
    }

    public async Task<ImageUploadResult> UploadAsync(Stream stream, string fileName, string? folderSuffix = null, CancellationToken ct = default)
    {
        var folder = string.IsNullOrWhiteSpace(folderSuffix)
            ? _settings.Folder
            : $"{_settings.Folder}/{folderSuffix.Trim('/')}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);
        if (result.Error != null)
        {
            _logger.LogError("Cloudinary upload failed: {Error}", result.Error.Message);
            throw new InvalidOperationException($"Image upload failed: {result.Error.Message}");
        }

        _logger.LogInformation("Uploaded image {PublicId} ({Bytes} bytes)", result.PublicId, result.Bytes);
        return new ImageUploadResult(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task<bool> DeleteAsync(string publicId, CancellationToken ct = default)
    {
        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
        return result.Result == "ok";
    }
}

// Mirrors the email-service fallback - when Cloudinary credentials aren't set the API
// still boots and the endpoint returns a clear error rather than 500ing on a null client.
public class DisabledImageUploadService(ILogger<DisabledImageUploadService> logger) : IImageUploadService
{
    public Task<ImageUploadResult> UploadAsync(Stream stream, string fileName, string? folderSuffix = null, CancellationToken ct = default)
    {
        logger.LogWarning("[Cloudinary disabled] Upload requested for {FileName} but no credentials configured", fileName);
        throw new InvalidOperationException("Image uploads are not configured. Set Cloudinary credentials to enable.");
    }

    public Task<bool> DeleteAsync(string publicId, CancellationToken ct = default) => Task.FromResult(false);
}
