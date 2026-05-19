using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;

namespace server_vehicle_parts_ms.Helpers;

public class ReminderJobs(AppDbContext db, IEmailService email, ILogger<ReminderJobs> logger)
{
    // ── Instant low-stock alert (fires when a sale drops a part at/below reorder) ───
    public async Task SendLowStockInstantAlertAsync(Guid partId, CancellationToken ct = default)
    {
        var part = await db.VehicleParts
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.Id == partId, ct);
        if (part == null)
        {
            logger.LogWarning("Low-stock instant alert skipped - part {PartId} not found", partId);
            return;
        }

        var admins = await db.Users
            .Where(u => u.Role == UserRoles.Admin && u.IsActive)
            .ToListAsync(ct);
        if (admins.Count == 0) return;

        var customNote = await LoadCustomMessageAsync(ReminderJobCatalog.LowStockDigest, ct);
        var safeName = System.Net.WebUtility.HtmlEncode(part.Name);
        var safeSku = System.Net.WebUtility.HtmlEncode(part.Sku);
        var safeVendor = System.Net.WebUtility.HtmlEncode(part.Vendor?.Name ?? "-");

        var html = $"""
            <div style="font-family:sans-serif;max-width:560px;margin:auto;padding:32px">
              <h2 style="color:#b91c1c">Low stock alert</h2>
              {RenderCustomMessageBlock(customNote)}
              <p><strong>{safeName}</strong> (SKU <code>{safeSku}</code>) has hit its reorder threshold.</p>
              <table style="border-collapse:collapse;margin:16px 0">
                <tr><td style="padding:4px 12px">Current stock</td><td style="padding:4px 12px"><strong>{part.StockQuantity}</strong></td></tr>
                <tr><td style="padding:4px 12px">Reorder level</td><td style="padding:4px 12px">{part.ReorderLevel}</td></tr>
                <tr><td style="padding:4px 12px">Vendor</td><td style="padding:4px 12px">{safeVendor}</td></tr>
              </table>
              <p style="font-size:13px;color:#6b7280">Please reorder soon to avoid stock-outs.</p>
              <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0"/>
              <p style="font-size:12px;color:#9ca3af">- Vehicle Parts MS</p>
            </div>
            """;

