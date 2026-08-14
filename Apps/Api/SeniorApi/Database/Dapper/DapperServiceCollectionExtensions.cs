using Application.Abstractions.Persistence;
using Database.Dapper.Repositories;
using Domain.Customers;
using Domain.Orders;
using Domain.Products;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Database.Dapper;

public static class DapperServiceCollectionExtensions
{
    /// <summary>
    /// Call this from SeniorApi/Program.cs instead of AddEntityFrameworkPersistence to swap the
    /// whole persistence stack to Dapper without touching Application or Domain.
    /// </summary>
    public static IServiceCollection AddDapperPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:SqlServer' configuration value.");

        // SqlConnectionFactory is intentionally a singleton: it only holds a connection string and
        // hands out brand-new, unopened SqlConnection instances. The connections themselves are
        // short-lived (opened per repository call, disposed via `using`) and rely on ADO.NET's own
        // connection pooling rather than a long-lived shared connection.
        services.AddSingleton(new SqlConnectionFactory(connectionString));

        services.AddScoped<ICustomerRepository, DapperCustomerRepository>();
        services.AddScoped<IProductRepository, DapperProductRepository>();
        services.AddScoped<IOrderRepository, DapperOrderRepository>();
        services.AddScoped<IUnitOfWork, DapperUnitOfWork>();

        return services;
    }
}
