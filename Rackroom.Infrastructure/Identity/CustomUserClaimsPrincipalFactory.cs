using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Rackroom.Infrastructure.Identity;

public class CustomUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public CustomUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        ClaimsIdentity identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("GymId", user.GymId.ToString()));
        return identity;
    }
}
