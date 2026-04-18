namespace UserService.Infrastructure.Persistence
{
    /// <summary>
    /// Defines a contract for detecting specific types of database exceptions, such as unique constraint violations.
    /// </summary>
    public interface IDatabaseExceptionDetector
    {
        /// <summary>
        /// Determines whether the specified exception represents a unique constraint violation error.
        /// </summary>
        /// <remarks>Use this method to detect database errors caused by attempts to insert or update data
        /// that would violate a unique constraint, such as duplicate keys. The specific exception types and messages
        /// considered may depend on the underlying data provider.</remarks>
        /// <param name="ex">The exception to evaluate for a unique constraint violation. Cannot be null.</param>
        /// <returns>true if the exception indicates a unique constraint violation; otherwise, false.</returns>
        bool IsUniqueConstraintViolation(Exception ex);
    }
}
