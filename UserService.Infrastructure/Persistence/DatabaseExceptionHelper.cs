using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace UserService.Infrastructure.Persistence
{
    /// <summary>
    /// Provides logic to detect SQL Server unique constraint violation exceptions.
    /// </summary>
    /// <remarks>This class is intended for use with Entity Framework Core and SQL Server. It implements the
    /// IDatabaseExceptionDetector interface to identify exceptions caused by unique constraint violations, which
    /// typically occur when attempting to insert or update data that would result in duplicate key values.</remarks>
    public class SqlServerExceptionDetector : IDatabaseExceptionDetector
    {
        /// <summary>
        /// Determines whether the specified exception represents a SQL Server unique constraint violation.
        /// </summary>
        /// <remarks>This method checks for SQL Server error codes 2627 and 2601, which correspond to
        /// unique constraint and duplicate key violations, respectively. Use this method to detect when a database
        /// operation fails due to a unique constraint violation, such as inserting a duplicate value into a column with
        /// a unique index.</remarks>
        /// <param name="ex">The exception to evaluate for a unique constraint violation. Cannot be null.</param>
        /// <returns>true if the exception indicates a unique constraint violation; otherwise, false.</returns>
        public bool IsUniqueConstraintViolation(Exception ex)
        {
            if (ex is DbUpdateException dbEx &&
                dbEx.InnerException is SqlException sqlEx)
            {
                return sqlEx.Number == 2627 || sqlEx.Number == 2601;
            }

            return false;
        }
    }
}
