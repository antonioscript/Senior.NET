namespace Application.Abstractions.Persistence;

/// <summary>
/// Commits the work done against the repositories in this scope.
/// - EF Core implementation: a thin wrapper around DbContext.SaveChangesAsync. The change tracker
///   batches every Add/Update from every repository call in the request into one transaction, so
///   this is where that batch is actually sent to the database.
/// - Dapper implementation: each Dapper repository write (AddAsync/UpdateAsync) already opens its
///   own connection, runs in its own transaction, and commits before returning - there is no
///   change tracker to defer the write until later. SaveChangesAsync is therefore a deliberate
///   no-op there. That is a real limitation: composing two Dapper repository writes into one
///   atomic commit needs an explicitly shared IDbTransaction passed between them (or ambient
///   TransactionScope), not this interface. Spelling that out is more honest than pretending this
///   abstraction gives Dapper the same guarantee EF Core gets for free.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
