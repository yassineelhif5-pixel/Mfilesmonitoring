using System.Security.Claims;
using MFCMP.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MFCMP.Api.Services;

public sealed class PortalClaimsPrincipalFactory(
    UserManager<PortalUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<PortalUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(PortalUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("portal_password_changed", (!user.MustChangePassword).ToString().ToLowerInvariant()));
        return identity;
    }
}
