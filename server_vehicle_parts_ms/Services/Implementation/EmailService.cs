using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using server_vehicle_parts_ms.Services.Interface;

namespace server_vehicle_parts_ms.Services.Implementation;

public class EmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpSettings = configuration.GetSection("Smtp");
        Console.WriteLine($"[EmailService] Attempting to send email to {toEmail} via {smtpSettings["Host"]}...");
        
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtpSettings["SenderName"], smtpSettings["SenderEmail"]));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            // Use server certificate validation callback if needed (for testing)
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(
                smtpSettings["Host"], 
                int.Parse(smtpSettings["Port"] ?? "587"), 
                SecureSocketOptions.StartTls
            );
            Console.WriteLine("[EmailService] Connected to SMTP server.");

            await client.AuthenticateAsync(smtpSettings["Username"], smtpSettings["Password"]);
            Console.WriteLine("[EmailService] Authenticated successfully.");

            await client.SendAsync(message);
            Console.WriteLine("[EmailService] Email sent successfully!");

            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] ERROR: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"[EmailService] INNER ERROR: {ex.InnerException.Message}");
            
            throw new Exception($"Failed to send email: {ex.Message}");
        }
    }
}
