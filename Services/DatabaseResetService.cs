using GSoftPosNew.Data;
using Microsoft.EntityFrameworkCore;

namespace GSoftPosNew.Services
{
    public class DatabaseResetService
    {
        private readonly AppDbContext _context;

        public DatabaseResetService(AppDbContext context)
        {
            _context = context;
        }

        public async Task ResetDatabaseExceptUsersAsync()
        {
            var tableNames = _context.Model.GetEntityTypes()
                .Select(t => t.GetTableName())
                .Distinct()
                .Where(t => t != "Users") // keep Users table
                .ToList();

            // Disable constraints
            await _context.Database.ExecuteSqlRawAsync("EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

            foreach (var table in tableNames)
            {
                // DELETE instead of TRUNCATE → avoids FK issues
                await _context.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]");
            }

            // Enable constraints again
            await _context.Database.ExecuteSqlRawAsync("EXEC sp_msforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
        }
    }
}
