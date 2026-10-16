namespace AstraKa.Domain.Exceptions;

/// <summary>
/// Thrown when a business rule on an entity is violated (e.g. a cohort has no free seats left, or
/// an enrollment is left twice). One exception type for every such rule, each raised with a message
/// that explains exactly what went wrong - deliberately not a type hierarchy. Application decides how
/// to map it to an HTTP response.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