        foreach (var admin in admins)
        {
            await email.SendAsync(admin.Email, admin.FullName,
                $"Low stock: {part.Name} ({part.Sku})", html, ct: ct);
        }
        logger.LogInformation("Low-stock instant alert sent for {Sku} to {Count} admins", part.Sku, admins.Count);
    }

    // ── Daily low-stock digest (recurring) ───────────────────────────────────────────
    public async Task SendLowStockDigestAsync(CancellationToken ct = default)
    {
        var lowStock = await db.VehicleParts
            .Include(p => p.Vendor)
            .Where(p => p.IsActive && p.StockQuantity <= p.ReorderLevel)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync(ct);

        if (lowStock.Count == 0)
        {
            logger.LogInformation("Low-stock digest: nothing to report");
            return;
        }

        var admins = await db.Users
            .Where(u => u.Role == UserRoles.Admin && u.IsActive)
            .ToListAsync(ct);
        if (admins.Count == 0) return;

        var customNote = await LoadCustomMessageAsync(ReminderJobCatalog.LowStockDigest, ct);
        var rows = string.Join("", lowStock.Select(p => $"""
            <tr>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb">{System.Net.WebUtility.HtmlEncode(p.Name)}</td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb"><code>{System.Net.WebUtility.HtmlEncode(p.Sku)}</code></td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb;text-align:right"><strong>{p.StockQuantity}</strong></td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb;text-align:right">{p.ReorderLevel}</td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb">{System.Net.WebUtility.HtmlEncode(p.Vendor?.Name ?? "-")}</td>
            </tr>
            """));

        var html = $"""
            <div style="font-family:sans-serif;max-width:720px;margin:auto;padding:32px">
              <h2 style="color:#1a1a2e">Daily low-stock digest</h2>
              {RenderCustomMessageBlock(customNote)}
              <p><strong>{lowStock.Count}</strong> part(s) are at or below their reorder level.</p>
              <table style="border-collapse:collapse;width:100%;font-size:14px">
                <thead>
                  <tr style="background:#f3f4f6">
                    <th style="padding:8px 12px;text-align:left">Part</th>
                    <th style="padding:8px 12px;text-align:left">SKU</th>
                    <th style="padding:8px 12px;text-align:right">Stock</th>
                    <th style="padding:8px 12px;text-align:right">Reorder</th>
                    <th style="padding:8px 12px;text-align:left">Vendor</th>
                  </tr>
                </thead>
                <tbody>{rows}</tbody>
              </table>
              <p style="font-size:13px;color:#6b7280;margin-top:24px">Review and place purchase orders as needed.</p>
              <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0"/>
              <p style="font-size:12px;color:#9ca3af">- Vehicle Parts MS</p>
            </div>
            """;

        foreach (var admin in admins)
        {
            await email.SendAsync(admin.Email, admin.FullName,
                $"Low stock digest - {lowStock.Count} part(s) need reorder", html, ct: ct);
        }

        foreach (var admin in admins)
        {
            db.Notifications.Add(new Notifications
            {
                UserId = admin.Id,
                Title = $"{lowStock.Count} part(s) low on stock",
                Body = "See email digest for details.",
                Type = NotificationType.LowStock
            });
        }
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Low-stock digest sent to {Count} admins ({Parts} parts)", admins.Count, lowStock.Count);
    }

    // ── Daily unpaid-credit reminders (recurring) ────────────────────────────────────
    public async Task SendUnpaidCreditRemindersAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var overdue = await db.SalesInvoices
            .Include(i => i.Customer)
            .Where(i =>
                (i.Status == SalesInvoiceStatus.Issued || i.Status == SalesInvoiceStatus.PartiallyPaid) &&
                i.DueAt != null &&
                i.DueAt < now)
            .OrderBy(i => i.DueAt)
            .ToListAsync(ct);

        if (overdue.Count == 0)
        {
            logger.LogInformation("Unpaid-credit reminders: no overdue invoices");
            return;
        }

        var (currency, systemName, customNote) = await LoadEmailContextAsync(ReminderJobCatalog.UnpaidCreditReminders, ct);
        int sent = 0;
        foreach (var invoice in overdue)
        {
            if (await SendInvoiceReminderAsync(invoice, currency, systemName, customNote, now, ct)) sent++;
        }

        await NotifyAdminsOfReminderBatchAsync(overdue.Count, sent, ct);
        logger.LogInformation("Unpaid-credit reminders: {Sent} email(s) sent for {Total} overdue invoice(s)", sent, overdue.Count);
    }

    // ── Targeted unpaid-credit reminders (admin-triggered, by invoice id) ────────────
    public async Task SendRemindersForInvoicesAsync(List<Guid> invoiceIds, CancellationToken ct = default)
    {
        if (invoiceIds.Count == 0) return;

        var invoices = await db.SalesInvoices
            .Include(i => i.Customer)
            .Where(i =>
                invoiceIds.Contains(i.Id) &&
                (i.Status == SalesInvoiceStatus.Issued || i.Status == SalesInvoiceStatus.PartiallyPaid))
            .ToListAsync(ct);

        if (invoices.Count == 0)
        {
            logger.LogInformation("Targeted reminders: no eligible invoices for {Count} requested IDs", invoiceIds.Count);
            return;
        }

        var (currency, systemName, customNote) = await LoadEmailContextAsync(ReminderJobCatalog.UnpaidCreditReminders, ct);
        var now = DateTimeOffset.UtcNow;
        int sent = 0;
        foreach (var invoice in invoices)
        {
            if (await SendInvoiceReminderAsync(invoice, currency, systemName, customNote, now, ct)) sent++;
        }

        await NotifyAdminsOfReminderBatchAsync(invoices.Count, sent, ct);
        logger.LogInformation("Targeted reminders: {Sent} email(s) sent for {Total} requested invoice(s)", sent, invoices.Count);
    }

    private async Task<bool> SendInvoiceReminderAsync(
        SalesInvoices invoice, string currency, string systemName, string? customNote, DateTimeOffset now, CancellationToken ct)
    {
        if (invoice.Customer == null || string.IsNullOrWhiteSpace(invoice.Customer.Email))
            return false;

        var safeName = System.Net.WebUtility.HtmlEncode(invoice.Customer.FullName);
        var safeInvoice = System.Net.WebUtility.HtmlEncode(invoice.InvoiceNumber);
        var safeSystem = System.Net.WebUtility.HtmlEncode(systemName);
        var isOverdue = invoice.DueAt != null && invoice.DueAt < now;
        var headline = isOverdue
            ? $"is currently <strong>{(int)Math.Ceiling((now - invoice.DueAt!.Value).TotalDays)} day(s) overdue</strong>"
            : (invoice.DueAt != null
                ? $"is due on <strong>{invoice.DueAt:yyyy-MM-dd}</strong>"
                : "has an outstanding balance");

        var html = $"""
            <div style="font-family:sans-serif;max-width:560px;margin:auto;padding:32px">
              <h2 style="color:#1a1a2e">Payment reminder</h2>
              <p>Hi <strong>{safeName}</strong>,</p>
              {RenderCustomMessageBlock(customNote)}
              <p>This is a friendly reminder that invoice <strong>{safeInvoice}</strong> from <strong>{safeSystem}</strong> {headline}.</p>
              <table style="border-collapse:collapse;margin:16px 0">
                <tr><td style="padding:4px 12px">Invoice</td><td style="padding:4px 12px"><strong>{safeInvoice}</strong></td></tr>
                <tr><td style="padding:4px 12px">Amount due</td><td style="padding:4px 12px"><strong>{currency} {invoice.Total:N2}</strong></td></tr>
                <tr><td style="padding:4px 12px">Due date</td><td style="padding:4px 12px">{(invoice.DueAt?.ToString("yyyy-MM-dd") ?? "-")}</td></tr>
                <tr><td style="padding:4px 12px">Status</td><td style="padding:4px 12px">{invoice.Status}</td></tr>
              </table>
              <p>Please settle the outstanding balance at your earliest convenience. If you've already paid,
                 please disregard this message.</p>
              <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0"/>
              <p style="font-size:12px;color:#9ca3af">- {safeSystem}</p>
            </div>
            """;

        await email.SendAsync(invoice.Customer.Email, invoice.Customer.FullName,
            $"Payment reminder - invoice {invoice.InvoiceNumber}", html, ct: ct);
        return true;
    }

    private async Task<(string currency, string systemName, string? customNote)> LoadEmailContextAsync(string jobKey, CancellationToken ct)
    {
        var settings = await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var note = await LoadCustomMessageAsync(jobKey, ct);
        return (settings?.Currency ?? "Rs.", settings?.SystemName ?? "Vehicle Parts MS", note);
    }

    private async Task<string?> LoadCustomMessageAsync(string jobKey, CancellationToken ct)
    {
        return await db.ReminderSchedules
            .AsNoTracking()
            .Where(r => r.JobKey == jobKey)
            .Select(r => r.CustomMessage)
            .FirstOrDefaultAsync(ct);
    }

    // Renders the admin's persisted note as a highlighted block above the templated email body.
    // Plain-text in, HTML-escaped out (line breaks -> <br/>), so admins can't inject markup.
    private static string RenderCustomMessageBlock(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return "";
        var escaped = System.Net.WebUtility.HtmlEncode(note.Trim()).Replace("\n", "<br/>");
        return $"""
            <div style="background:#fef3c7;border-left:4px solid #f59e0b;padding:12px 16px;margin:0 0 16px;border-radius:4px;font-size:14px;color:#92400e">
              {escaped}
            </div>
            """;
    }

    private async Task NotifyAdminsOfReminderBatchAsync(int eligible, int sent, CancellationToken ct)
    {
        var admins = await db.Users
            .Where(u => u.Role == UserRoles.Admin && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(ct);
        foreach (var adminId in admins)
        {
            db.Notifications.Add(new Notifications
            {
                UserId = adminId,
                Title = $"{eligible} invoice reminder(s) processed",
                Body = $"Reminder emails delivered to {sent} customer(s).",
                Type = NotificationType.UnpaidCredit
            });
        }
        await db.SaveChangesAsync(ct);
    }
}
