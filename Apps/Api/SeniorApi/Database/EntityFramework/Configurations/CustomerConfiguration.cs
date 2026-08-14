using Domain.Customers;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.EntityFramework.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="Customer"/>.
/// One IEntityTypeConfiguration{T} per aggregate keeps mapping concerns out of the DbContext and
/// out of the domain entity itself (no [Required]/[MaxLength] data-annotation attributes on
/// Customer - the domain model stays persistence-ignorant).
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Email is a value object (record), not a primitive. OwnsOne tells EF Core to store it as
        // columns on the *same* Customers table (no separate Email table/join) while still letting
        // the domain model use a real Email type with its own validation in Email.Create.
        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(320) // RFC 5321 max email length
                .IsRequired();

            email.HasIndex(e => e.Value).IsUnique();
        });

        // Customer.Orders is exposed as IReadOnlyCollection<Order> with no public Add method - EF
        // Core needs to know it's allowed to write directly into the private `_orders` backing
        // field when materializing query results. Without this, EF would look for a way to add to
        // an IReadOnlyCollection and fail.
        builder.Metadata.FindNavigation(nameof(Customer.Orders))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany<Order>(nameof(Customer.Orders))
            .WithOne()
            .HasForeignKey(nameof(Order.CustomerId))
            .OnDelete(DeleteBehavior.Restrict); // don't let a customer delete cascade into their order history

        builder.Property(c => c.CreatedAtUtc).IsRequired();
    }
}
