using _3K.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// İş akışı 10: Mail akışı
    /// Günlük güncelleme, haftalık eksik listesi, proje bazlı eksik listeleri
    /// SMTP ayarları environment variable veya appsettings'den alınır
    /// </summary>
    public class MailService : IMailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailService> _logger;

        public MailService(IConfiguration configuration, ILogger<MailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task GunlukGuncellemeMailiGonderAsync()
        {
            // TODO: Günlük güncelleme mail içeriği sistem verilerinden otomatik üretilecek
            _logger.LogInformation("Günlük güncelleme maili gönderiliyor...");
            await SendMailAsync("Günlük Güncelleme", "Günlük güncelleme raporu hazırlandı.");
        }

        public async Task HaftalikEksikListesiGonderAsync()
        {
            _logger.LogInformation("Haftalık eksik listesi maili gönderiliyor...");
            await SendMailAsync("Haftalık Eksik Listesi", "Haftalık eksik listesi hazırlandı.");
        }

        public async Task ProjeBazliEksikListesiGonderAsync(int projeId)
        {
            _logger.LogInformation("Proje {ProjeId} bazlı eksik listesi gönderiliyor...", projeId);
            await SendMailAsync($"Proje {projeId} Eksik Listesi", $"Proje {projeId} eksik listesi hazırlandı.");
        }

        private async Task SendMailAsync(string subject, string body)
        {
            try
            {
                var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST")
                    ?? _configuration["MailSettings:SmtpHost"]
                    ?? "localhost";
                var smtpPort = int.Parse(
                    Environment.GetEnvironmentVariable("SMTP_PORT")
                    ?? _configuration["MailSettings:SmtpPort"]
                    ?? "587");
                var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER")
                    ?? _configuration["MailSettings:SmtpUser"]
                    ?? "";
                var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS")
                    ?? _configuration["MailSettings:SmtpPass"]
                    ?? "";
                var fromEmail = Environment.GetEnvironmentVariable("MAIL_FROM")
                    ?? _configuration["MailSettings:FromEmail"]
                    ?? "noreply@3k.com";
                var toEmail = Environment.GetEnvironmentVariable("MAIL_TO")
                    ?? _configuration["MailSettings:ToEmail"]
                    ?? "";

                if (string.IsNullOrEmpty(toEmail))
                {
                    _logger.LogWarning("Mail alıcısı tanımlanmamış, mail gönderilmedi.");
                    return;
                }

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage(fromEmail, toEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Mail başarıyla gönderildi: {Subject}", subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mail gönderilemedi: {Subject}", subject);
            }
        }
    }
}
