using Database.EntityFramework;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VerticalSliceApi.Features;

namespace VerticalSliceApi.Features.Products;

public static class GetProducts
{
    public record Query : IRequest<IReadOnlyList<Response>>;

    public record Response(Guid Id, string Sku, string Name, decimal UnitPrice, int StockQuantity);

    public sealed class Handler(AppDbContext db) : IRequestHandler<Query, IReadOnlyList<Response>>
    {
        public async Task<IReadOnlyList<Response>> Handle(Query request, CancellationToken ct) =>
            await db.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new Response(p.Id, p.Sku, p.Name, p.UnitPrice, p.StockQuantity))
                .ToListAsync(ct);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/api/products", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new Query(), ct)))
                .WithTags("Products")
                .WithName(nameof(GetProducts))
                .Produces<IReadOnlyList<Response>>();
        }
    }
}
