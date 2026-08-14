using Domain.Orders;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.EntityFramework.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Quantity).IsRequired();

        builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);

        // OrderItem deliberately has no `Product Product { get; }` navigation - it only needs the
        // id to know what was ordered, and a price snapshot it already owns. HasOne<Product>()
        // (no navigation expression) wires up the FK without forcing a domain reference that the
        // aggregate doesn't actually need.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict); // never let deleting a product cascade into historical order lines
    }
}
