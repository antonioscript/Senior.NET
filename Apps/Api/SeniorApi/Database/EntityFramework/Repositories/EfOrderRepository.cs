using Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Database.EntityFramework.Repositories;

public class EfOrderRepository(AppDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await dbContext.Orders.AddAsync(order, cancellationToken);

    // The change tracker already knows about every mutation made through Order's domain methods
    // (AddItem, MarkAsPaid, Ship, Cancel) as long as the instance was loaded from this same
    // DbContext - so there is nothing to do here beyond letting EfUnitOfWork call SaveChangesAsync.
    // This method exists purely to satisfy the ORM-agnostic interface; compare with
    // DapperOrderRepository.UpdateAsync, which has to issue real UPDATE statements.
    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
