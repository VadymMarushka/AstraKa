namespace AstraKa.Api.ExceptionHandling;

/// <summary>
/// Named SQL Server error numbers this layer reacts to - uniqueness and FK violations that reach
/// SQL Server directly. <see cref="ForeignKeyConstraintViolation"/> mirrors
/// Infrastructure/Ado's own constant of the same value; kept separate since they're different
/// layers' concerns (that one reacts to it in ADO.NET deletes, this one in EF Core saves).
/// </summary>
internal static class SqlErrorNumbers
{
    /// <summary>"Violation of UNIQUE KEY constraint" / "Violation of PRIMARY KEY constraint".</summary>
    public const int UniqueConstraintViolation = 2627;

    /// <summary>"Cannot insert duplicate key row ... with unique index".</summary>
    public const int UniqueIndexViolation = 2601;

    /// <summary>"The DELETE/UPDATE statement conflicted with the REFERENCE constraint".</summary>
    public const int ForeignKeyConstraintViolation = 547;
}
