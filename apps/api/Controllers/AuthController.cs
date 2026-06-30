using Api.Dtos;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/auth")]
[ApiController]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuthEmailService authEmailService,
    IAntiforgery antiforgery) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly IAuthEmailService _authEmailService = authEmailService;
    private readonly IAntiforgery _antiforgery = antiforgery;

    [HttpGet("csrf", Name = "getCsrfToken")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken ?? string.Empty, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
        });

        return NoContent();
    }

    [HttpGet("me", Name = "getCurrentUser")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public ActionResult<CurrentUserDto> Me()
    {
        var email = User.Identity?.IsAuthenticated == true
            ? User.Identity.Name
            : null;

        return Ok(new CurrentUserDto(email is not null, email));
    }

    [HttpPost("register", Name = "register")]
    [AllowAnonymous]
    [RequireAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);

        if (existingUser is not null)
        {
            if (!await _userManager.IsEmailConfirmedAsync(existingUser))
            {
                await _authEmailService.SendConfirmationEmailAsync(existingUser, cancellationToken);
            }

            return NoContent();
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return ValidationProblem(ModelState);
        }

        await _authEmailService.SendConfirmationEmailAsync(user, cancellationToken);
        return NoContent();
    }

    [HttpPost("login", Name = "login")]
    [AllowAnonymous]
    [RequireAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email.Trim(),
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        return result.Succeeded
            ? NoContent()
            : Problem("Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
    }

    [HttpPost("logout", Name = "logout")]
    [Authorize]
    [RequireAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpPost("confirm-email", Name = "confirmEmail")]
    [AllowAnonymous]
    [RequireAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return Problem("The confirmation link is invalid or expired.", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Code);
        return result.Succeeded
            ? NoContent()
            : Problem("The confirmation link is invalid or expired.", statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("forgot-password", Name = "forgotPassword")]
    [AllowAnonymous]
    [RequireAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
        {
            await _authEmailService.SendPasswordResetEmailAsync(user, cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("reset-password", Name = "resetPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Problem("The reset link is invalid or expired.", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Code, request.Password);
        if (result.Succeeded)
        {
            return NoContent();
        }

        if (IsInvalidToken(result))
        {
            return Problem("The reset link is invalid or expired.", statusCode: StatusCodes.Status400BadRequest);
        }

        AddIdentityErrors(result);
        return ValidationProblem(ModelState);
    }

    [HttpPost("resend-confirmation", Name = "resendConfirmation")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
        {
            await _authEmailService.SendConfirmationEmailAsync(user, cancellationToken);
        }

        return NoContent();
    }

    private static bool IsInvalidToken(IdentityResult result)
    {
        return result.Errors.Any(error =>
            string.Equals(error.Code, "InvalidToken", StringComparison.OrdinalIgnoreCase));
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }
    }
}
