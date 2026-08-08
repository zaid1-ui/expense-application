using System.Data;
using Microsoft.Data.SqlClient;

namespace ExpenseApp.Services
{
    // Replaces ExpenseAppDbContext for all data access — every controller now
    // opens a raw connection through here and calls a stored procedure via
    // Dapper. No LINQ-to-SQL translation, no change tracking.
    public class DapperContext
    {
        private readonly string _connectionString;

        public DapperContext(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing DefaultConnection connection string.");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
