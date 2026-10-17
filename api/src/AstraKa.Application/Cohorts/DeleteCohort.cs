using AstraKa.Application.Common;
using AstraKa.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Cohorts;

/// <summary>No author/Oracle check here - that's the Api's resource-based <c>AuthorOrOracle</c> policy.</summary>
public sealed record DeleteCohortCommand(Guid Id) : IRequest;

public sealed class DeleteCohortCommandHandler(IAppDbContext db) : IRequestHandler<DeleteCohortCommand>
{
    public async Task Handle(DeleteCohortCommand request, CancellationToken cancellationToken)
    {
        var cohort = await db.Cohorts.SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (cohort is null)
        {
            throw new NotFoundException($"Cohort {request.Id} does not exist.");
        }

        // Explicit check, not a raw FK error - GlobalExceptionHandler's 547 case is only the
        // safety net for the race between this check and the delete below.
        var hasEnrollments = await db.Enrollments.AnyAsync(e => e.CohortId == request.Id, cancellationToken);
        if (hasEnrollments)
        {
            throw new DomainException("This constellation has enrollments and cannot be deleted.");
        }

        db.Cohorts.Remove(cohort);
        await db.SaveChangesAsync(cancellationToken);
    }
}
