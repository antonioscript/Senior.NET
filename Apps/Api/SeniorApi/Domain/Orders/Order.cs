using Domain.Common;

namespace Domain.Orders;

/// <summary>
/// Aggregate root: <see cref="OrderItem"/> can only be added/removed through this type, which keeps
/// invariants (status transitions, non-empty items, positive quantities) in one place instead of
/// scattered across repositories or controllers.
/// </summary>
public class Order : AuditableEntity, IAggregateRoot
{
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }

    // Optimistic concurrency token. EF Core maps this to a rowversion/timestamp column
    // (OrderConfiguration.IsRowVersion) and throws DbUpdateConcurrencyException on a stale write.
    // Dapper gets no such protection for free - the Dapper repository has to check this value
    // explicitly in its UPDATE ... WHERE clause to get the same guarantee.
    public byte[] RowVersion { get; private set; } = [];

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal Total => _items.Sum(i => i.LineTotal);

    private Order()
    {
    }

    private Order(Guid id, Guid customerId)
        : base(id)
    {
        CustomerId = customerId;
        Status = OrderStatus.Pending;
    }

    public static Order Create(Guid customerId) => new(Guid.NewGuid(), customerId);

    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot add items to an order in '{Status}' status.");
        }

        _items.Add(new OrderItem(Id, productId, quantity, unitPrice));
        Touch();
    }

    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Only pending orders can be paid; current status is '{Status}'.");
        }

        if (_items.Count == 0)
        {
            throw new InvalidOperationException("Cannot pay for an order with no items.");
        }

        Status = OrderStatus.Paid;
        Touch();
    }

    public void Ship()
    {
        if (Status != OrderStatus.Paid)
        {
            throw new InvalidOperationException($"Only paid orders can ship; current status is '{Status}'.");
        }

        Status = OrderStatus.Shipped;
        Touch();
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot cancel an order that is already '{Status}'.");
        }

        Status = OrderStatus.Cancelled;
        Touch();
    }

    /// <summary>Rebuilds an Order from a row already read out of the database - see Customer.Rehydrate for why this exists.</summary>
    internal static Order Rehydrate(Guid id, Guid customerId, OrderStatus status, byte[] rowVersion, DateTime createdAtUtc, DateTime? updatedAtUtc)
    {
        var order = new Order(id, customerId)
        {
            Status = status,
            RowVersion = rowVersion
        };
        order.SetAuditTimestamps(createdAtUtc, updatedAtUtc);
        return order;
    }

    /// <summary>Re-attaches an OrderItem that already exists in storage (as opposed to a brand-new one created via AddItem).</summary>
    internal void AttachExistingItem(OrderItem item) => _items.Add(item);

    /// <summary>
    /// Refreshes the concurrency token after an INSERT. SQL Server generates the rowversion value
    /// itself - you cannot supply one - so the Dapper repository reads it back via
    /// `OUTPUT inserted.RowVersion` and pushes it onto this in-memory instance afterwards. EF Core
    /// does the equivalent automatically for every rowversion/identity column on SaveChanges.
    /// </summary>
    internal void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;
}
