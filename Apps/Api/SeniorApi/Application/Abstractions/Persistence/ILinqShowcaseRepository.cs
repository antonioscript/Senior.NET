using Domain.Customers;
using Domain.Orders;
using Domain.Products;

namespace Application.Abstractions.Persistence;

// Each method here maps 1-to-1 to one LINQ concept or EF Core feature. Implementations live in
// Database/EntityFramework/Repositories/EfLinqShowcaseRepository.cs with detailed comments.
// There is intentionally no Dapper counterpart: LINQ is specific to IQueryable / EF Core.
public interface ILinqShowcaseRepository
{
    // 1. SELECT projection — avoid loading columns you don't need
    Task<IReadOnlyList<ProductSummary>> GetProductSummariesAsync(CancellationToken ct = default);

    // 2. WHERE + OrderBy + Skip/Take — filtering, sorting and pagination in one query
    Task<IReadOnlyList<Order>> GetOrdersPagedAsync(
        OrderStatus status, int page, int pageSize, CancellationToken ct = default);

    // 3. GroupBy + Sum — revenue rolled up per customer
    Task<IReadOnlyList<CustomerRevenue>> GetRevenueByCustomerAsync(CancellationToken ct = default);

    // 4. GroupBy + Count — how many orders exist per status
    Task<IReadOnlyList<OrderCountByStatus>> GetOrderCountByStatusAsync(CancellationToken ct = default);

    // 5. Any — generates SQL EXISTS, much cheaper than Count() > 0 for existence checks
    Task<bool> HasLowStockProductsAsync(int threshold, CancellationToken ct = default);

    // 6. All — generates SQL NOT EXISTS (equivalent to "none violate the condition")
    Task<bool> AllProductsInStockAsync(CancellationToken ct = default);

    // 7. SelectMany — flatten nested collections; equivalent to a JOIN in SQL
    Task<IReadOnlyList<OrderItemFlat>> GetAllItemsByCustomerAsync(Guid customerId, CancellationToken ct = default);

    // 8. AsSplitQuery — load a customer + orders + items via separate queries instead of one
    //    giant cartesian-product JOIN (see implementation comment for when this matters)
    Task<IReadOnlyList<Customer>> GetCustomersWithOrdersSplitAsync(CancellationToken ct = default);

    // 9. FromSql — escape hatch to raw SQL; EF Core still maps results and parameterises safely
    Task<IReadOnlyList<Product>> SearchProductsByNameAsync(string term, CancellationToken ct = default);

    // 10. Compiled query — pre-compiled LINQ expression; avoids re-translating to SQL on every call
    Task<Customer?> GetCustomerByEmailCompiledAsync(string email, CancellationToken ct = default);
}
