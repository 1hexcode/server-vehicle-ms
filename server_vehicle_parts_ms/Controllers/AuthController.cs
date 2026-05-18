using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Services.Implementation;

namespace server_vehicle_parts_ms.Controllers;

/// <summary>
/// Handles public authentication flows: customer self-registration,
/// email verification callback, and resend-verification.
/// Login is handled by <see cref="LoginController"/>.
/// </summary>
[Route("api/auth")]
[ApiController]
[EnableRateLimiting("auth-strict")]
public class AuthController(
    AppDbContext db,
    EmailVerificationService verificationService,
    ILogger<AuthController> logger) : ControllerBase
{
    // ────────────────────────────────────────────────────────────────────
    // POST /api/auth/register
    // Public customer self-registration
    // ────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Registers a new customer account.
    /// The account is created active (<c>IsActive = true</c>) so the
    /// customer can log in immediately. <c>IsEmailVerified = false</c>
    /// until the link in the verification email is clicked.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Duplicate e-mail check
        if (await db.Users.AnyAsync(u => u.Email == dto.Email.ToLowerInvariant(), ct))
            return Conflict(new ApiResponse<string> { Success = false, Message = "Email already registered." });

        // Duplicate phone check
        if (await db.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber.Trim(), ct))
            return Conflict(new ApiResponse<string> { Success = false, Message = "Phone number already registered." });

        // Password confirmation
        if (dto.Password != dto.PasswordVerify)
            return BadRequest(new ApiResponse<string> { Success = false, Message = "Passwords do not match." });

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return BadRequest(new ApiResponse<string> { Success = false, Message = "Password must be at least 6 characters." });

        var hasher = new PasswordHasher<Users>();
        var user = new Users
        {
            Email       = dto.Email.ToLowerInvariant().Trim(),
            FullName    = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            Address     = dto.Address.Trim(),
            Role        = UserRoles.Customer,
            // Active immediately; email verification is tracked separately
            IsActive        = true,
            IsEmailVerified = false,
        };
        user.Password = hasher.HashPassword(user, dto.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        // Generate token + enqueue verification email via Hangfire
        await verificationService.SendVerificationEmailAsync(user, ct);

        logger.LogInformation("New customer registered: {Email}", user.Email);

        return Ok(new ApiResponse<string>
        {
            Success = true,
            Message = "Registration successful. Please check your email and click the verification link to activate your account."
        });
    }

    // ────────────────────────────────────────────────────────────────────
    // GET /api/auth/verify-email?token=<raw_token>
    // Called by the frontend after the user clicks the link
    // ────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Verifies the email token.  On success returns a JWT so the frontend
    /// can log the user in immediately without a separate login step.
    /// </summary>
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken ct)
    {
        var result = await verificationService.VerifyEmailAsync(token, ct);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // ────────────────────────────────────────────────────────────────────
    // POST /api/auth/resend-verification
    // Body: { "email": "user@example.com" }
    // ────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Resends the verification email if the current token has expired.
    /// Always returns 200 to prevent email-enumeration attacks.
    /// </summary>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto dto, CancellationToken ct)
    {
        var result = await verificationService.ResendVerificationAsync(dto.Email, ct);
        // Always 200 regardless - anti-enumeration
        return Ok(result);
    }
}
