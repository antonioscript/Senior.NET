using Database.EntityFramework;
using Microsoft.EntityFrameworkCore;
using SeniorApi.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Persistence: pick ONE of the two registrations below. Both implement the same
// Application.Abstractions.Persistence interfaces (ICustomerRepository, IOrderRepository, ...),
// so swapping the line is enough to change which ORM backs the whole API - nothing in
// Application/Domain or the controllers needs to know which one is active.
//   - AddEntityFrameworkPersistence: Database/EntityFramework (DbContext, change tracking, migrations)
//   - AddDapperPersistence:          Database/Dapper (hand-written SQL, no tracking)
builder.Services.AddEntityFrameworkPersistence(builder.Configuration);
// builder.Services.AddDapperPersistence(builder.Configuration);

// Authentication: validates JWTs issued by the Keycloak realm started via docker-compose.yml.
// See SeniorApi/Security/KeycloakAuthenticationExtensions.cs for the full explanation.
builder.Services.AddKeycloakAuthentication(builder.Configuration);

var app = builder.Build();

// AUTO-MIGRATION ON STARTUP -------------------------------------------------------------------
// MigrateAsync() applies any pending EF Core migrations the moment the API starts, creating the
// database if it doesn't exist yet. This means `docker compose up -d` + `dotnet run` is enough
// to go from zero to a working schema — no manual `dotnet ef database update` step needed.
//
// GetService<AppDbContext>() (not GetRequiredService) returns null when AddDapperPersistence is
// active (AppDbContext was never registered), so the block is safely skipped in that mode.
//
// Trade-offs to know:
//   PRO  — great for dev and small teams: one less deployment step, always in sync.
//   CON  — in high-availability production setups, multiple instances starting simultaneously can
//           race to apply the same migration. For those scenarios, run migrations as a separate
//           pre-deploy step (e.g. a Kubernetes init container or a CI job calling `dotnet ef`).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetService<AppDbContext>();
    if (db is not null)
        await db.Database.MigrateAsync();
}
// ---------------------------------------------------------------------------------------------

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

// UseAuthentication must run before UseAuthorization: it's what actually reads the
// Authorization header, validates the JWT, and builds the ClaimsPrincipal that UseAuthorization
// (and [Authorize] on controllers) then checks.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
