using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace MathTime.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string message)
        {
            var host = _config["EmailSettings:Host"];

            var portValue = _config["EmailSettings:Port"];
            var email = _config["EmailSettings:Email"];
            var password = _config["EmailSettings:Password"];
            var sslValue = _config["EmailSettings:EnableSSL"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portValue) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Email settings are not configured properly.");
            }

            if (!int.TryParse(portValue, out var port))
                throw new InvalidOperationException("Invalid SMTP port configuration.");

            if (!bool.TryParse(sslValue, out var ssl))
                ssl = true;

            using var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(email, password),
                EnableSsl = ssl
            };

            using var mail = new MailMessage(email, to, subject, message)
            {
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(mail);
        }
    }
}
