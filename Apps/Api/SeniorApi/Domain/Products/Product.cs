using Domain.Common;

namespace Domain.Products;

public class Product : AuditableEntity, IAggregateRoot
{
    public string Sku { get; private set; }
    public string Name { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    private Product()
    {
        Sku = string.Empty;
        Name = string.Empty;
    }

    private Product(Guid id, string sku, string name, decimal unitPrice, int stockQuantity)
        : base(id)
    {
        Sku = sku;
        Name = name;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
    }

    public static Product Create(string sku, string name, decimal unitPrice, int stockQuantity)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        }

        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");
        }

        return new Product(Guid.NewGuid(), sku, name, unitPrice, stockQuantity);
    }

    public void ChangePrice(decimal newUnitPrice)
    {
        if (newUnitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newUnitPrice), "Unit price cannot be negative.");
        }

        UnitPrice = newUnitPrice;
        Touch();
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (quantity > StockQuantity)
        {
            throw new InvalidOperationException($"Insufficient stock for '{Sku}': requested {quantity}, available {StockQuantity}.");
        }

        StockQuantity -= quantity;
        Touch();
    }

    /// <summary>Rebuilds a Product from a row already read out of the database - see Customer.Rehydrate for why this exists.</summary>
    internal static Product Rehydrate(Guid id, string sku, string name, decimal unitPrice, int stockQuantity, DateTime createdAtUtc, DateTime? updatedAtUtc)
    {
        var product = new Product(id, sku, name, unitPrice, stockQuantity);
        product.SetAuditTimestamps(createdAtUtc, updatedAtUtc);
        return product;
    }
}
