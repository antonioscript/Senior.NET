namespace Domain.Common;

/// <summary>
/// Base type for entities with identity-based equality.
/// Two entities are equal when their <see cref="Id"/> matches, regardless of other property values
/// (unlike value objects, which compare by value - see <c>ValueObject</c> in Domain.Common).
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other || obj.GetType() != GetType())
        {
            return false;
        }

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
