using Domain.Common;

namespace Domain.Customers;

// Interface defined in Domain — the aggregate defines what it needs from storage, not the
// infrastructure layer. Implementations live in Database/EntityFramework and Database/Dapper.
// See Domain/Common/IAggregateRoot.cs for the full rationale behind this placement.
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Loads the full aggregate: Customer + Orders + OrderItems.
    // Kept separate from GetByIdAsync to avoid fetching the graph when you only need the root —
    // a generic IRepository.Get(Guid) cannot express this distinction.
    Task<Customer?> GetWithOrdersAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
}
