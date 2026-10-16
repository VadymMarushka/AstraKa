namespace AstraKa.Infrastructure.Ado;

/// <summary>Named SQL Server error numbers this layer reacts to, instead of bare magic numbers.</summary>
internal static class SqlErrorNumbers
{
    /// <summary>"The DELETE/UPDATE statement conflicted with the REFERENCE constraint" - a Restrict FK blocked the write.</summary>
    public const int ForeignKeyConstraintViolation = 547;
}
