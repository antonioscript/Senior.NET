using Application.Abstractions.Persistence;
using Domain.Customers;
using Domain.Orders;
using Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Database.EntityFramework.Repositories;

// ------------------------------------------------------------------------------------------------
// LINQ + EF CORE SHOWCASE
// ------------------------------------------------------------------------------------------------
// LINQ (Language Integrated Query) is C#'s built-in syntax for querying sequences. When used
// against EF Core's DbSet<T> (which implements IQueryable<T>), each LINQ operator is *translated*
// to SQL by the EF Core provider — the query is not executed in memory, it is sent to the database.
//
// Key mental model — IQueryable<T> vs IEnumerable<T>:
//   IQueryable<T>  → query is built up as an expression tree; SQL is only generated and sent to
//                    the DB when you call .ToList(), .FirstOrDefault(), .Count(), etc.
//   IEnumerable<T> → data is already in memory; LINQ operators run as in-memory C# loops.
//
// Never call .ToList() in the middle of a chain if you intend to filter/sort after it — that pulls
// all rows into memory first and then filters them in C#:
//
//   BAD:  dbContext.Products.ToList().Where(p => p.UnitPrice > 10)  // loads ALL rows, then filters
//   GOOD: dbContext.Products.Where(p => p.UnitPrice > 10).ToListAsync() // one SQL WHERE clause
// ------------------------------------------------------------------------------------------------
public class EfLinqShowcaseRepository(AppDbContext db) : ILinqShowcaseRepository
{
    // 1. SELECT PROJECTION -----------------------------------------------------------------------
    // Select() projects each entity to a different shape before the SQL is executed.
    // Without Select, EF loads every column of every related table. With it, the generated SQL
    // only fetches the columns you actually reference — important for wide tables.
    public async Task<IReadOnlyList<ProductSummary>> GetProductSummariesAsync(CancellationToken ct = default) =>
        await db.Products
            .AsNoTracking()     // read-only: skip change-tracker overhead (no need to track for a list)
            .OrderBy(p => p.Name)
            .Select(p => new ProductSummary(p.Id, p.Name, p.UnitPrice, p.StockQuantity))
            .ToListAsync(ct);

