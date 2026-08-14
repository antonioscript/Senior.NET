using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SeniorApi.Security;

// ---------------------------------------------------------------------------------------------
// KEYCLOAK + JWT BEARER - "what is this and why" cheat sheet
// ---------------------------------------------------------------------------------------------
// Keycloak is an OAuth2 / OpenID Connect *server*: it owns the login screen, the user database,
// and the signing keys used to issue tokens. SeniorApi never talks to Keycloak about passwords -
// it only ever receives an already-issued JWT access token in the `Authorization: Bearer <token>`
// header and verifies it was really signed by this realm and hasn't expired. That verification is
// exactly what the JwtBearer middleware (the package added below) does for you; nothing in this
// file talks to Keycloak directly except to fetch its public signing keys.
//
// NuGet package used (SeniorApi.csproj):
//   - Microsoft.AspNetCore.Authentication.JwtBearer
//       Adds the `AddJwtBearer` authentication handler. Given just an `Authority` (the realm's
//       base URL), it fetches `{Authority}/.well-known/openid-configuration` - the OIDC discovery
//       document - which tells it where the realm's JWKS endpoint (public signing keys) lives. It
//       then validates every incoming token's signature, issuer, audience and expiry against that,
//       with zero hand-written crypto code in this project.
//
// Two concepts worth knowing before reading the config below:
//   - Realm: a Keycloak realm is a fully isolated tenant - its own users, clients, and roles.
//     "senior-net" (see infra/keycloak/realm-export.json) is the only one this project uses.
//   - Audience (`aud` claim): Keycloak's default access token is only "addressed" to its own
//     built-in `account` client, NOT to "senior-api", unless something tells it otherwise. The
//     `senior-api-audience` protocol mapper in realm-export.json is that "something" - without it,
//     ValidateAudience below would reject every token Keycloak issues.
// ---------------------------------------------------------------------------------------------
public static class KeycloakAuthenticationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloak = configuration.GetSection("Keycloak");
        var authority = keycloak["Authority"] ?? throw new InvalidOperationException("Missing 'Keycloak:Authority' configuration value.");
        var audience = keycloak["Audience"] ?? throw new InvalidOperationException("Missing 'Keycloak:Audience' configuration value.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;

                // Local dev only: Keycloak in this project's docker-compose runs over plain HTTP.
                // A real deployment serves the realm over HTTPS and this must be true (the default)
                // - otherwise the discovery document and JWKS keys could be tampered with in transit.
                options.RequireHttpsMetadata = keycloak.GetValue("RequireHttpsMetadata", true);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,

                    // Keycloak's realm roles do NOT arrive as the flat claim type ASP.NET Core's
                    // [Authorize(Roles = "...")] looks for by default - they arrive nested inside a
                    // single JSON claim called "realm_access": {"roles": ["admin", ...]}. Setting
                    // RoleClaimType here would do nothing useful on its own; the actual translation
                    // from that nested JSON into individual role claims happens in
                    // KeycloakRoleClaimsTransformation, registered below.
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization();

        // IClaimsTransformation runs once per request, after authentication succeeds and before
        // authorization checks run - the right place to reshape Keycloak's token claims into the
        // shape ASP.NET Core's authorization system expects.
        services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();

        return services;
    }
}
