using Dapper;
using Domain.Orders;

namespace Database.Dapper.Repositories;

public class DapperOrderRepository(SqlConnectionFactory connectionFactory) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var orders = await QueryOrdersWithItemsAsync("WHERE o.Id = @Id", new { Id = id }, cancellationToken);
        return orders.SingleOrDefault();
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await QueryOrdersWithItemsAsync("WHERE o.CustomerId = @CustomerId", new { CustomerId = customerId }, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        // RowVersion is a SQL Server `rowversion` column - the engine generates it, you cannot
        // INSERT a value into it. OUTPUT inserted.RowVersion reads back what the engine assigned
        // so the in-memory Order can be kept in sync with the row that now exists in the database.
        const string insertOrderSql = """
            INSERT INTO Orders (Id, CustomerId, Status, CreatedAtUtc, UpdatedAtUtc)
            OUTPUT inserted.RowVersion
            VALUES (@Id, @CustomerId, @Status, @CreatedAtUtc, @UpdatedAtUtc)
            """;

        var rowVersion = await connection.QuerySingleAsync<byte[]>(new CommandDefinition(
            insertOrderSql,
            new { order.Id, order.CustomerId, Status = order.Status.ToString(), order.CreatedAtUtc, order.UpdatedAtUtc },
            transaction,
            cancellationToken: cancellationToken));

        order.SetRowVersion(rowVersion);

        const string insertItemSql = """
            INSERT INTO OrderItems (Id, OrderId, ProductId, Quantity, UnitPrice)
            VALUES (@Id, @OrderId, @ProductId, @Quantity, @UnitPrice)
            """;

        foreach (var item in order.Items)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                insertItemSql,
                new { item.Id, item.OrderId, item.ProductId, item.Quantity, item.UnitPrice },
                transaction,
                cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }

    /// <summary>
    /// Mirrors what EF Core's change tracker + DbUpdateConcurrencyException give you automatically:
    /// the WHERE clause checks RowVersion explicitly, and zero affected rows means someone else
    /// updated this order first. OrderItem has no in-place edit in this domain model (items are
    /// added once, never changed), so syncing the child rows is a delete-then-reinsert rather than
    /// a diff - simplest correct option for the handful of rows a typical order has.
    /// </summary>
    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string updateOrderSql = """
            UPDATE Orders
            SET Status = @Status, UpdatedAtUtc = @UpdatedAtUtc
            WHERE Id = @Id AND RowVersion = @RowVersion
            """;

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            updateOrderSql,
            new { order.Id, Status = order.Status.ToString(), order.UpdatedAtUtc, order.RowVersion },
            transaction,
            cancellationToken: cancellationToken));

        if (affected == 0)
        {
            throw new InvalidOperationException($"Order '{order.Id}' was modified by another process; reload it before retrying.");
        }

        const string deleteItemsSql = "DELETE FROM OrderItems WHERE OrderId = @OrderId";
        await connection.ExecuteAsync(new CommandDefinition(deleteItemsSql, new { OrderId = order.Id }, transaction, cancellationToken: cancellationToken));

        const string insertItemSql = """
            INSERT INTO OrderItems (Id, OrderId, ProductId, Quantity, UnitPrice)
            VALUES (@Id, @OrderId, @ProductId, @Quantity, @UnitPrice)
            """;

        foreach (var item in order.Items)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                insertItemSql,
                new { item.Id, item.OrderId, item.ProductId, item.Quantity, item.UnitPrice },
                transaction,
                cancellationToken: cancellationToken));
        }

        transaction.Commit();
    }

    private async Task<List<Order>> QueryOrdersWithItemsAsync(string whereClause, object parameters, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();

        // whereClause is always a literal passed by the methods above, never user input - safe to
        // interpolate. Every actual value still flows through `parameters`, fully parameterized.
        var sql = $"""
            SELECT o.Id, o.CustomerId, o.Status, o.RowVersion, o.CreatedAtUtc, o.UpdatedAtUtc,
                   i.Id, i.OrderId, i.ProductId, i.Quantity, i.UnitPrice
            FROM Orders o
            LEFT JOIN OrderItems i ON i.OrderId = o.Id
            {whereClause}
            ORDER BY o.Id, i.Id
            """;

        var ordersById = new Dictionary<Guid, Order>();

        await connection.QueryAsync<OrderRow, OrderItemRow, Order>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken),
            (orderRow, itemRow) =>
            {
                if (!ordersById.TryGetValue(orderRow.Id, out var order))
                {
                    order = Order.Rehydrate(orderRow.Id, orderRow.CustomerId, Enum.Parse<OrderStatus>(orderRow.Status), orderRow.RowVersion, orderRow.CreatedAtUtc, orderRow.UpdatedAtUtc);
                    ordersById[order.Id] = order;
                }

                if (itemRow.Id is Guid itemId)
                {
                    order.AttachExistingItem(OrderItem.Rehydrate(itemId, itemRow.OrderId!.Value, itemRow.ProductId!.Value, itemRow.Quantity!.Value, itemRow.UnitPrice!.Value));
                }

                return order;
            },
            splitOn: "Id");

        return ordersById.Values.ToList();
    }
}
