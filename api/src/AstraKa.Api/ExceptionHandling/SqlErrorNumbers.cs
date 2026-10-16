namespace AstraKa.Api.ExceptionHandling;

/// <summary>
/// Named SQL Server error numbers this layer reacts to. Not the same concern as
/// Infrastructure/Ado's own <c>SqlErrorNumbers</c> (that one's about Restrict FK violations on
/// delete) - this one is about uniqueness violations reaching SQL Server directly.
/// </summary>
internal static class SqlErrorNumbers
{
    /// <summary>"Violation of UNIQUE KEY constraint" / "Violation of PRIMARY KEY constraint".</summary>
    public const int UniqueConstraintViolation = 2627;

    /// <summary>"Cannot insert duplicate key row ... with unique index".</summary>
    public const int UniqueIndexViolation = 2601;
}
