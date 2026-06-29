namespace Api.Validation;

using System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class SupportedCountryCodeAttribute : ValidationAttribute
{
    private static readonly HashSet<string> SupportedCodes = new(StringComparer.Ordinal)
    {
        "AT",
        "CH",
        "DE",
        "FR",
        "IT",
        "LI",
        "SI"
    };

    public SupportedCountryCodeAttribute()
        : base("Country code must be one of: AT, CH, DE, FR, IT, LI, SI.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is string countryCode && SupportedCodes.Contains(countryCode);
    }
}
