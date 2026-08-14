using Database.EntityFramework;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VerticalSliceApi.Features;

namespace VerticalSliceApi.Features.Customers;

public static class GetCustomerById
{
    // The Query carries the input (id) as a property, not a constructor argument, so MediatR
    // can bind it from the route without needing a custom model binder.
    public record Query(Guid Id) : IRequest<Response?>;

    public record Response(Guid Id, string Name, string Email, DateTime CreatedAtUtc);

    public sealed class Handler(AppDbContext db) : IRequestHandler<Query, Response?>
    {
        public async Task<Response?> Handle(Query request, CancellationToken ct) =>
            await db.Customers
                .AsNoTracking()
                .Where(c => c.Id == request.Id)
                .Select(c => new Response(c.Id, c.Name, c.Email.Value, c.CreatedAtUtc))
                .FirstOrDefaultAsync(ct);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/customers/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new Query(id), ct);
                // Results.NotFound() / Results.Ok() are the Minimal API equivalents of
                // NotFound() / Ok() in a controller. The difference: these are static factory
                // methods on Results, not inherited from ControllerBase.
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithTags("Customers")
            .WithName(nameof(GetCustomerById))
            .Produces<Response>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }
}
