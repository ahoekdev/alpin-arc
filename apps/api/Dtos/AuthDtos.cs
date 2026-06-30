using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public sealed record CurrentUserDto(bool IsAuthenticated, string? Email);

public sealed record RegisterRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; }
}

public sealed record ConfirmEmailRequestDto
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Required]
    public string Code { get; init; } = string.Empty;
}

public sealed record ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}

public sealed record ResetPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Code { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record ResendConfirmationRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
