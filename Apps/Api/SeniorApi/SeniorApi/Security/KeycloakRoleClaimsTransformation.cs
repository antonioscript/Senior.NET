using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace SeniorApi.Security;

/// <summary>
/// Keycloak puts realm roles in the access token as a single claim named "realm_access" whose
/// value is a JSON object - <c>{"roles": ["admin", "customer"]}</c> - not as one claim per role.
/// ASP.NET Core's [Authorize(Roles = "admin")] has no idea that shape exists; it just checks
/// whether the current ClaimsPrincipal has a claim of type RoleClaimType (configured as
/// ClaimTypes.Role in KeycloakAuthenticationExtensions) whose value equals "admin". This class is
/// the translation step between the two: it runs once per authenticated request and adds one
/// ClaimTypes.Role claim per entry in realm_access.roles, so [Authorize(Roles = ...)] works exactly
/// as if the roles had come from ASP.NET Core Identity all along.
/// </summary>
public class KeycloakRoleClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        var realmAccess = principal.FindFirst("realm_access")?.Value;

        if (identity is null || realmAccess is null)
        {
            return Task.FromResult(principal);
        }

        // Re-running this on every call to TransformAsync for the same principal would duplicate
        // role claims - IClaimsTransformation can run more than once per request pipeline, so
        // guard against adding the same role twice.
        var existingRoles = identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();

        using var json = JsonDocument.Parse(realmAccess);
        if (json.RootElement.TryGetProperty("roles", out var roles))
        {
            foreach (var role in roles.EnumerateArray())
            {
                var roleName = role.GetString();
                if (roleName is not null && existingRoles.Add(roleName))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }

        return Task.FromResult(principal);
    }
}
