namespace Domain.Common;

/// <summary>
/// Adds creation/update timestamps. EF Core can populate these automatically via a
/// <c>SaveChanges</c> interceptor (see EntityFramework/Interceptors); Dapper repositories
/// must set them explicitly before INSERT/UPDATE since there is no change tracker.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id)
        : base(id)
    {
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    /// <summary>
    /// Overwrites the timestamps with values read back from storage. Only persistence code that
    /// reconstructs an entity outside of EF Core's own materialization (i.e. the Dapper
    /// repositories) should ever call this - see Domain/AssemblyInfo.cs.
    /// </summary>
    internal void SetAuditTimestamps(DateTime createdAtUtc, DateTime? updatedAtUtc)
    {
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }
}
