using System.Threading.Tasks;

namespace Hospital.Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
