using System.Net;
using System.Net.Mail;

namespace Fixawyy.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
        var portStr = _config["EmailSettings:Port"] ?? "587";
        var port = int.TryParse(portStr, out var p) ? p : 587;
        var senderEmail = _config["EmailSettings:SenderEmail"] ?? "info.eu.yo@gmail.com";
        var senderPassword = _config["EmailSettings:SenderPassword"]?.Replace(" ", "").Trim();
        var senderName = _config["EmailSettings:SenderName"] ?? "Fixawyy Platform";

        if (string.IsNullOrWhiteSpace(senderPassword) || senderPassword.Contains("YOUR_16_DIGIT"))
        {
            _logger.LogWarning("Email sending skipped: Google App Password is not set in appsettings.json. Code simulated in TempData.");
            return;
        }

        try
        {
            using var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}. Ensure Gmail App Password is valid.", toEmail);
        }
    }
}
