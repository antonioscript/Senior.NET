using Domain.Customers;
using Domain.Orders;
using Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Database.EntityFramework;

// ---------------------------------------------------------------------------------------------
// EF CORE - "what is this and why" cheat sheet
// ---------------------------------------------------------------------------------------------
// EF Core is a full ORM: it tracks entity state in memory (the "change tracker"), generates SQL
// for you from LINQ, manages relationships/cascades, and ships a migrations system to evolve the
// schema over time. You trade some control over the exact SQL for a lot of productivity - that
// tradeoff is the whole point of comparing this folder against Database/Dapper.
//
// NuGet packages used in this project (Database.csproj) and what each one is for:
//   - Microsoft.EntityFrameworkCore.SqlServer
//       The SQL Server *provider*. EF Core itself is database-agnostic; the provider translates
//       LINQ/SaveChanges into T-SQL. Swap this package (e.g. Npgsql.EntityFrameworkCore.PostgreSQL,
//       Microsoft.EntityFrameworkCore.Sqlite) to target a different engine - almost nothing in this
//       folder would need to change. It transitively pulls in Microsoft.EntityFrameworkCore (the
//      core ORM: DbContext, change tracker, LINQ provider, model building).
//   - Microsoft.EntityFrameworkCore.Design
//       Build-time only package (PrivateAssets="all" by default) that powers the `dotnet ef`
//       tooling: migrations scaffolding, "update-database", reverse engineering an existing DB.
//       Required in whichever project you scaffold migrations from.
//   - Microsoft.Data.SqlClient
//       The actual ADO.NET driver that opens the TCP connection to SQL Server. Both the EF Core
//       provider above and the Dapper repositories in ../Dapper share this same driver/connection
//       string - they are just two different layers of abstraction over the same wire protocol.
//
// To install the CLI tool that drives migrations from the terminal (one-time, machine-wide):
//   dotnet tool install --global dotnet-ef
// Commands, run from the Database project folder:
//   dotnet ef migrations add InitialCreate --project . --startup-project .
//   dotnet ef database update              --project . --startup-project .
// Using Database itself as --startup-project works (no need to point at SeniorApi, and no need
// for SeniorApi to reference Microsoft.EntityFrameworkCore.Design) precisely because
// AppDbContextFactory.cs below implements IDesignTimeDbContextFactory<AppDbContext> - the `dotnet
// ef` tooling finds it via reflection and uses it to build a DbContext without running the web
// host, the real DI container, or reading appsettings.json at all.
// ---------------------------------------------------------------------------------------------
public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Keeps this class free of per-entity mapping noise: every IEntityTypeConfiguration<T> in
        // the Configurations/ folder is picked up automatically by assembly scanning.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
