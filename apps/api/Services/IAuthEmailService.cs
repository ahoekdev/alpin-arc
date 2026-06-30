using Api.Models;

namespace Api.Services;

public interface IAuthEmailService
{
    Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task SendPasswordResetEmailAsync(ApplicationUser user, CancellationToken cancellationToken);
}
