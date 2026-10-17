using AstraKa.Application.Common;
using AstraKa.Domain.Entities;
using AstraKa.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AstraKa.Application.Enrollments;

/// <summary>No body - <c>CohortId</c> comes from the route, the Wanderer from <see cref="ICurrentUser"/>.</summary>
public sealed record EnrollCommand(Guid CohortId) : IRequest;

public sealed class EnrollCommandHandler(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IRequestHandler<EnrollCommand>
{
    public async Task Handle(EnrollCommand request, CancellationToken cancellationToken)
    {
        var wandererId = currentUser.UserId!.Value;

        // Explicit transaction around the whole operation (reads + writes), deliberately, for the
        // lab report: it makes "enroll" one unit, even though the default READ COMMITTED isolation
        // means these reads don't block anyone else. The actual race for the last seat isn't won
        // by this transaction - it's caught optimistically by Cohort.RowVersion below, since both
        // concurrent requests update the same Cohort row.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var cohort = await db.Cohorts
                .Include(c => c.Course)
                .ThenInclude(co => co.CourseSigns)
                .SingleOrDefaultAsync(c => c.Id == request.CohortId, cancellationToken);

            if (cohort is null)
            {
                throw new NotFoundException($"Cohort {request.CohortId} does not exist.");
            }

            cohort.Course.EnsurePublished();

            var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
            cohort.EnsureNotStarted(today);

            cohort.EnsureHasFreeSeat();

            var zodiacSignId = await db.Wanderers
                .Where(w => w.UserId == wandererId)
                .Select(w => w.ZodiacSignId)
                .SingleAsync(cancellationToken);

            if (!cohort.Course.IsOpenToSign(zodiacSignId))
            {
                throw new DomainException("This course is not open to your zodiac sign.");
            }

            var alreadyEnrolled = await db.Enrollments.AnyAsync(e => e.WandererId == wandererId && e.CohortId == request.CohortId, cancellationToken);
            if (alreadyEnrolled)
            {
                throw new DomainException("You are already enrolled in this constellation.");
            }

            var enrollment = new Enrollment(wandererId, cohort.Id, cohort.Course.Price, timeProvider.GetUtcNow().UtcDateTime);
            cohort.TakeSeat();
            db.Enrollments.Add(enrollment);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new DomainException("The last seat has just been taken.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
