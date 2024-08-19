using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace MyApp.Data;

public class CustomUserSession : AuthUserSession
{
    public override void PopulateFromClaims(IRequest httpReq, ClaimsPrincipal principal)
    {
        // Populate Session with data from Identity Auth Claims
        ProfileUrl = principal.FindFirstValue(JwtClaimTypes.Picture);
    }
}

/// <summary>
/// Add additional claims to the Identity Auth Cookie
/// </summary>
public class AdditionalUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor,
    IDbConnection db)
    : UserClaimsPrincipalFactory<ApplicationUser,IdentityRole>(userManager, roleManager, optionsAccessor)
{
    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        var claims = new List<Claim>();
        // Add additional claims here
        if (user.ProfilePath != null)
        {
            claims.Add(new Claim(JwtClaimTypes.Picture, user.ProfilePath));
        }
        
        var userId = db.Scalar<int>("SELECT ROWID FROM AspNetUsers WHERE Id = @Id", new { user.Id });
        if (userId > 0)
            claims.Add(new Claim(JwtClaimTypes.Subject, $"{userId}"));

        identity.AddClaims(claims);
        return principal;
    }
}
