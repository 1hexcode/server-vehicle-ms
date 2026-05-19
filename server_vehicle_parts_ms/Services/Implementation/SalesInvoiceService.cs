using Hangfire;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;
using server_vehicle_parts_ms.Helpers;

namespace server_vehicle_parts_ms.Services.Implementation;

public class SalesInvoiceService(AppDbContext db, StockMovementService stock, NotificationService notifications, IBackgroundJobClient jobs)
{
    private const decimal LoyaltyThreshold = 5000m;
    private const decimal LoyaltyRate = 0.10m;
    // Earn rule: 1 loyalty point per Rs. 100 of invoice subtotal (rounded down).
    private const decimal PointsPerCurrencyUnit = 100m;

    private static int PointsEarnedFor(decimal subtotal) => (int)Math.Floor(subtotal / PointsPerCurrencyUnit);

    public async Task<ApiResponse<SalesInvoiceDto>> CreateAsync(SalesInvoiceRequestDto dto, Guid createdBy)
    {
        var customer = await db.Users.FirstOrDefaultAsync(u => u.Id == dto.CustomerId);
        if (customer == null || customer.Role != UserRoles.Customer)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Customer not found" };

        if (dto.VehicleId.HasValue)
        {
            var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId.Value);
            if (vehicle == null)
                return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Vehicle not found" };
            if (vehicle.CustomerId != customer.Id)
                return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Vehicle does not belong to that customer" };
        }

