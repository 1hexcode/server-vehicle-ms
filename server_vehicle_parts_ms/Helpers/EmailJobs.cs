using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Helpers;

public class EmailJobs(AppDbContext db, IEmailService email, ILogger<EmailJobs> logger)
{
    // ── Verification email (sent at registration) ─────────────────────────
    public async Task SendVerificationEmailAsync(Guid userId, string token, string frontendUrl, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            logger.LogWarning("Verification email skipped - user {UserId} not found", userId);
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
              <p style="font-size:12px;color:#9ca3af">- Vehicle Parts MS</p>
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
            logger.LogWarning("Welcome email skipped - user {UserId} not found", userId);
            return;
        }

        var html = $"""
            <div style="font-family:sans-serif;max-width:560px;margin:auto;padding:32px">
              <h2 style="color:#1a1a2e">✅ Your account is verified!</h2>
              <p>Hi <strong>{System.Net.WebUtility.HtmlEncode(user.FullName)}</strong>,</p>
              <p>Your email has been confirmed and your <strong>Vehicle Parts MS</strong> account is now active.
                 You can sign in at any time using your email
                 <a href="mailto:{System.Net.WebUtility.HtmlEncode(user.Email)}">{System.Net.WebUtility.HtmlEncode(user.Email)}</a>.</p>
              <p style="font-size:13px;color:#6b7280">- Vehicle Parts MS</p>
            </div>
            """;

        await email.SendAsync(user.Email, user.FullName, "Welcome to Vehicle Parts MS 🎉", html, ct: ct);
    }

    // ── Invoice email (staff-triggered delivery of a sales invoice) ───────
    public async Task SendInvoiceEmailAsync(Guid invoiceId, string? toEmailOverride, CancellationToken ct = default)
    {
        var invoice = await db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Vehicle)
            .Include(i => i.Lines).ThenInclude(l => l.Part)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice == null)
        {
            logger.LogWarning("Invoice email skipped - invoice {InvoiceId} not found", invoiceId);
            return;
        }

        var toEmail = !string.IsNullOrWhiteSpace(toEmailOverride) ? toEmailOverride!.Trim() : invoice.Customer?.Email;
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            logger.LogWarning("Invoice email skipped - no recipient for invoice {InvoiceId}", invoiceId);
            return;
        }

        var settings = await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var currency = settings?.Currency ?? "Rs.";
        var systemName = settings?.SystemName ?? "Vehicle Parts MS";
        var systemAddress = settings?.Address;
        var systemPhone = settings?.ContactPhone;

        var safeSystem = System.Net.WebUtility.HtmlEncode(systemName);
        var safeCustomer = System.Net.WebUtility.HtmlEncode(invoice.Customer?.FullName ?? "Customer");
        var safeInvoice = System.Net.WebUtility.HtmlEncode(invoice.InvoiceNumber);
        var vehicleLine = invoice.Vehicle != null
            ? $"<tr><td style=\"padding:4px 12px\">Vehicle</td><td style=\"padding:4px 12px\">{System.Net.WebUtility.HtmlEncode(invoice.Vehicle.VehicleNumber)}</td></tr>"
            : "";

        var lineRows = string.Join("", invoice.Lines.Select(l => $"""
            <tr>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb">{System.Net.WebUtility.HtmlEncode(l.Part?.Name ?? "")}</td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb"><code>{System.Net.WebUtility.HtmlEncode(l.Part?.Sku ?? "")}</code></td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb;text-align:right">{l.Quantity}</td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb;text-align:right">{currency} {l.UnitPrice:N2}</td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb;text-align:right">{currency} {l.LineTotal:N2}</td>
            </tr>
            """));

        var html = $"""
            <div style="font-family:sans-serif;max-width:720px;margin:auto;padding:32px">
              <div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:24px">
                <div>
                  <h2 style="margin:0;color:#1a1a2e">Invoice {safeInvoice}</h2>
                  <p style="margin:4px 0;color:#6b7280;font-size:13px">{safeSystem}{(string.IsNullOrWhiteSpace(systemAddress) ? "" : " · " + System.Net.WebUtility.HtmlEncode(systemAddress))}{(string.IsNullOrWhiteSpace(systemPhone) ? "" : " · " + System.Net.WebUtility.HtmlEncode(systemPhone))}</p>
                </div>
                <div style="text-align:right;font-size:13px;color:#6b7280">
                  <div>Issued: <strong>{(invoice.IssuedAt?.ToString("yyyy-MM-dd") ?? "-")}</strong></div>
                  <div>Due: <strong>{(invoice.DueAt?.ToString("yyyy-MM-dd") ?? "-")}</strong></div>
                  <div>Status: <strong>{invoice.Status}</strong></div>
                </div>
              </div>

              <p>Hi <strong>{safeCustomer}</strong>,</p>
              <p>Please find the details of your invoice below. The full balance is payable by the due date shown.</p>

              <table style="border-collapse:collapse;margin:16px 0;font-size:13px">
                <tr><td style="padding:4px 12px">Customer</td><td style="padding:4px 12px"><strong>{safeCustomer}</strong></td></tr>
                {vehicleLine}
              </table>

              <table style="border-collapse:collapse;width:100%;font-size:14px;margin-top:16px">
                <thead>
                  <tr style="background:#f3f4f6">
                    <th style="padding:8px 12px;text-align:left">Item</th>
                    <th style="padding:8px 12px;text-align:left">SKU</th>
                    <th style="padding:8px 12px;text-align:right">Qty</th>
                    <th style="padding:8px 12px;text-align:right">Unit price</th>
                    <th style="padding:8px 12px;text-align:right">Line total</th>
                  </tr>
                </thead>
                <tbody>{lineRows}</tbody>
              </table>

              <table style="margin:24px 0 0 auto;font-size:14px">
                <tr><td style="padding:4px 12px;text-align:right">Subtotal</td><td style="padding:4px 12px;text-align:right"><strong>{currency} {invoice.Subtotal:N2}</strong></td></tr>
                <tr><td style="padding:4px 12px;text-align:right">Discount</td><td style="padding:4px 12px;text-align:right">- {currency} {invoice.Discount:N2}</td></tr>
                <tr><td style="padding:4px 12px;text-align:right">Tax</td><td style="padding:4px 12px;text-align:right">{currency} {invoice.Tax:N2}</td></tr>
                <tr><td style="padding:8px 12px;text-align:right;border-top:2px solid #1a1a2e">Total due</td><td style="padding:8px 12px;text-align:right;border-top:2px solid #1a1a2e"><strong style="font-size:16px">{currency} {invoice.Total:N2}</strong></td></tr>
              </table>

              <p style="font-size:13px;color:#6b7280;margin-top:24px">If you have any questions about this invoice, please reply to this email or contact us at {(string.IsNullOrWhiteSpace(settings?.ContactEmail) ? safeSystem : System.Net.WebUtility.HtmlEncode(settings!.ContactEmail))}.</p>
              <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0"/>
              <p style="font-size:12px;color:#9ca3af">- {safeSystem}</p>
            </div>
            """;

        await email.SendAsync(toEmail, invoice.Customer?.FullName ?? "Customer",
            $"Invoice {invoice.InvoiceNumber} from {systemName}", html, ct: ct);
        logger.LogInformation("Invoice {InvoiceNumber} emailed to {Email}", invoice.InvoiceNumber, toEmail);
    }
}
