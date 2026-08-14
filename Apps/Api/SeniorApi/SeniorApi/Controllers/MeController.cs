using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeniorApi.Controllers;

/// <summary>
/// Has no business purpose - it exists purely so you can see what a validated Keycloak token
/// looks like once it reaches your code, and to demonstrate the two authorization styles
/// [Authorize] enables: "must be authenticated" vs "must hold a specific role."
/// </summary>
[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    /// <summary>
    /// Any authenticated user, any role. Dumps every claim the JwtBearer handler parsed out of the
    /// access token, plus the role claims KeycloakRoleClaimsTransformation added on top - compare
    /// the "role" entries here against the raw "realm_access" claim to see the transformation work.
    /// </summary>
    [Authorize]
    [HttpGet]
    public IActionResult Get() =>
        Ok(new
        {
            Subject = User.FindFirst("sub")?.Value,
            Username = User.FindFirst("preferred_username")?.Value,
            Email = User.FindFirst("email")?.Value,
            Roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value),
            AllClaims = User.Claims.Select(c => new { c.Type, c.Value })
        });

    /// <summary>
    /// Only reachable with the "admin" realm role - try this with bob's token (role "customer")
    /// from the README's curl example and watch it come back 403, not 401: 401 means "you're not
    /// authenticated at all," 403 means "you are, but you're not allowed here."
    /// </summary>
    [Authorize(Roles = "admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly() => Ok(new { Message = $"Hello {User.FindFirst("preferred_username")?.Value}, you have the 'admin' role." });
}
