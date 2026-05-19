using Hangfire;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Dtos;
using server_vehicle_parts_ms.Dtos.Request;
using server_vehicle_parts_ms.Dtos.Response;
using server_vehicle_parts_ms.Helpers;

namespace server_vehicle_parts_ms.Services.Implementation;

public class ReminderScheduleService(
    AppDbContext db,
    IRecurringJobManager recurring,
    IBackgroundJobClient jobs,
    ILogger<ReminderScheduleService> logger)
{
    public async Task<ApiResponse<List<ReminderScheduleDto>>> ListAsync()
    {
        var items = await db.ReminderSchedules.OrderBy(r => r.DisplayName).ToListAsync();
        return new ApiResponse<List<ReminderScheduleDto>>
        {
            Success = true,
            Data = items.Select(ToDto).ToList()
        };
    }

    public async Task<ApiResponse<ReminderScheduleDto>> GetAsync(string jobKey)
    {
        var s = await db.ReminderSchedules.FirstOrDefaultAsync(r => r.JobKey == jobKey);
        if (s == null)
            return new ApiResponse<ReminderScheduleDto> { Success = false, Message = "Schedule not found" };
        return new ApiResponse<ReminderScheduleDto> { Success = true, Data = ToDto(s) };
    }

    public async Task<ApiResponse<ReminderScheduleDto>> UpdateAsync(string jobKey, ReminderScheduleUpdateDto dto)
    {
        var s = await db.ReminderSchedules.FirstOrDefaultAsync(r => r.JobKey == jobKey);
        if (s == null)
            return new ApiResponse<ReminderScheduleDto> { Success = false, Message = "Schedule not found" };

        if (dto.Frequency == ReminderFrequency.Weekly && dto.DayOfWeek == null)
            return new ApiResponse<ReminderScheduleDto> { Success = false, Message = "DayOfWeek is required for weekly schedules" };
        if (dto.Frequency == ReminderFrequency.Monthly && dto.DayOfMonth == null)
            return new ApiResponse<ReminderScheduleDto> { Success = false, Message = "DayOfMonth is required for monthly schedules" };

        s.Frequency = dto.Frequency;
        s.Hour = dto.Hour;
        s.Minute = dto.Minute;
        s.DayOfWeek = dto.Frequency == ReminderFrequency.Weekly ? dto.DayOfWeek : null;
        s.DayOfMonth = dto.Frequency == ReminderFrequency.Monthly ? dto.DayOfMonth : null;
        s.IsEnabled = dto.IsEnabled;
        s.CustomMessage = string.IsNullOrWhiteSpace(dto.CustomMessage) ? null : dto.CustomMessage.Trim();

        await db.SaveChangesAsync();
        Apply(s);

        logger.LogInformation("Reminder schedule {JobKey} updated: {Cron} (enabled={Enabled})",
            s.JobKey, BuildCron(s), s.IsEnabled);

        return new ApiResponse<ReminderScheduleDto>
        {
            Success = true,
            Message = "Schedule updated",
            Data = ToDto(s)
        };
    }

    public async Task<ApiResponse<string>> RunNowAsync(string jobKey)
    {
        var s = await db.ReminderSchedules.FirstOrDefaultAsync(r => r.JobKey == jobKey);
        if (s == null)
            return new ApiResponse<string> { Success = false, Message = "Schedule not found" };

        switch (s.JobKey)
        {
            case ReminderJobCatalog.LowStockDigest:
                jobs.Enqueue<ReminderJobs>(j => j.SendLowStockDigestAsync(CancellationToken.None));
                break;
            case ReminderJobCatalog.UnpaidCreditReminders:
                jobs.Enqueue<ReminderJobs>(j => j.SendUnpaidCreditRemindersAsync(CancellationToken.None));
                break;
            default:
                return new ApiResponse<string> { Success = false, Message = $"Unknown job key '{s.JobKey}'" };
        }

        logger.LogInformation("Reminder job {JobKey} triggered manually", s.JobKey);
        return new ApiResponse<string> { Success = true, Message = "Job enqueued" };
    }

    // ── Sync DB rows -> Hangfire registry ─────────────────────────────────────
    /// <summary>
    /// Ensures every catalog entry has a DB row (seeded with defaults) and that
    /// every enabled schedule is registered with Hangfire. Disabled schedules
    /// are removed from the recurring registry. Safe to call at every startup.
    /// </summary>
    public async Task SyncFromDatabaseAsync(CancellationToken ct = default)
    {
        var existing = await db.ReminderSchedules.ToListAsync(ct);
        var byKey = existing.ToDictionary(r => r.JobKey);
        bool dirty = false;

        foreach (var entry in ReminderJobCatalog.All)
        {
            if (!byKey.TryGetValue(entry.JobKey, out var row))
            {
                row = new ReminderSchedule
                {
                    JobKey = entry.JobKey,
                    DisplayName = entry.DisplayName,
                    Description = entry.Description,
                    Frequency = ReminderFrequency.Daily,
                    Hour = 8,
                    Minute = 0,
                    IsEnabled = true,
                };
                db.ReminderSchedules.Add(row);
                dirty = true;
            }
            else if (row.DisplayName != entry.DisplayName || row.Description != entry.Description)
            {
                row.DisplayName = entry.DisplayName;
                row.Description = entry.Description;
                dirty = true;
            }
        }

        if (dirty) await db.SaveChangesAsync(ct);

        var all = await db.ReminderSchedules.ToListAsync(ct);
        foreach (var s in all) Apply(s);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    public void Apply(ReminderSchedule s)
    {
        if (!s.IsEnabled)
        {
            recurring.RemoveIfExists(s.JobKey);
            return;
        }

        var cron = BuildCron(s);
        var opts = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };

        switch (s.JobKey)
        {
            case ReminderJobCatalog.LowStockDigest:
                recurring.AddOrUpdate<ReminderJobs>(
                    s.JobKey,
                    j => j.SendLowStockDigestAsync(CancellationToken.None),
                    cron,
                    opts);
                break;
            case ReminderJobCatalog.UnpaidCreditReminders:
                recurring.AddOrUpdate<ReminderJobs>(
                    s.JobKey,
                    j => j.SendUnpaidCreditRemindersAsync(CancellationToken.None),
                    cron,
                    opts);
                break;
            default:
                logger.LogWarning("Unknown reminder job key in DB: {JobKey}", s.JobKey);
                break;
        }
    }

    public static string BuildCron(ReminderSchedule s) => s.Frequency switch
    {
        ReminderFrequency.Daily   => $"{s.Minute} {s.Hour} * * *",
        ReminderFrequency.Weekly  => $"{s.Minute} {s.Hour} * * {s.DayOfWeek ?? 1}",
        ReminderFrequency.Monthly => $"{s.Minute} {s.Hour} {s.DayOfMonth ?? 1} * *",
        _ => $"{s.Minute} {s.Hour} * * *"
    };

    private static ReminderScheduleDto ToDto(ReminderSchedule s) => new()
    {
        Id = s.Id,
        JobKey = s.JobKey,
        DisplayName = s.DisplayName,
        Description = s.Description,
        Frequency = s.Frequency.ToString(),
        Hour = s.Hour,
        Minute = s.Minute,
        DayOfWeek = s.DayOfWeek,
        DayOfMonth = s.DayOfMonth,
        IsEnabled = s.IsEnabled,
        CustomMessage = s.CustomMessage,
        Cron = BuildCron(s),
        TimeZone = "UTC",
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
