using Application.Abstractions.Persistence;
using Database.EntityFramework.Repositories;
using Domain.Customers;
using Domain.Orders;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Database.EntityFramework;

public static class EntityFrameworkServiceCollectionExtensions
{
    /// <summary>
    /// Call this from SeniorApi/Program.cs to wire up the EF Core stack. Kept separate from
    /// AddDapperPersistence (Database/Dapper) so Program.cs can register one or the other (or both,
    /// side by side, for direct comparison) by changing a single line.
    /// </summary>
    public static IServiceCollection AddEntityFrameworkPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:SqlServer' configuration value.");

        // DbContext is registered scoped (the default for AddDbContext) - one instance, and one
        // change tracker, per HTTP request. Never register a DbContext as a singleton: it is not
        // thread-safe and the change tracker would accumulate every entity ever loaded for the
        // life of the app.
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ILinqShowcaseRepository, EfLinqShowcaseRepository>();

        return services;
    }
}
