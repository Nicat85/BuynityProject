namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface IEmailService
{
    Task SendProfileUpdatedEmailAsync(string toEmail, string fullName, string userName, string email, string profileImageUrl);
    Task SendEmailAsync(string toEmail, string subject, string htmlContent);

}