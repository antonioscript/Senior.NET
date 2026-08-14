namespace Domain.Common;

// ------------------------------------------------------------------------------------------------
// REPOSITORY PATTERN — MICROSOFT'S APPROACH
// ------------------------------------------------------------------------------------------------
// Microsoft's guidance (eShopOnContainers, Clean Architecture samples) defines repositories this
// way:
//   1. One repository interface per aggregate root — NOT a generic IRepository<T> that every
//      entity can use.
//   2. The interface lives in the Domain layer, not in Application or Infrastructure. The domain
//      defines what it needs from storage; the infrastructure provides it.
//   3. Only aggregate roots have repositories. OrderItem, for example, must never have its own
//      IOrderItemRepository — it is only ever accessed through Order.
//
// This marker interface (no methods, no properties) is what enforces rule 3 at compile time.
// If you try to write IRepository<OrderItem>, the `where T : IAggregateRoot` constraint
// (see the concrete repository interfaces in each aggregate folder) will fail to compile,
// because OrderItem does not implement IAggregateRoot.
//
// Why NOT a generic IRepository<T>?
//   A generic interface with Add/GetById/GetAll/Update/Delete methods encourages treating all
//   aggregates the same way. It hides important semantic differences:
//     - GetByIdAsync vs GetWithOrdersAsync: the first loads only the root; the second loads the
//       full aggregate graph. A generic Get can't express that distinction.
//     - UpdateAsync: for EF Core this can be a no-op (change tracker handles it); for Dapper it
//       needs explicit SQL. A generic Update() pretends both are the same.
//   Aggregate-specific interfaces make these differences visible and intentional.
// ------------------------------------------------------------------------------------------------
public interface IAggregateRoot { }
