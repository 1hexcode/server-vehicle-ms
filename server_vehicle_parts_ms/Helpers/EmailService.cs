using System.Net.Http.Json;
using System.Text.Json.Serialization;
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

    // Resend HTTP API key. When set, ResendEmailService is preferred over SMTP because
    // Railway's free plans block outbound SMTP but allow plain HTTPS.
    public string ResendApiKey { get; set; } = "";
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
        logger.LogInformation("[EMAIL DISABLED - would send] to={To} subject={Subject}", toEmail, subject);
        return Task.CompletedTask;
    }
}

// Sends via Resend's HTTP API (https://resend.com). One POST to api.resend.com:443,
// so works from environments that block outbound SMTP (Railway free plans, etc).
public class ResendEmailService(IHttpClientFactory httpClientFactory, EmailSettings settings, ILogger<ResendEmailService> logger) : IEmailService
{
    private const string ResendEndpoint = "https://api.resend.com/emails";

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, string? plainTextBody = null, CancellationToken ct = default)
    {
        var fromEmail = string.IsNullOrWhiteSpace(settings.FromEmail) ? "onboarding@resend.dev" : settings.FromEmail;
        var fromName = string.IsNullOrWhiteSpace(settings.FromName) ? "Vehicle Parts MS" : settings.FromName;
        var fromHeader = $"{fromName} <{fromEmail}>";

        var payload = new ResendSendRequest(
            From: fromHeader,
            To: new[] { toEmail },
            Subject: subject,
            Html: htmlBody,
            Text: plainTextBody);

        var client = httpClientFactory.CreateClient(nameof(ResendEmailService));
        using var request = new HttpRequestMessage(HttpMethod.Post, ResendEndpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("Authorization", $"Bearer {settings.ResendApiKey}");

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            // Read the body so the retry-attempt log carries Resend's error text instead of a bare status code.
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Resend send failed ({Status}) for {Email}: {Body}", (int)response.StatusCode, toEmail, body);
            throw new InvalidOperationException($"Resend API returned {(int)response.StatusCode}: {body}");
        }

        logger.LogInformation("Sent email to {Email} subject {Subject} via Resend", toEmail, subject);
    }

    private sealed record ResendSendRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html,
        [property: JsonPropertyName("text")] string? Text);
}
