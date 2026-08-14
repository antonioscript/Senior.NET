using Domain.Common;

namespace Domain.Orders;

// Interface defined in Domain — see ICustomerRepository.cs for the rationale.
public interface IOrderRepository
{
    // Always loads with items — OrderItem is never fetched standalone; it only exists as part of
    // the Order aggregate. This is enforced by having no IOrderItemRepository.
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    // EF Core: no-op (change tracker already knows what changed; SaveChanges flushes it).
    // Dapper: must issue explicit UPDATE + DELETE/INSERT for items (no tracker).
    // The different implementations behind the same interface is why this method exists here
    // rather than disappearing into EF's change-tracking magic.
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
