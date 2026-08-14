using Dapper;
using Domain.Products;

namespace Database.Dapper.Repositories;

public class DapperProductRepository(SqlConnectionFactory connectionFactory) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, Sku, Name, UnitPrice, StockQuantity, CreatedAtUtc, UpdatedAtUtc
            FROM Products
            WHERE Id = @Id
            """;

        var row = await connection.QuerySingleOrDefaultAsync<ProductRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        const string sql = "SELECT Id, Sku, Name, UnitPrice, StockQuantity, CreatedAtUtc, UpdatedAtUtc FROM Products ORDER BY Name";

        var rows = await connection.QueryAsync<ProductRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(ToDomain).ToList();
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string sql = """
            INSERT INTO Products (Id, Sku, Name, UnitPrice, StockQuantity, CreatedAtUtc, UpdatedAtUtc)
            VALUES (@Id, @Sku, @Name, @UnitPrice, @StockQuantity, @CreatedAtUtc, @UpdatedAtUtc)
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { product.Id, product.Sku, product.Name, product.UnitPrice, product.StockQuantity, product.CreatedAtUtc, product.UpdatedAtUtc },
            transaction,
            cancellationToken: cancellationToken));

        transaction.Commit();
    }

    private static Product ToDomain(ProductRow row) =>
        Product.Rehydrate(row.Id, row.Sku, row.Name, row.UnitPrice, row.StockQuantity, row.CreatedAtUtc, row.UpdatedAtUtc);
}
