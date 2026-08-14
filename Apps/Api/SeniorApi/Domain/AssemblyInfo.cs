using System.Runtime.CompilerServices;

// EF Core never needs this: it materializes entities via the private parameterless constructor and
// writes properties directly through reflection, bypassing visibility entirely. Dapper has no such
// mechanism for rebuilding an aggregate's *internal collections* (Order.Items, Customer.Orders), so
// the Database assembly is granted access to a small set of internal `Rehydrate`/`AttachExisting*`
// members on these entities - used only by the Dapper repositories, never by application code.
[assembly: InternalsVisibleTo("Database")]
