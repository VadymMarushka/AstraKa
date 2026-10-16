namespace AstraKa.Tests.Common;

/// <summary>
/// Serializes every test class in this collection so they share one <see cref="DatabaseFixture"/>
/// (and the one LocalDB database it owns) without running concurrently against it.
/// </summary>
[CollectionDefinition("Database")]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
