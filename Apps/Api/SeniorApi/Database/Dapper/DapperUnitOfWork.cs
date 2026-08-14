using Application.Abstractions.Persistence;

namespace Database.Dapper;

/// <summary>
/// See the XML doc on IUnitOfWork for the full explanation: every Dapper repository write already
/// opens its own connection/transaction and commits before returning, so there is nothing left to
/// flush here. This type exists only so SeniorApi can depend on IUnitOfWork uniformly regardless
/// of which persistence stack is registered (see Program.cs).
/// </summary>
public class DapperUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}
