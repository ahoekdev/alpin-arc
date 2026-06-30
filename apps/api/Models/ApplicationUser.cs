using Microsoft.AspNetCore.Identity;

namespace Api.Models;

public sealed class ApplicationUser : IdentityUser
{
    public ICollection<LodgeFavorite> LodgeFavorites { get; } = [];
}
