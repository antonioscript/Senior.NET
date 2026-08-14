using Database.EntityFramework;
using Domain.Customers;
using MediatR;
using VerticalSliceApi.Features;

namespace VerticalSliceApi.Features.Customers;

public static class CreateCustomer
{
    // Command = write intent. Returns a Response (the created resource) rather than void so the
    // endpoint can return 201 with a Location header pointing to the new resource.
    // Commands are named in the imperative: CreateCustomer, not CustomerCreation.
    public record Command(string Name, string Email) : IRequest<Response>;

    public record Response(Guid Id, string Name, string Email);

    public sealed class Handler(AppDbContext db) : IRequestHandler<Command, Response>
    {
        public async Task<Response> Handle(Command request, CancellationToken ct)
        {
            // Domain logic stays in the domain — the handler orchestrates, the aggregate enforces.
            // Customer.Create throws if email is invalid; that exception propagates as 500 here.
            // (In a real API, a validation pipeline with FluentValidation would catch bad input
            // before reaching the handler and return 400. That's the next TODO item on the list.)
            var customer = Customer.Create(request.Name, request.Email);

            db.Customers.Add(customer);

            // In Vertical Slice there's no UnitOfWork interface — the handler calls SaveChangesAsync
            // directly. If you need two operations in one transaction, they go in the same handler.
            // If that becomes unwieldy, that's the signal to reconsider whether they belong in the
            // same feature, or whether you need a Saga/Process Manager.
            await db.SaveChangesAsync(ct);

            return new Response(customer.Id, customer.Name, customer.Email.Value);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/customers", async (Command command, ISender sender, CancellationToken ct) =>
            {
                var response = await sender.Send(command, ct);
                // Results.Created returns HTTP 201 with a Location header.
                // The string is the URL of the new resource, not a template.
                return Results.Created($"/api/customers/{response.Id}", response);
            })
            .WithTags("Customers")
            .WithName(nameof(CreateCustomer))
            .Produces<Response>(StatusCodes.Status201Created);
        }
    }
}
