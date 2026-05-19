namespace server_vehicle_parts_ms.Helpers;

public static class ReminderJobCatalog
{
    public const string LowStockDigest = "low-stock-digest";
    public const string UnpaidCreditReminders = "unpaid-credit-reminders";

    public record Entry(string JobKey, string DisplayName, string Description);

    public static readonly IReadOnlyList<Entry> All = new[]
    {
        new Entry(
            LowStockDigest,
            "Low-stock digest",
            "Emails admins a list of parts at or below their reorder level."),
        new Entry(
            UnpaidCreditReminders,
            "Unpaid credit reminders",
            "Emails customers a reminder for any sales invoice past its due date."),
    };
}
