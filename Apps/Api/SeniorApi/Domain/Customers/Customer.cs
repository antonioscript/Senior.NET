using Domain.Common;
using Domain.Orders;

namespace Domain.Customers;

public class Customer : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public Email Email { get; private set; }

    // Read-only navigation: Order is its own aggregate root (created via Order.Create, persisted
    // through IOrderRepository), so Customer never mutates this list itself. EF Core still populates
    // it directly through the backing field when a query does .Include(c => c.Orders) - see
    // CustomerConfiguration for the Metadata API call that allows field-only access here.
    private readonly List<Order> _orders = [];
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    // Parameterless constructor required by EF Core to materialize entities without
    // calling application code; keep it private so nothing outside the ORM can use it.
    private Customer()
    {
        Name = string.Empty;
        Email = null!;
    }

    private Customer(Guid id, string name, Email email)
        : base(id)
    {
        Name = name;
        Email = email;
    }

    public static Customer Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        return new Customer(Guid.NewGuid(), name, Email.Create(email));
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        Name = name;
        Touch();
    }

    /// <summary>
    /// Rebuilds a Customer from rows already read out of the database. Used only by
    /// Database/Dapper/Repositories/DapperCustomerRepository.cs - EF Core never calls this because
    /// its change tracker materializes entities directly via reflection instead.
    /// </summary>
    internal static Customer Rehydrate(Guid id, string name, Email email, DateTime createdAtUtc, DateTime? updatedAtUtc)
    {
        var customer = new Customer(id, name, email);
        customer.SetAuditTimestamps(createdAtUtc, updatedAtUtc);
        return customer;
    }

    /// <summary>Re-attaches an Order that already exists in storage (as opposed to a brand-new one created via Order.Create).</summary>
    internal void AttachExistingOrder(Order order) => _orders.Add(order);
}
