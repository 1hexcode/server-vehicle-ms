using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using server_vehicle_parts_ms.Services.Interface;
using System;
using System.IO;
using System.Threading.Tasks;

namespace server_vehicle_parts_ms.Services.Implementation;

public class LocalImageUploadService(IWebHostEnvironment env) : IImageUploadService
{
    public async Task<ImageUploadResult> UploadAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null");

        // Use the wwwroot folder for serving static files
        var uploadsFolder = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", folder);
        
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative URL that can be accessed via static file middleware
        var relativeUrl = $"/uploads/{folder}/{uniqueFileName}";

        return new ImageUploadResult
        {
            Url = relativeUrl,
            PublicId = filePath // Store local path as publicId for deletion
        };
    }

    public Task<bool> DeleteAsync(string publicId)
    {
        if (File.Exists(publicId))
        {
            File.Delete(publicId);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
