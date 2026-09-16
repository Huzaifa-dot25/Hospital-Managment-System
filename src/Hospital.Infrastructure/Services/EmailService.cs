using Hospital.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Hospital.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // For development, we just log the email content.
            // In a real application, this would use SMTP or a service like SendGrid, Mailgun, etc.
            _logger.LogInformation("--- START EMAIL ---");
            _logger.LogInformation($"To: {toEmail}");
            _logger.LogInformation($"Subject: {subject}");
            _logger.LogInformation($"Body: {body}");
            _logger.LogInformation("--- END EMAIL ---");

            return Task.CompletedTask;
        }
    }
}
