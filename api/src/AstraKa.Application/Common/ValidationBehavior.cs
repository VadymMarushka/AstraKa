using FluentValidation;
using MediatR;

namespace AstraKa.Application.Common;

/// <summary>
/// The one MediatR pipeline behavior that runs validation - handlers never call validators
/// themselves, per CLAUDE.md. Runs every registered <see cref="IValidator{T}"/> for the request and,
/// if any fail, throws FluentValidation's own <see cref="ValidationException"/> instead of letting
/// the handler run; Api's <c>GlobalExceptionHandler</c> turns that into a 400 with field errors.
/// A request with no registered validator (most queries) passes straight through.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
