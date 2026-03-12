using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using ZapChat.Api.Services.Interfaces;
using ZapChat.Api.Common.Config;

namespace ZapChat.Api.Services
{
    public class SendMailService : ISendMailService
    {
        private readonly EmailSetting _emailSettings;
        private readonly ILogger<SendMailService> _logger;

        public SendMailService(
            IOptions<EmailSetting> emailSettings,
            ILogger<SendMailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return MailboxAddress.TryParse(email, out _);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            if (!IsValidEmail(to))
            {
                _logger.LogError("Email không hợp lệ: {Email}", to);
                throw new ArgumentException($"Email không hợp lệ: {to}");
            }

            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_emailSettings.DisplayName, _emailSettings.Mail));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = body };
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(
                    _emailSettings.Host,
                    _emailSettings.Port,
                    MailKit.Security.SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(_emailSettings.Mail, _emailSettings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email đã gửi thành công tới: {Email}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email tới {Email}", to);
                return false;
            }
        }
    }
}