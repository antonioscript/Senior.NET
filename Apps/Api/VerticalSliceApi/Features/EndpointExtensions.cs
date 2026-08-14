using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace VerticalSliceApi.Features;

public static class EndpointExtensions
{
    // Scans the assembly for every concrete class that implements IEndpoint and registers each
    // as a transient service. Called once in Program.cs during startup.
    //
    // TryAddEnumerable prevents duplicate registrations if AddEndpoints is called more than once
    // (e.g. in tests). Multiple implementations of the same interface are registered as separate
    // entries in the DI container, which is what IEnumerable<IEndpoint> relies on.
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var descriptors = assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsAssignableTo(typeof(IEndpoint)))
            .Select(t => ServiceDescriptor.Transient(typeof(IEndpoint), t));

        services.TryAddEnumerable(descriptors);
        return services;
    }

    // Resolves all registered IEndpoint instances from DI and calls MapEndpoint on each.
    // The order endpoints are mapped depends on the order types appear in the assembly;
    // it does not affect routing behaviour but matters for OpenAPI ordering.
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();
        foreach (var endpoint in endpoints)
            endpoint.MapEndpoint(app);
        return app;
    }
}
