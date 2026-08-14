using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Database.EntityFramework;

/// <summary>
/// Lets `dotnet ef migrations add/database update` construct an AppDbContext without spinning up
/// the whole SeniorApi host (no Program.cs, no DI container, no Kestrel). The `dotnet ef` tooling
/// looks for an IDesignTimeDbContextFactory{T} implementation in the target project automatically -
/// nothing else needs to reference or call this class.
///
/// The connection string below is only used for design-time schema work (generating migrations,
/// scaffolding); SeniorApi's real DI registration (see Program.cs) supplies the actual runtime
/// connection string from appsettings/environment/secrets instead.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=SeniorApiDb;User Id=sa;Password=Senior@2026!;TrustServerCertificate=True;");

        return new AppDbContext(optionsBuilder.Options);
    }
}
