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
            var port = int.Parse(_config["EmailSettings:Port"]);
            var email = _config["EmailSettings:Email"];
            var password = _config["EmailSettings:Password"];
            var ssl = bool.Parse(_config["EmailSettings:EnableSSL"]);

            var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(email, password),
                EnableSsl = ssl
            };

            var mail = new MailMessage(email, to, subject, message);
            mail.IsBodyHtml = true;

            await smtp.SendMailAsync(mail);
        }
    }
}
