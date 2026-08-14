using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.EntityFramework.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        // Enum -> string conversion. Stored as int by default; mapping to string trades a few
        // bytes per row for SQL that stays human-readable ("WHERE Status = 'Shipped'") and removes
        // the risk of silently reassigning meaning if someone reorders the enum members later.
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Optimistic concurrency token. SQL Server's `rowversion` column auto-increments on every
        // UPDATE; EF Core includes it in the WHERE clause of generated UPDATE statements and throws
        // DbUpdateConcurrencyException if zero rows matched (someone else changed it first).
        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        builder.HasIndex(o => o.CustomerId);

        // Order.Items follows the same "EF writes through the private backing field" pattern as
        // Customer.Orders above - required because the public surface is IReadOnlyCollection.
        builder.Metadata.FindNavigation(nameof(Order.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade); // deleting an order deletes its line items - they have no independent lifetime

        builder.Property(o => o.CreatedAtUtc).IsRequired();
    }
}
