using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;

namespace server_vehicle_parts_ms.Helpers;

public class EmailJobs(AppDbContext db, IEmailService email, ILogger<EmailJobs> logger)
{
    public async Task SendWelcomeEmailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            logger.LogWarning("Welcome email skipped — user {UserId} not found", userId);
            return;
        }

        var html = $"""
            <p>Hi {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>
            <p>Your account has been created. You can now sign in using your email <strong>{System.Net.WebUtility.HtmlEncode(user.Email)}</strong>.</p>
            <p>— Vehicle Parts MS</p>
        """;

        await email.SendAsync(user.Email, user.FullName, "Welcome to Vehicle Parts MS", html, ct: ct);
    }
}
