namespace Api.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken);
}
