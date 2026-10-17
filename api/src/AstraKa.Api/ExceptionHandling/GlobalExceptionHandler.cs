using AstraKa.Application.Common;
using AstraKa.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Api.ExceptionHandling;

/// <summary>
/// The one place exceptions become HTTP responses, per CLAUDE.md: validation -> 400 with field
/// errors, <see cref="NotFoundException"/> -> 404, <see cref="DomainException"/> -> 409,
/// <see cref="DbUpdateConcurrencyException"/> -> 409, a uniqueness or FK-restrict violation that
/// reaches SQL Server directly -> 409 (whether wrapped in EF Core's <see cref="DbUpdateException"/>
/// or raised raw by ADO.NET code), everything else -> 500 with no details outside Development.
/// Every expected (non-500) case logs Information with just the message; only the 500 path logs
/// Error with the full exception.
/// </summary>
internal sealed class GlobalExceptionHandler(IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validationException => BuildValidationProblem(validationException),
            NotFoundException notFound => BuildProblem(StatusCodes.Status404NotFound, "Not found", notFound.Message),
            DomainException domainException => BuildProblem(StatusCodes.Status409Conflict, "Conflict", domainException.Message),
            DbUpdateConcurrencyException => BuildProblem(StatusCodes.Status409Conflict, "Conflict", "The resource was modified by someone else. Reload and try again."),
            DbUpdateException dbUpdateException when dbUpdateException.InnerException is SqlException sqlException && IsUniqueConstraintViolation(sqlException) =>
                BuildProblem(StatusCodes.Status409Conflict, "Conflict", "A record with these values already exists."),
            DbUpdateException dbUpdateException when dbUpdateException.InnerException is SqlException sqlException && IsForeignKeyViolation(sqlException) =>
                BuildProblem(StatusCodes.Status409Conflict, "Conflict", "This record is still referenced by other data and cannot be deleted."),
            // ADO.NET code (Infrastructure/Ado) talks to SQL Server directly - a uniqueness or FK
            // violation there surfaces as a raw SqlException, never wrapped in a DbUpdateException,
            // so it needs its own cases rather than falling through to the 500 fallback below.
            SqlException sqlException when IsUniqueConstraintViolation(sqlException) =>
                BuildProblem(StatusCodes.Status409Conflict, "Conflict", "A record with these values already exists."),
            SqlException sqlException when IsForeignKeyViolation(sqlException) =>
                BuildProblem(StatusCodes.Status409Conflict, "Conflict", "This operation references data that doesn't exist, or data that is still in use."),
            _ => BuildProblem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", environment.IsDevelopment() ? exception.ToString() : null),
        };

        // Expected exceptions (anything that isn't the 500 fallback) are routine - just the
        // message, no stack trace. Only genuinely unexpected ones get logged at Error with the
        // full exception.
        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("{Method} {Path} -> {Status}: {Message}", httpContext.Request.Method, httpContext.Request.Path, problem.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    /// <summary>
    /// A uniqueness violation (unique constraint 2627 or unique index 2601) that reached SQL Server
    /// directly - distinct from the onboarding 409s, which are raised deliberately as
    /// <see cref="DomainException"/> before ever reaching the database. This is the safety net for
    /// the cases that get past an application-level check (e.g. a race), for both EF Core (wrapped
    /// in <see cref="DbUpdateException"/>) and ADO.NET (raised raw) callers.
    /// </summary>
    private static bool IsUniqueConstraintViolation(SqlException exception)
    {
        return exception.Number == SqlErrorNumbers.UniqueConstraintViolation || exception.Number == SqlErrorNumbers.UniqueIndexViolation;
    }

    /// <summary>
    /// A Restrict FK violation (or a CHECK constraint violation - SQL Server uses 547 for both)
    /// reaching SQL Server directly - the safety net for the race between an explicit "is this
    /// still in use" check (e.g. <c>DeleteExerciseCommandHandler</c>) and the actual delete, for
    /// both EF Core and ADO.NET callers.
    /// </summary>
    private static bool IsForeignKeyViolation(SqlException exception)
    {
        return exception.Number == SqlErrorNumbers.ForeignKeyConstraintViolation;
    }

    private static ValidationProblemDetails BuildValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        };
    }

    private static ProblemDetails BuildProblem(int status, string title, string? detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
        };
    }
}
