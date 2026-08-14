using Domain.Orders;

namespace Application.Abstractions.Persistence;

// Result records used by ILinqShowcaseRepository.
// They live in Application (not Domain) because they are query-model shapes - read-side
// projections optimised for display, not domain aggregates enforcing business rules.

public record ProductSummary(Guid Id, string Name, decimal Price, int StockQuantity);

// GroupBy aggregation result: one row per customer with rolled-up financials.
public record CustomerRevenue(Guid CustomerId, string CustomerName, decimal TotalRevenue, int OrderCount);

// GroupBy Count result: how many orders exist per status.
public record OrderCountByStatus(OrderStatus Status, int Count);

// SelectMany result: each item flattened out, carrying its parent order ID.
public record OrderItemFlat(
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
