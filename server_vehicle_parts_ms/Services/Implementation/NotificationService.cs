using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;

namespace server_vehicle_parts_ms.Services.Implementation;

public class NotificationService(AppDbContext db)
{
    public async Task<ApiResponse<List<NotificationDto>>> ListMineAsync(Guid userId, bool? unreadOnly)
    {
        var q = db.Notifications.Where(n => n.UserId == userId);
        if (unreadOnly == true) q = q.Where(n => !n.IsRead);
        var items = await q.OrderByDescending(n => n.CreatedAt).ToListAsync();
        return new ApiResponse<List<NotificationDto>>
        {
            Success = true,
            Data = items.Select(ToDto).ToList()
        };
    }

    public async Task<ApiResponse<NotificationDto>> CreateAsync(NotificationCreateDto dto)
    {
        if (!await db.Users.AnyAsync(u => u.Id == dto.UserId))
            return new ApiResponse<NotificationDto> { Success = false, Message = "Target user not found" };

        var n = new Notifications
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Body = dto.Body,
            Type = dto.Type
        };
        db.Notifications.Add(n);
        await db.SaveChangesAsync();
        return new ApiResponse<NotificationDto> { Success = true, Message = "Notification created", Data = ToDto(n) };
    }

    public async Task<ApiResponse<string>> MarkReadAsync(Guid userId, Guid id)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (n == null)
            return new ApiResponse<string> { Success = false, Message = "Notification not found" };
        if (!n.IsRead)
        {
            n.IsRead = true;
            await db.SaveChangesAsync();
        }
        return new ApiResponse<string> { Success = true, Message = "Marked as read" };
    }

    public async Task<ApiResponse<string>> MarkAllReadAsync(Guid userId)
    {
        await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return new ApiResponse<string> { Success = true, Message = "All notifications marked as read" };
    }

    public Task EnqueueAsync(Guid userId, string title, string? body, NotificationType type)
    {
        db.Notifications.Add(new Notifications
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type
        });
        return Task.CompletedTask;
    }

    public async Task NotifyAdminsAsync(string title, string? body, NotificationType type)
    {
        var admins = await db.Users.Where(u => u.Role == UserRoles.Admin).Select(u => u.Id).ToListAsync();
        foreach (var adminId in admins)
        {
            db.Notifications.Add(new Notifications
            {
                UserId = adminId,
                Title = title,
                Body = body,
                Type = type
            });
        }
    }

    public static NotificationDto ToDto(Notifications n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        Title = n.Title,
        Body = n.Body,
        Type = n.Type.ToString(),
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
