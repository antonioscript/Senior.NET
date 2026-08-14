using Application.Abstractions.Persistence;
using Domain.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SeniorApi.Controllers;

/// <summary>
/// Each endpoint here exercises one LINQ / EF Core concept. The real value is in the
/// implementation: Database/EntityFramework/Repositories/EfLinqShowcaseRepository.cs.
/// This controller just exposes the results so you can call them with curl or Swagger
/// and see what the data actually looks like. Run with alice's token (admin role).
/// </summary>
[ApiController]
[Route("api/linq")]
[Authorize]
public class LinqShowcaseController(ILinqShowcaseRepository repo) : ControllerBase
{
    // GET /api/linq/products/summary
    // Demonstrates: Select projection — only the columns you need reach the wire.
    [HttpGet("products/summary")]
    public async Task<IActionResult> ProductSummaries(CancellationToken ct) =>
        Ok(await repo.GetProductSummariesAsync(ct));

    // GET /api/linq/orders/paged?status=Pending&page=1&pageSize=5
    // Demonstrates: Where + OrderBy + Skip/Take — one SQL statement.
    [HttpGet("orders/paged")]
    public async Task<IActionResult> OrdersPaged(
        [FromQuery] OrderStatus status = OrderStatus.Pending,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken ct = default) =>
        Ok(await repo.GetOrdersPagedAsync(status, page, pageSize, ct));

    // GET /api/linq/revenue-by-customer
    // Demonstrates: GroupBy + Sum — SQL GROUP BY with aggregates.
    [HttpGet("revenue-by-customer")]
    public async Task<IActionResult> RevenueByCustomer(CancellationToken ct) =>
        Ok(await repo.GetRevenueByCustomerAsync(ct));

    // GET /api/linq/orders/count-by-status
    // Demonstrates: GroupBy + Count.
    [HttpGet("orders/count-by-status")]
    public async Task<IActionResult> OrderCountByStatus(CancellationToken ct) =>
        Ok(await repo.GetOrderCountByStatusAsync(ct));

    // GET /api/linq/products/has-low-stock?threshold=5
    // Demonstrates: Any() → SQL EXISTS (cheaper than Count() > 0).
    [HttpGet("products/has-low-stock")]
    public async Task<IActionResult> HasLowStock([FromQuery] int threshold = 5, CancellationToken ct = default) =>
        Ok(new { HasLowStock = await repo.HasLowStockProductsAsync(threshold, ct) });

    // GET /api/linq/products/all-in-stock
    // Demonstrates: All() → SQL NOT EXISTS.
    [HttpGet("products/all-in-stock")]
    public async Task<IActionResult> AllInStock(CancellationToken ct) =>
        Ok(new { AllInStock = await repo.AllProductsInStockAsync(ct) });

    // GET /api/linq/customers/{id}/all-items
    // Demonstrates: SelectMany — two levels of nesting flattened into one list.
    [HttpGet("customers/{id:guid}/all-items")]
    public async Task<IActionResult> AllItemsByCustomer(Guid id, CancellationToken ct) =>
        Ok(await repo.GetAllItemsByCustomerAsync(id, ct));

    // GET /api/linq/customers/with-orders-split
    // Demonstrates: AsSplitQuery — three separate SELECTs instead of one cartesian JOIN.
    [HttpGet("customers/with-orders-split")]
    public async Task<IActionResult> CustomersWithOrdersSplit(CancellationToken ct) =>
        Ok(await repo.GetCustomersWithOrdersSplitAsync(ct));

    // GET /api/linq/products/search?term=laptop
    // Demonstrates: FromSql with interpolation — raw SQL, safely parameterised by EF Core.
    [HttpGet("products/search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string term = "", CancellationToken ct = default) =>
        Ok(await repo.SearchProductsByNameAsync(term, ct));

    // GET /api/linq/customers/by-email?email=alice@example.com
    // Demonstrates: compiled query — expression tree translated once, reused on every call.
    [HttpGet("customers/by-email")]
    public async Task<IActionResult> CustomerByEmail([FromQuery] string email = "", CancellationToken ct = default)
    {
        var customer = await repo.GetCustomerByEmailCompiledAsync(email, ct);
        return customer is null ? NotFound() : Ok(customer);
    }
}
