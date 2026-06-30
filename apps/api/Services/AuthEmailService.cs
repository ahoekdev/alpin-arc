using System.Text.Encodings.Web;
using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Api.Services;

public sealed class AuthEmailService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IConfiguration configuration) : IAuthEmailService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly IConfiguration _configuration = configuration;

    public async Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var url = BuildClientUrl("/confirm-email", new Dictionary<string, string?>
        {
            ["userId"] = user.Id,
            ["code"] = code,
            ["returnUrl"] = "/login",
        });

        await _emailSender.SendEmailAsync(
            user.Email,
            "Confirm your AlpinArc account",
            $"""
            <p>Confirm your AlpinArc account by opening this link:</p>
            <p><a href="{HtmlEncoder.Default.Encode(url)}">Confirm email</a></p>
            """,
            cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var url = BuildClientUrl("/reset-password", new Dictionary<string, string?>
        {
            ["email"] = user.Email,
            ["code"] = code,
        });

        await _emailSender.SendEmailAsync(
            user.Email,
            "Reset your AlpinArc password",
            $"""
            <p>Reset your AlpinArc password by opening this link:</p>
            <p><a href="{HtmlEncoder.Default.Encode(url)}">Reset password</a></p>
            """,
            cancellationToken);
    }

    private string BuildClientUrl(string path, IDictionary<string, string?> query)
    {
        var baseUrl = _configuration["ClientApp:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("ClientApp:BaseUrl is not configured.");
        }

        return QueryHelpers.AddQueryString($"{baseUrl}{path}", query);
    }
}