    // 2. WHERE + ORDERBY + SKIP/TAKE (pagination) ------------------------------------------------
    // All three compose into a single SQL statement:
    //   SELECT ... FROM Orders WHERE Status = @status ORDER BY CreatedAtUtc DESC
    //   OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
    //
    // page is 1-based (page 1 = first page). Skip = (page - 1) * pageSize.
    public async Task<IReadOnlyList<Order>> GetOrdersPagedAsync(
        OrderStatus status, int page, int pageSize, CancellationToken ct = default) =>
        await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAtUtc)  // most recent first
            .ThenBy(o => o.Id)                       // stable secondary sort avoids flicker across pages
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    // 3. GROUPBY + SUM — aggregation via correlated sub-selects ----------------------------------
    // EF Core translates navigation properties inside Select() to correlated subqueries or JOINs.
    // c.Orders.SelectMany(o => o.Items).Sum(...) becomes a single SQL expression per customer row.
    //
    // Pattern to remember: GroupBy in EF Core works best when you project everything you need
    // inside the same .Select() — avoid grouping first and then filtering/joining afterwards,
    // as that forces client-side evaluation.
    public async Task<IReadOnlyList<CustomerRevenue>> GetRevenueByCustomerAsync(CancellationToken ct = default) =>
        await db.Customers
            .AsNoTracking()
            .Select(c => new CustomerRevenue(
                c.Id,
                c.Name,
                // correlated subquery: SUM of (qty × price) across every item across every order
                c.Orders.SelectMany(o => o.Items).Sum(i => (decimal?)(i.Quantity * i.UnitPrice)) ?? 0m,
                c.Orders.Count()))
            .OrderByDescending(r => r.TotalRevenue)
            .ToListAsync(ct);

    // 4. GROUPBY + COUNT -------------------------------------------------------------------------
    // Simpler aggregation: count orders per status.
    // Generates: SELECT Status, COUNT(*) FROM Orders GROUP BY Status
    public async Task<IReadOnlyList<OrderCountByStatus>> GetOrderCountByStatusAsync(CancellationToken ct = default) =>
        await db.Orders
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new OrderCountByStatus(g.Key, g.Count()))
            .OrderBy(r => r.Status.ToString())
            .ToListAsync(ct);

    // 5. ANY — existence check -------------------------------------------------------------------
    // Any() generates SQL EXISTS(...), which is far more efficient than Count() > 0:
    //   EXISTS stops scanning as soon as it finds one matching row.
    //   COUNT(*) scans and counts every matching row just to tell you it's > 0.
    //
    // Same contrast applies to .Any() vs .Where(...).ToList().Any() — the second variant loads
    // every matching row into memory before checking if the list is empty.
    public Task<bool> HasLowStockProductsAsync(int threshold, CancellationToken ct = default) =>
        db.Products.AnyAsync(p => p.StockQuantity <= threshold, ct);

    // 6. ALL — universal quantifier --------------------------------------------------------------
    // All() generates SQL NOT EXISTS (violating row). Returns true only if every row satisfies the
    // predicate. Returns true on an empty table (vacuous truth — same as standard SQL semantics).
    // Equivalent: NOT EXISTS (SELECT 1 FROM Products WHERE StockQuantity <= 0)
    public Task<bool> AllProductsInStockAsync(CancellationToken ct = default) =>
        db.Products.AllAsync(p => p.StockQuantity > 0, ct);

    // 7. SELECTMANY — flattening nested collections ----------------------------------------------
    // SelectMany is the LINQ equivalent of an inner join or a "flatMap": it takes a sequence of
    // sequences and collapses them into a single flat sequence.
    //
    // Here: Orders → OrderItems (one level of nesting collapsed).
    // EF Core translates this to JOINs. Without SelectMany you would need nested foreach in C#
    // over already-loaded data — which means more round-trips or Cartesian products.
    public async Task<IReadOnlyList<OrderItemFlat>> GetAllItemsByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .SelectMany(
                o => o.Items,
                (order, item) => new OrderItemFlat(
                    order.Id,
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice,
                    item.Quantity * item.UnitPrice))
            .ToListAsync(ct);

    // 8. ASSPLITQUERY — avoid cartesian product explosion ----------------------------------------
    // When you eager-load multiple collections (Include + ThenInclude), EF Core by default
    // generates a single JOIN query. With 3 Customers × 10 Orders × 5 Items, that JOIN produces
    // 3 × 10 × 5 = 150 rows in the result set even though there are only 150 real items total.
    // The more levels you nest, the worse the explosion.
    //
    // AsSplitQuery() instead executes one SELECT per collection:
    //   SELECT * FROM Customers
    //   SELECT * FROM Orders WHERE CustomerId IN (...)
    //   SELECT * FROM OrderItems WHERE OrderId IN (...)
    // Three queries, no row duplication — EF Core stitches the results together in memory.
    //
    // Trade-off: multiple round-trips vs fewer rows. Use AsSplitQuery when the cartesian product
    // would be large (many items per parent); use the default single query when the dataset is small.
    public async Task<IReadOnlyList<Customer>> GetCustomersWithOrdersSplitAsync(CancellationToken ct = default) =>
        await db.Customers
            .AsNoTracking()
            .Include(c => c.Orders)
            .ThenInclude(o => o.Items)
            .AsSplitQuery()
            .ToListAsync(ct);

    // 9. FROMSQL — raw SQL with EF Core mapping --------------------------------------------------
    // Use FromSql when the LINQ translator can't express what you need (full-text search,
    // window functions, CTEs, stored procedures, etc.).
    //
    // FromSql with an interpolated string is safe: EF Core extracts the interpolated values as
    // ADO.NET parameters (@p0, @p1, ...) — it does NOT concatenate them into the SQL string.
    // This prevents SQL injection even though it looks like string interpolation.
    //
    //   SAFE:   .FromSql($"SELECT ... WHERE Name LIKE {pattern}")     ← parameterised by EF Core
    //   UNSAFE: .FromSqlRaw($"SELECT ... WHERE Name LIKE '{term}'")   ← real string concat, avoid
    //
    // The returned IQueryable<T> can still be composed: .Where(...).OrderBy(...).ToListAsync()
    // appends to the generated SQL as a sub-query or CTE depending on the provider.
    public async Task<IReadOnlyList<Product>> SearchProductsByNameAsync(string term, CancellationToken ct = default)
    {
        var pattern = $"%{term}%";
        return await db.Products
            .FromSql($"SELECT * FROM Products WHERE Name LIKE {pattern}")
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    // 10. COMPILED QUERY — eliminate repeated SQL translation overhead ---------------------------
    // Every time EF Core runs a LINQ query it traverses the expression tree and translates it to
    // SQL. For queries called thousands of times per second (hot paths), this overhead is
    // measurable. A compiled query caches the translation after the first call.
    //
    // Rules for compiled queries:
    //   - Must be a static field or property (compiled once per AppDomain, not per request).
    //   - Parameters must be primitive types (string, Guid, int...) — no lambda Expressions.
    //   - Works with async via EF.CompileAsyncQuery.
    //
    // For most CRUD applications the improvement is negligible. Measure before optimising.
    private static readonly Func<AppDbContext, string, IAsyncEnumerable<Customer>> _getByEmailQuery =
        EF.CompileAsyncQuery((AppDbContext ctx, string email) =>
            ctx.Customers.Where(c => c.Email.Value == email));

    public async Task<Customer?> GetCustomerByEmailCompiledAsync(string email, CancellationToken ct = default)
    {
        await foreach (var customer in _getByEmailQuery(db, email).WithCancellation(ct))
            return customer;
        return null;
    }
}
