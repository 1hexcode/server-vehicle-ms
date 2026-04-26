using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace server_vehicle_parts_ms.Helpers;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, string? plainTextBody = null, CancellationToken ct = default);
}

public class EmailSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "";
}

public class MailKitEmailService(EmailSettings settings, ILogger<MailKitEmailService> logger) : IEmailService
{
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, string? plainTextBody = null, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = plainTextBody ?? StripHtml(htmlBody)
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(settings.Host, settings.Port, socketOptions, ct);
        await client.AuthenticateAsync(settings.User, settings.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation("Sent email to {Email} subject {Subject}", toEmail, subject);
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
}

public class LoggingOnlyEmailService(ILogger<LoggingOnlyEmailService> logger) : IEmailService
{
    public Task SendAsync(string toEmail, string toName, string subject, string htmlBody, string? plainTextBody = null, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL DISABLED — would send] to={To} subject={Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}
