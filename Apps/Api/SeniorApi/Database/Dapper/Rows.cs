namespace Database.Dapper;

// Dapper *can* populate private constructors/private setters directly via reflection (it skips
// visibility checks the same way EF Core's materializer does), but doing that here would hide the
// exact translation step we want to keep visible for this learning project. Instead, every query
// maps onto one of these plain, public-setter row types - the literal shape of a result set - and
// the repository explicitly converts each row into a domain object via the entity's internal
// Rehydrate factory (Domain/Customers/Customer.cs etc). That conversion step is where invariants
// would be re-checked if these tables could contain data written by something other than this app.

internal sealed class CustomerRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

internal sealed class ProductRow
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

internal sealed class OrderRow
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Every column here is nullable even though none of them are nullable in the OrderItems table:
/// this row only ever appears on the right-hand side of a LEFT JOIN (an order with zero items
/// still has to come back as one row), and Dapper throws trying to assign a SQL NULL to a
/// non-nullable value type like Guid or int. Nullable properties + an explicit null check in the
/// repository is the correct way to model "this side of the LEFT JOIN didn't match anything."
/// </summary>
internal sealed class OrderItemRow
{
    public Guid? Id { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? ProductId { get; set; }
    public int? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}
