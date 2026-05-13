using System.Security.Cryptography;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Helpers;

namespace server_vehicle_parts_ms.Services.Implementation;

public class EmailVerificationService(
    AppDbContext db,
    TokenService tokenService,
    IBackgroundJobClient jobs,
    IConfiguration config,
    ILogger<EmailVerificationService> logger)
{
    // ── Frontend base URL (used to build the link in the email) ──────────
    private string FrontendUrl =>
        Environment.GetEnvironmentVariable("APP_FRONTEND_URL")
        ?? config["AppFrontendUrl"]
        ?? "http://localhost:3000";

    // ── Generate token, persist it, enqueue email ────────────────────────
    /// <summary>
    /// Generates a new 32-byte crypto-random token, saves it to the user
    /// row, and enqueues the verification email via Hangfire.
    /// Call this right after creating a new user.
    /// </summary>
    public async Task SendVerificationEmailAsync(Data.Entities.Users user, CancellationToken ct = default)
    {
        var rawToken = GenerateToken();
        user.EmailVerificationToken       = rawToken;
        user.EmailVerificationTokenExpiry = DateTimeOffset.UtcNow.AddHours(24);
        await db.SaveChangesAsync(ct);

        var frontendUrl = FrontendUrl;
        jobs.Enqueue<EmailJobs>(j =>
            j.SendVerificationEmailAsync(user.Id, rawToken, frontendUrl, CancellationToken.None));

        logger.LogInformation("Verification token issued for user {UserId}", user.Id);
    }

    // ── Verify token from the link click ────────────────────────────────
    /// <summary>
    /// Finds the user by raw token, validates expiry, marks the account
    /// as verified+active, clears the token, and returns a JWT.
    /// </summary>
    public async Task<ApiResponse<string>> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Fail("Verification token is required.");

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, ct);

        if (user == null)
            return Fail("Token is invalid or has already been used.");

        if (user.IsEmailVerified)
            return Fail("This account is already verified. Please log in.");

        if (user.EmailVerificationTokenExpiry == null ||
            user.EmailVerificationTokenExpiry < DateTimeOffset.UtcNow)
        {
            // Clear the stale token so the user is forced to resend
            user.EmailVerificationToken       = null;
            user.EmailVerificationTokenExpiry = null;
            await db.SaveChangesAsync(ct);
            return Fail("Verification link has expired. Please request a new one.");
        }

        // ✅ Mark verified, activate account, wipe the one-time token
        user.IsEmailVerified          = true;
        user.IsActive                 = true;
        user.EmailVerificationToken   = null;
        user.EmailVerificationTokenExpiry = null;
        await db.SaveChangesAsync(ct);

        // Fire welcome email in the background
        jobs.Enqueue<EmailJobs>(j => j.SendWelcomeEmailAsync(user.Id, CancellationToken.None));

        logger.LogInformation("Email verified for user {UserId}", user.Id);

        // Issue JWT so the frontend can log the user in immediately
        var jwt = tokenService.CreateToken(user);
        return new ApiResponse<string>
        {
            Success = true,
            Data    = jwt,
            Message = "Email verified successfully. You are now logged in."
        };
    }

    // ── Resend verification email ─────────────────────────────────────────
    /// <summary>
    /// Re-issues a fresh token only when the existing one has expired
    /// (or was never set). Silently succeeds even for unknown emails to
    /// prevent user-enumeration attacks.
    /// </summary>
    public async Task<ApiResponse<string>> ResendVerificationAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Fail("Email is required.");

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

        // Always return the same message to prevent email enumeration
        const string genericMessage = "If that email is registered and unverified, a new link has been sent.";

        if (user == null || user.IsEmailVerified)
            return new ApiResponse<string> { Success = true, Message = genericMessage };

        // Flood-protection: don't resend if a valid token already exists
        if (user.EmailVerificationToken != null &&
            user.EmailVerificationTokenExpiry > DateTimeOffset.UtcNow)
        {
            return new ApiResponse<string> { Success = true, Message = genericMessage };
        }

        await SendVerificationEmailAsync(user, ct);
        return new ApiResponse<string> { Success = true, Message = genericMessage };
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        // Base64Url-safe (no +, /, or = padding)
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static ApiResponse<string> Fail(string message) =>
        new() { Success = false, Message = message };
}
