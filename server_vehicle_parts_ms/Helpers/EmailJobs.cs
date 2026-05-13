using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;

namespace server_vehicle_parts_ms.Helpers;

public class EmailJobs(AppDbContext db, IEmailService email, ILogger<EmailJobs> logger)
{
    // ── Verification email (sent at registration) ─────────────────────────
    public async Task SendVerificationEmailAsync(Guid userId, string token, string frontendUrl, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            logger.LogWarning("Verification email skipped — user {UserId} not found", userId);
            return;
        }

        // The frontend page reads ?token= and calls GET /api/auth/verify-email?token=…
        var verifyUrl = $"{frontendUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";
        var safeName  = System.Net.WebUtility.HtmlEncode(user.FullName);
        var safeEmail = System.Net.WebUtility.HtmlEncode(user.Email);

        var html = $"""
            <div style="font-family:sans-serif;max-width:560px;margin:auto;padding:32px">
              <h2 style="color:#1a1a2e">Verify your email address</h2>
              <p>Hi <strong>{safeName}</strong>,</p>
              <p>Thanks for registering with <strong>Vehicle Parts MS</strong>.<br>
                 Please click the button below to verify your email address
                 (<a href="mailto:{safeEmail}">{safeEmail}</a>) and activate your account.</p>
              <p style="margin:32px 0">
                <a href="{verifyUrl}"
                   style="background:#4f46e5;color:#fff;padding:12px 24px;border-radius:6px;
                          text-decoration:none;font-size:15px;font-weight:600">
                  Verify my email
                </a>
              </p>
              <p style="font-size:13px;color:#6b7280">
                This link expires in <strong>24 hours</strong>.<br>
                If you didn't create an account you can safely ignore this email.
              </p>
              <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0"/>
              <p style="font-size:12px;color:#9ca3af">— Vehicle Parts MS</p>
            </div>
            """;

        await email.SendAsync(user.Email, user.FullName, "Verify your Vehicle Parts MS account", html, ct: ct);
        logger.LogInformation("Verification email enqueued for {Email}", user.Email);
    }

    // ── Welcome email (sent after verification succeeds) ─────────────────
    public async Task SendWelcomeEmailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            logger.LogWarning("Welcome email skipped — user {UserId} not found", userId);
            return;
        }

        var html = $"""
            <div style="font-family:sans-serif;max-width:560px;margin:auto;padding:32px">
              <h2 style="color:#1a1a2e">✅ Your account is verified!</h2>
              <p>Hi <strong>{System.Net.WebUtility.HtmlEncode(user.FullName)}</strong>,</p>
              <p>Your email has been confirmed and your <strong>Vehicle Parts MS</strong> account is now active.
                 You can sign in at any time using your email
                 <a href="mailto:{System.Net.WebUtility.HtmlEncode(user.Email)}">{System.Net.WebUtility.HtmlEncode(user.Email)}</a>.</p>
              <p style="font-size:13px;color:#6b7280">— Vehicle Parts MS</p>
            </div>
            """;

        await email.SendAsync(user.Email, user.FullName, "Welcome to Vehicle Parts MS 🎉", html, ct: ct);
    }
}
