namespace AstraKa.Application.Common;

/// <summary>
/// Thrown when a requested resource doesn't exist. One exception type, same shape as
/// <see cref="Domain.Exceptions.DomainException"/> - mapped to 404 by Api's exception handler.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
