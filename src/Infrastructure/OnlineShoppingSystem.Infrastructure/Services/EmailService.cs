using OnlineShppingSystem.Application.Abstracts.Services;
using Microsoft.Extensions.Options;
using OnlineShppingSystem.Application.Shared.Settings;
using System.Net;
using System.Net.Mail;

namespace OnlineShoppingSystem.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_settings.From, "Buynity"),
            Subject = subject,
            Body = htmlContent,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);

        using var smtp = new SmtpClient(_settings.SmtpServer, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = true
        };

        await smtp.SendMailAsync(message);
    }

    public async Task SendProfileUpdatedEmailAsync(string toEmail, string fullName, string userName, string email, string profileImageUrl)
    {
        string htmlContent = $@"
        <body style='background-color:#f4f4f4; padding:20px; font-family:sans-serif;'>
            <div style='max-width:600px; margin:auto; background:#fff; padding:30px; border-radius:8px; box-shadow:0 0 10px rgba(0,0,0,0.1);'>
                <img src='https://res.cloudinary.com/dcim2xh9n/image/upload/v1753833472/ChatGPT_Image_Jul_30_2025_03_54_07_AM_r51jxb.png'
                     alt='Buynity' style='width:150px; margin-bottom:20px;' />
                <h2 style='color:#333;'>Salam, {fullName}!</h2>
                <p style='font-size:16px; color:#555;'>Profil məlumatlarınız uğurla yeniləndi.</p>

                <div style='margin-top:20px;'>
                    <img src='{profileImageUrl}' style='width:100px; border-radius:50%; border:2px solid #eee;' />
                    <p style='margin-top:10px;'><strong>Ad Soyad:</strong> {fullName}</p>
                    <p><strong>İstifadəçi adı:</strong> {userName}</p>
                    <p><strong>Email:</strong> {email}</p>
                </div>

                <p style='margin-top:30px; font-size:14px; color:#888;'>© {DateTime.UtcNow.Year} Buynity - Bütün hüquqlar qorunur.</p>
            </div>
        </body>";

        await SendEmailAsync(toEmail, "Profil məlumatlarınız yeniləndi", htmlContent);
    }
}
