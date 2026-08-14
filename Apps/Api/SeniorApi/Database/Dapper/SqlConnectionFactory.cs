using System.Data;
using Microsoft.Data.SqlClient;

namespace Database.Dapper;

// ---------------------------------------------------------------------------------------------
// DAPPER - "what is this and why" cheat sheet
// ---------------------------------------------------------------------------------------------
// Dapper is a micro-ORM: it does exactly one job - map the rows from an ADO.NET IDataReader onto
// your C# objects - and nothing else. No change tracker, no LINQ-to-SQL translation, no migrations.
// You write the SQL; Dapper just saves you from hand-rolled SqlDataReader["ColumnName"] casting.
// That makes it fast and predictable, at the cost of having to write (and keep in sync) every
// query and every INSERT/UPDATE/DELETE by hand - the opposite tradeoff from EF Core.
//
// NuGet packages used in this project (Database.csproj) and what each one is for:
//   - Dapper
//       Adds the extension methods (Query, QueryAsync, Execute, QueryFirstOrDefault, etc.) on top
//       of IDbConnection. That's the entire library - a few thousand lines, no provider model.
//   - Microsoft.Data.SqlClient
//       The ADO.NET driver Dapper extends. This is the SAME package the EF Core SqlServer provider
//       uses under the hood (see EntityFramework/AppDbContext.cs) - Dapper and EF Core are two
//       different layers of abstraction over the identical connection/driver.
//
// Connections are intentionally NOT held open for the lifetime of a repository or registered as a
// singleton: ADO.NET connection pooling already recycles physical connections behind the scenes,
// so the correct pattern is "open short-lived, dispose promptly" - `using var connection = ...` in
// every repository method, exactly like the ones in Repositories/.
// ---------------------------------------------------------------------------------------------
public class SqlConnectionFactory(string connectionString)
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