        var partIds = dto.Lines.Select(l => l.PartId).Distinct().ToList();
        var parts = await db.VehicleParts.Where(p => partIds.Contains(p.Id)).ToListAsync();
        if (parts.Count != partIds.Count)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "One or more parts not found" };

        var partLookup = parts.ToDictionary(p => p.Id);

        // pre-check stock availability (sum requested per part across lines)
        var requested = dto.Lines.GroupBy(l => l.PartId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        foreach (var (pid, needed) in requested)
        {
            if (partLookup[pid].StockQuantity < needed)
                return new ApiResponse<SalesInvoiceDto>
                {
                    Success = false,
                    Message = $"Insufficient stock for part {partLookup[pid].Sku} (have {partLookup[pid].StockQuantity}, need {needed})"
                };
        }

        var invoice = new SalesInvoices
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerId = customer.Id,
            VehicleId = dto.VehicleId,
            CreatedByUserId = createdBy,
            Status = SalesInvoiceStatus.Issued,
            IssuedAt = DateTimeOffset.UtcNow,
            DueAt = dto.DueAt,
            Tax = dto.Tax,
        };

        decimal subtotal = 0;
        foreach (var line in dto.Lines)
        {
            var part = partLookup[line.PartId];
            var lineTotal = line.UnitPrice * line.Quantity;
            invoice.Lines.Add(new SalesInvoiceLines
            {
                PartId = part.Id,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = lineTotal,
            });
            var wasAboveReorder = part.StockQuantity > part.ReorderLevel;
            part.StockQuantity -= line.Quantity;

            if (part.StockQuantity <= part.ReorderLevel)
            {
                await notifications.NotifyAdminsAsync(
                    "Low Stock Alert",
                    $"Part {part.Name} ({part.Sku}) is low on stock. Current quantity: {part.StockQuantity}",
                    NotificationType.LowStock
                );

                // Only fire an instant email when the sale is what pushed it across the threshold,
                // so admins don't get repeat alerts for every line of every sale once it's already low.
                if (wasAboveReorder)
                {
                    var partId = part.Id;
                    jobs.Enqueue<ReminderJobs>(j => j.SendLowStockInstantAlertAsync(partId, CancellationToken.None));
                }
            }

            subtotal += lineTotal;
        }

        invoice.Subtotal = subtotal;
        invoice.Discount = subtotal > LoyaltyThreshold ? Math.Round(subtotal * LoyaltyRate, 2) : 0m;
        invoice.Total = subtotal - invoice.Discount + dto.Tax;

        customer.LoyaltyPoints += PointsEarnedFor(subtotal);

        db.SalesInvoices.Add(invoice);

        foreach (var line in invoice.Lines)
            stock.Record(line.PartId, -line.Quantity, StockMovementReason.SalesInvoice, invoice.Id);

        await db.SaveChangesAsync();
        return new ApiResponse<SalesInvoiceDto> { Success = true, Message = "Invoice issued", Data = await LoadDtoAsync(invoice.Id) };
    }

    public async Task<ApiResponse<List<SalesInvoiceDto>>> ListAsync(Guid? customerFilter)
    {
        var q = db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Vehicle)
            .Include(i => i.Lines).ThenInclude(l => l.Part)
            .AsQueryable();
        if (customerFilter.HasValue) q = q.Where(i => i.CustomerId == customerFilter.Value);
        var items = await q.OrderByDescending(i => i.IssuedAt).ToListAsync();
        return new ApiResponse<List<SalesInvoiceDto>> { Success = true, Data = items.Select(ToDto).ToList() };
    }

    public async Task<ApiResponse<SalesInvoiceDto>> GetAsync(Guid id, Guid? customerFilter)
    {
        var invoice = await db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Vehicle)
            .Include(i => i.Lines).ThenInclude(l => l.Part)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Invoice not found" };
        if (customerFilter.HasValue && invoice.CustomerId != customerFilter.Value)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Forbidden" };
        return new ApiResponse<SalesInvoiceDto> { Success = true, Data = ToDto(invoice) };
    }

    public async Task<ApiResponse<string>> SendEmailAsync(Guid id, string? toEmailOverride)
    {
        var row = await db.SalesInvoices
            .Where(i => i.Id == id)
            .Select(i => new { i.InvoiceNumber, i.Status, CustomerEmail = i.Customer != null ? i.Customer.Email : null })
            .FirstOrDefaultAsync();
        if (row == null)
            return new ApiResponse<string> { Success = false, Message = "Invoice not found" };
        if (row.Status == SalesInvoiceStatus.Void)
            return new ApiResponse<string> { Success = false, Message = "Cannot email a voided invoice" };

        var recipient = !string.IsNullOrWhiteSpace(toEmailOverride) ? toEmailOverride!.Trim() : row.CustomerEmail;
        if (string.IsNullOrWhiteSpace(recipient))
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Customer has no email on file; provide a toEmail override"
            };

        jobs.Enqueue<EmailJobs>(j => j.SendInvoiceEmailAsync(id, recipient, CancellationToken.None));
        return new ApiResponse<string>
        {
            Success = true,
            Message = $"Invoice {row.InvoiceNumber} queued for delivery to {recipient}"
        };
    }

    public async Task<ApiResponse<BulkReminderResultDto>> SendRemindersAsync(List<Guid> invoiceIds)
    {
        var distinct = invoiceIds.Distinct().ToList();
        if (distinct.Count == 0)
            return new ApiResponse<BulkReminderResultDto> { Success = false, Message = "No invoice IDs provided" };

        // Inline pre-check so the admin gets an immediate breakdown of what will/won't be sent.
        var rows = await db.SalesInvoices
            .Where(i => distinct.Contains(i.Id))
            .Select(i => new { i.Id, i.Status, HasEmail = i.Customer != null && i.Customer.Email != null && i.Customer.Email != "" })
            .ToListAsync();

        var found = rows.Select(r => r.Id).ToHashSet();
        var eligible = rows
            .Where(r => (r.Status == SalesInvoiceStatus.Issued || r.Status == SalesInvoiceStatus.PartiallyPaid) && r.HasEmail)
            .Select(r => r.Id)
            .ToList();
        var notFound = distinct.Where(id => !found.Contains(id)).ToList();
        var skipped = rows
            .Where(r => !(r.Status == SalesInvoiceStatus.Issued || r.Status == SalesInvoiceStatus.PartiallyPaid) || !r.HasEmail)
            .Select(r => r.Id)
            .ToList();

        if (eligible.Count > 0)
            jobs.Enqueue<ReminderJobs>(j => j.SendRemindersForInvoicesAsync(eligible, CancellationToken.None));

        return new ApiResponse<BulkReminderResultDto>
        {
            Success = true,
            Message = eligible.Count > 0 ? $"Queued {eligible.Count} reminder email(s)" : "No eligible invoices to remind",
            Data = new BulkReminderResultDto
            {
                Queued = eligible,
                Skipped = skipped,
                NotFound = notFound
            }
        };
    }

    public async Task<ApiResponse<SalesInvoiceDto>> VoidAsync(Guid id)
    {
        var invoice = await db.SalesInvoices
            .Include(i => i.Customer).Include(i => i.Vehicle)
            .Include(i => i.Lines).ThenInclude(l => l.Part)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Invoice not found" };
        if (invoice.Status == SalesInvoiceStatus.Void)
            return new ApiResponse<SalesInvoiceDto> { Success = false, Message = "Invoice already voided" };

        invoice.Status = SalesInvoiceStatus.Void;
        foreach (var line in invoice.Lines)
        {
            line.Part.StockQuantity += line.Quantity;
            stock.Record(line.PartId, line.Quantity, StockMovementReason.Void, invoice.Id);
        }

        // Reverse the points that were earned when this invoice was created. Clamp to zero
        // so a customer can never end up with a negative balance.
        if (invoice.Customer != null)
        {
            var refund = PointsEarnedFor(invoice.Subtotal);
            invoice.Customer.LoyaltyPoints = Math.Max(0, invoice.Customer.LoyaltyPoints - refund);
        }

        await db.SaveChangesAsync();
        return new ApiResponse<SalesInvoiceDto> { Success = true, Message = "Invoice voided, stock restored", Data = ToDto(invoice) };
    }

    private async Task<SalesInvoiceDto> LoadDtoAsync(Guid id)
    {
        var invoice = await db.SalesInvoices
            .Include(i => i.Customer).Include(i => i.Vehicle)
            .Include(i => i.Lines).ThenInclude(l => l.Part)
            .FirstAsync(i => i.Id == id);
        return ToDto(invoice);
    }

    private static SalesInvoiceDto ToDto(SalesInvoices i) => new()
    {
        Id = i.Id,
        InvoiceNumber = i.InvoiceNumber,
        CustomerId = i.CustomerId,
        CustomerName = i.Customer?.FullName ?? "",
        VehicleId = i.VehicleId,
        VehicleNumber = i.Vehicle?.VehicleNumber,
        CreatedByUserId = i.CreatedByUserId,
        Subtotal = i.Subtotal,
        Discount = i.Discount,
        Tax = i.Tax,
        Total = i.Total,
        Status = i.Status.ToString(),
        IssuedAt = i.IssuedAt,
        DueAt = i.DueAt,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt,
        Lines = i.Lines.Select(l => new SalesInvoiceLineDto
        {
            Id = l.Id,
            PartId = l.PartId,
            PartName = l.Part?.Name ?? "",
            Sku = l.Part?.Sku ?? "",
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            LineTotal = l.LineTotal,
        }).ToList()
    };
}
