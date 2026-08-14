using Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Database.EntityFramework.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICustomerRepository"/>. Notice how thin this is - the
/// change tracker does most of the work; AddAsync just stages the entity, and the actual INSERT
/// only happens when EfUnitOfWork.SaveChangesAsync runs. Compare against
/// Database/Dapper/Repositories/DapperCustomerRepository.cs, which has to write every statement by hand.
/// </summary>
public class EfCustomerRepository(AppDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> GetWithOrdersAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Customers
            .Include(c => c.Orders)
            .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Customers.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await dbContext.Customers.AddAsync(customer, cancellationToken);
}
