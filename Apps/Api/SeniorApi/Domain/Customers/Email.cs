using System.Text.RegularExpressions;

namespace Domain.Customers;

/// <summary>
/// Value object: compared by value, immutable, self-validating.
/// EF Core maps this as an "owned type" (Configurations/CustomerConfiguration.cs) so it stays a
/// single column in the database while remaining a real type in the domain model.
/// Dapper has no concept of owned types - the Dapper repository maps the raw string column back
/// into an Email manually, which is exactly the kind of plumbing EF Core gives you for free.
/// </summary>
public sealed partial record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(value));
        }

        if (!EmailRegex().IsMatch(value))
        {
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));
        }

        return new Email(value.Trim().ToLowerInvariant());
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
