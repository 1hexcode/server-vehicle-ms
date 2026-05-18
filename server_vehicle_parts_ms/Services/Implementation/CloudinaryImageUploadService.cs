using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using server_vehicle_parts_ms.Services.Interface;
using System;
using System.Threading.Tasks;

namespace server_vehicle_parts_ms.Services.Implementation;

public class CloudinaryImageUploadService : IImageUploadService
{
    private readonly IConfiguration _config;

    public CloudinaryImageUploadService(IConfiguration config)
    {
        _config = config;
        
        // TODO: Initialize Cloudinary instance here when SDK is added
        // Example:
        // var acc = new Account(
        //     _config["Cloudinary:CloudName"],
        //     _config["Cloudinary:ApiKey"],
        //     _config["Cloudinary:ApiSecret"]
        // );
        // _cloudinary = new Cloudinary(acc);
    }

    public async Task<ImageUploadResult> UploadAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null");

        // TODO: Implement actual Cloudinary upload logic here when SDK is added
        // Example:
        // using var stream = file.OpenReadStream();
        // var uploadParams = new ImageUploadParams()
        // {
        //     File = new FileDescription(file.FileName, stream),
        //     Folder = folder
        // };
        // var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        // return new ImageUploadResult { Url = uploadResult.SecureUrl.ToString(), PublicId = uploadResult.PublicId };

        // Placeholder fallback logic for now
        throw new NotImplementedException("Cloudinary integration is not yet fully implemented. Set up the Cloudinary SDK and credentials.");
    }

    public async Task<bool> DeleteAsync(string publicId)
    {
        // TODO: Implement actual Cloudinary deletion logic here
        // Example:
        // var deletionParams = new DeletionParams(publicId);
        // var result = await _cloudinary.DestroyAsync(deletionParams);
        // return result.Result == "ok";
        
        throw new NotImplementedException("Cloudinary integration is not yet fully implemented.");
    }
}
