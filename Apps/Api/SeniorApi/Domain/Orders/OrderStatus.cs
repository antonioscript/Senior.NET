namespace Domain.Orders;

/// <summary>
/// Stored as a string in the database (see OrderConfiguration's HasConversion call) instead of the
/// default int, so the column stays readable in ad-hoc SQL/Dapper queries and reordering this enum
/// later doesn't silently corrupt existing rows.
/// </summary>
public enum OrderStatus
{
    Pending,
    Paid,
    Shipped,
    Completed,
    Cancelled
}
