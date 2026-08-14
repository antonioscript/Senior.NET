using Domain.Common;

namespace Domain.Orders;

/// <summary>
/// Child entity owned by <see cref="Order"/> - it has no meaning outside an order, so there is no
/// IOrderItemRepository; it is only ever loaded/saved as part of its parent aggregate.
/// </summary>
public class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    // Snapshot of the product price at the time the order was placed - intentionally NOT a live
    // reference to Product.UnitPrice, so historical orders don't change value when prices move.
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private OrderItem()
    {
    }

    private OrderItem(Guid id, Guid orderId, Guid productId, int quantity, decimal unitPrice)
        : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal OrderItem(Guid orderId, Guid productId, int quantity, decimal unitPrice)
        : this(Guid.NewGuid(), orderId, productId, quantity, unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }
    }

    /// <summary>
    /// Rebuilds an OrderItem from a row already read out of the database, preserving its original
    /// Id instead of minting a new one. Skips the constructor validation above on purpose: the row
    /// was validated once already when it was first written, and EF Core's own materialization
    /// (private parameterless ctor + direct property writes) skips it too, so Dapper stays
    /// consistent with how the "free" ORM path behaves.
    /// </summary>
    internal static OrderItem Rehydrate(Guid id, Guid orderId, Guid productId, int quantity, decimal unitPrice) =>
        new(id, orderId, productId, quantity, unitPrice);
}
