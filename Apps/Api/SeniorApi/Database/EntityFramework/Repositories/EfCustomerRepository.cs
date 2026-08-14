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

    // dbContext.Customers is a DbSet<Customer> — a type provided by EF Core (NuGet:
    // Microsoft.EntityFrameworkCore). DbSet<T> represents the collection of all Customer rows in
    // the database and exposes LINQ operators (Where, FirstOrDefault...) plus mutation methods.
    //
    // DbSet<T>.AddAsync does NOT execute any SQL. It only tells EF Core's change tracker
    // "this entity is new — when SaveChanges runs, generate an INSERT for it" by setting its
    // EntityState to EntityState.Added. The actual INSERT waits until EfUnitOfWork.SaveChangesAsync
    // is called at the end of the request.
    //
    // Why AddAsync instead of Add (synchronous)?
    //   Add() and AddAsync() behave identically for Guid/identity primary keys. AddAsync only
    //   becomes truly asynchronous when a value generator needs to read from the database to
    //   produce the key — for example, HiLo sequences in SQL Server (which pre-fetch a block of
    //   IDs with a SELECT NEXT VALUE FOR). For GUIDs (our case) the key is generated in memory by
    //   Guid.NewGuid() before this call, so Add() would be equally correct here. AddAsync is used
    //   for consistency with the async pattern of the rest of the method.
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await dbContext.Customers.AddAsync(customer, cancellationToken);
}
