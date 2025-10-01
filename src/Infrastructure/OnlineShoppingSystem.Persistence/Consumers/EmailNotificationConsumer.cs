using MassTransit;
using OnlineShppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.Events;

namespace OnlineShoppingSystem.Persistence.Consumers;

public class EmailNotificationConsumer : IConsumer<EmailNotificationEvent>
{
    private readonly IEmailService _emailService;

    public EmailNotificationConsumer(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Consume(ConsumeContext<EmailNotificationEvent> context)
    {
        var msg = context.Message;

        if (msg.UseHtmlTemplate && msg.FullName is not null && msg.UserName is not null)
        {
            string htmlContent = $@"
            <body style='background-color:#f4f4f4; padding:20px; font-family:sans-serif;'>
                <div style='max-width:600px; margin:auto; background:#fff; padding:30px; border-radius:8px; box-shadow:0 0 10px rgba(0,0,0,0.1);'>
                    <img src='https://res.cloudinary.com/dcim2xh9n/image/upload/v1753833472/ChatGPT_Image_Jul_30_2025_03_54_07_AM_r51jxb.png'
                         alt='Buynity' style='width:150px; margin-bottom:20px;' />
                    <h2 style='color:#333;'>Salam, {msg.FullName}!</h2>
                    <p style='font-size:16px; color:#555;'>{msg.Body}</p>

                    <div style='margin-top:20px;'>
                        <img src='{msg.ProfileImageUrl ?? "https://ui-avatars.com/api/?name=" + msg.FullName}' 
                             style='width:100px; border-radius:50%; border:2px solid #eee;' />
                        <p><strong>Username:</strong> {msg.UserName}</p>
                        <p><strong>Email:</strong> {msg.To}</p>
                    </div>

                    <p style='margin-top:30px; font-size:14px; color:#888;'>© {DateTime.UtcNow.Year} Buynity</p>
                </div>
            </body>";

            await _emailService.SendEmailAsync(msg.To, msg.Subject, htmlContent);
        }
        else
        {
            await _emailService.SendEmailAsync(msg.To, msg.Subject, msg.Body);
        }
    }
}
