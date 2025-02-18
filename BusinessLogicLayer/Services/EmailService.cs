using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Kiểm tra các giá trị configuration
            var smtpServer = _configuration["EmailSettings:SmtpServer"]
                ?? throw new InvalidOperationException("SMTP Server is not configured");
            var smtpPortStr = _configuration["EmailSettings:SmtpPort"]
                ?? throw new InvalidOperationException("SMTP Port is not configured");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"]
                ?? throw new InvalidOperationException("SMTP Username is not configured");
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"]
                ?? throw new InvalidOperationException("SMTP Password is not configured");
            var senderEmail = _configuration["EmailSettings:SenderEmail"]
                ?? throw new InvalidOperationException("Sender Email is not configured");
            var senderName = _configuration["EmailSettings:SenderName"]
                ?? "System";

            var smtpPort = int.Parse(smtpPortStr);

            using var message = new MailMessage();
            message.From = new MailAddress(senderEmail, senderName);
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            await client.SendMailAsync(message);
        }
    }
}
