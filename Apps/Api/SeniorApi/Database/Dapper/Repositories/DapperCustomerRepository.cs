using System.Data;
using Dapper;
using Domain.Customers;
using Domain.Orders;

namespace Database.Dapper.Repositories;

public class DapperCustomerRepository(SqlConnectionFactory connectionFactory) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, Name, Email, CreatedAtUtc, UpdatedAtUtc
            FROM Customers
            WHERE Id = @Id
            """;

        var row = await connection.QuerySingleOrDefaultAsync<CustomerRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : ToDomain(row);
    }

    /// <summary>
    /// Two round trips on purpose: one for the customer, one (multi-mapped) for their orders and
    /// items. EF Core's .Include(c => c.Orders).ThenInclude(o => o.Items) decides for you whether
    /// to generate a single giant JOIN or split into multiple SELECTs (AsSplitQuery) - with Dapper
    /// that decision is yours to make explicitly, query by query.
    /// </summary>
    public async Task<Customer?> GetWithOrdersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await GetByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT o.Id, o.CustomerId, o.Status, o.RowVersion, o.CreatedAtUtc, o.UpdatedAtUtc,
                   i.Id, i.OrderId, i.ProductId, i.Quantity, i.UnitPrice
            FROM Orders o
            LEFT JOIN OrderItems i ON i.OrderId = o.Id
            WHERE o.CustomerId = @CustomerId
            ORDER BY o.Id, i.Id
            """;

        var ordersById = new Dictionary<Guid, Order>();

        // splitOn marks where each result column block starts a new mapped type. The first column
        // of OrderRow ("o.Id") is implicit; "Id" here tells Dapper the OrderItemRow block starts at
        // the second "Id" column in the result set.
        await connection.QueryAsync<OrderRow, OrderItemRow, Order>(
            new CommandDefinition(sql, new { CustomerId = id }, cancellationToken: cancellationToken),
            (orderRow, itemRow) =>
            {
                if (!ordersById.TryGetValue(orderRow.Id, out var order))
                {
                    order = ToDomain(orderRow);
                    ordersById[order.Id] = order;
                    customer.AttachExistingOrder(order);
                }

                // LEFT JOIN means an order with zero items still comes back as one row with every
                // OrderItemRow column null - see the comment on OrderItemRow for why that requires
                // nullable properties plus this explicit check instead of relying on defaults.
                if (itemRow.Id is Guid itemId)
                {
                    order.AttachExistingItem(OrderItem.Rehydrate(itemId, itemRow.OrderId!.Value, itemRow.ProductId!.Value, itemRow.Quantity!.Value, itemRow.UnitPrice!.Value));
                }

                return order;
            },
            splitOn: "Id");

        return customer;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = "SELECT Id, Name, Email, CreatedAtUtc, UpdatedAtUtc FROM Customers ORDER BY Name";

        var rows = await connection.QueryAsync<CustomerRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string sql = """
            INSERT INTO Customers (Id, Name, Email, CreatedAtUtc, UpdatedAtUtc)
            VALUES (@Id, @Name, @Email, @CreatedAtUtc, @UpdatedAtUtc)
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { customer.Id, customer.Name, Email = customer.Email.Value, customer.CreatedAtUtc, customer.UpdatedAtUtc },
            transaction,
            cancellationToken: cancellationToken));

        transaction.Commit();
    }

    private static Customer ToDomain(CustomerRow row) =>
        Customer.Rehydrate(row.Id, row.Name, Email.Create(row.Email), row.CreatedAtUtc, row.UpdatedAtUtc);

    private static Order ToDomain(OrderRow row) =>
        Order.Rehydrate(row.Id, row.CustomerId, Enum.Parse<OrderStatus>(row.Status), row.RowVersion, row.CreatedAtUtc, row.UpdatedAtUtc);
}
