using Database.EntityFramework;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VerticalSliceApi.Features;

namespace VerticalSliceApi.Features.Customers;

// ------------------------------------------------------------------------------------------------
// VERTICAL SLICE — GET ALL CUSTOMERS
// ------------------------------------------------------------------------------------------------
// In Clean Architecture (SeniorApi) this same operation crosses four layers:
//   Controller → ICustomerRepository (Application) → EfCustomerRepository → AppDbContext
//
// In Vertical Slice, everything for this one operation lives here — in this single file:
//   Endpoint → Handler → AppDbContext
//
// There is no repository interface between the handler and the database. The handler IS the
// use case. If you need to change how customers are fetched for this endpoint, you change this
// file. You don't ripple through layers.
//
// Trade-off: if two features query customers differently, they each have their own SELECT — there
// is no shared GetAllAsync() to accidentally break both. That's intentional: coupling through
// shared repositories is what Vertical Slice avoids. Duplication across features is acceptable;
// coupling between features is not.
// ------------------------------------------------------------------------------------------------
public static class GetCustomers
{
    // IRequest<T> marks this as a MediatR message that produces T.
    // The type itself carries no logic — it's just a "what do I want" value object.
    // Naming convention: Query = read, Command = write.
    public record Query : IRequest<IReadOnlyList<Response>>;

    // Response DTO lives next to the feature that produces it.
    // Another feature (e.g. SearchCustomers) that needs a different shape defines its own record.
    // There is no shared CustomerDto that all features must conform to.
    public record Response(Guid Id, string Name, string Email, DateTime CreatedAtUtc);

    // sealed: there is exactly one handler per Query type in MediatR. Sealing prevents
    // accidental subclassing that would shadow the registration.
    public sealed class Handler(AppDbContext db) : IRequestHandler<Query, IReadOnlyList<Response>>
    {
        public async Task<IReadOnlyList<Response>> Handle(Query request, CancellationToken ct) =>
            await db.Customers
                .AsNoTracking()
                .OrderBy(c => c.Name)
                // Select projects directly to the response DTO without loading the full entity —
                // same technique as the EfLinqShowcaseRepository projection, but here it lives
                // right next to its endpoint instead of in a shared repository class.
                .Select(c => new Response(c.Id, c.Name, c.Email.Value, c.CreatedAtUtc))
                .ToListAsync(ct);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // ISender is the MediatR dispatch interface. .Send() looks up the Handler registered
            // for the given IRequest type and calls Handle — no service locator, no switch, just
            // type-safe dispatch wired up by AddMediatR at startup.
            //
            // Minimal API equivalent of:  [HttpGet] public async Task<IActionResult> GetAll(...)
            app.MapGet("/api/customers", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new Query(), ct)))
                .WithTags("Customers")
                .WithName(nameof(GetCustomers))
                .Produces<IReadOnlyList<Response>>();
        }
    }
}
