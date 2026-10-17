using AstraKa.Domain.Enums;
using AstraKa.Domain.Exceptions;

namespace AstraKa.Domain.Entities;

/// <summary>
/// One run of a <see cref="Course"/> (UI name "Constellation"): a start date and a limited number
/// of seats. <see cref="SeatsTaken"/> is a deliberate denormalization so that two simultaneous
/// enrollments for the last seat update the same row, letting <see cref="RowVersion"/> catch the
/// conflict.
/// </summary>
public class Cohort
{
    public Guid Id { get; private set; }

    /// <summary>FK to <see cref="Course"/> ("Runs as").</summary>
    public Guid CourseId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public int SeatLimit { get; private set; }

    public int SeatsTaken { get; private set; }

    /// <summary>Concurrency token, configured by Infrastructure as a SQL Server rowversion column.</summary>
    public byte[]? RowVersion { get; private set; }

    public Course Course { get; private set; } = null!;

    public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();

    private Cohort()
    {
    }

    public Cohort(Guid id, Guid courseId, DateOnly startDate, int seatLimit)
    {
        if (seatLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seatLimit), seatLimit, "Seat limit must be greater than zero.");
        }

        Id = id;
        CourseId = courseId;
        StartDate = startDate;
        SeatLimit = seatLimit;
        SeatsTaken = 0;
    }

    /// <summary>Guard for enrolling: at least one seat must still be free.</summary>
    public void EnsureHasFreeSeat()
    {
        if (SeatsTaken >= SeatLimit)
        {
            throw new DomainException("The last seat has been taken.");
        }
    }

    /// <summary>Guard for enrolling: joining is only allowed before the cohort has started.</summary>
    public void EnsureNotStarted(DateOnly today)
    {
        if (today >= StartDate)
        {
            throw new DomainException("This constellation has already started.");
        }
    }

    /// <summary>Takes one seat, after re-checking that one is still free.</summary>
    public void TakeSeat()
    {
        EnsureHasFreeSeat();
        SeatsTaken++;
    }

    /// <summary>Guard for scheduling: a constellation must start after today. Seed data is exempt
    /// - it is constructed directly through the constructor, never through this.</summary>
    public void EnsureScheduledInFuture(DateOnly today, DateOnly startDate)
    {
        if (startDate <= today)
        {
            throw new DomainException("A constellation must be scheduled to start after today.");
        }
    }

    /// <summary>
    /// Full update - cannot run once the cohort has started, the *new* StartDate must still be in
    /// the future (otherwise an upcoming cohort could be moved into the past), and seats already
    /// taken can never be orphaned by shrinking the limit below them.
    /// </summary>
    public void Update(DateOnly today, DateOnly startDate, int seatLimit)
    {
        EnsureNotStarted(today);
        EnsureScheduledInFuture(today, startDate);

        if (seatLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seatLimit), seatLimit, "Seat limit must be greater than zero.");
        }

        if (seatLimit < SeatsTaken)
        {
            throw new DomainException($"Seat limit cannot be set below the {SeatsTaken} seat(s) already taken.");
        }

        StartDate = startDate;
        SeatLimit = seatLimit;
    }

    /// <summary>Last day of the course, derived from how many days the course program has.</summary>
    public DateOnly GetEndDate(int courseDayCount)
    {
        return StartDate.AddDays(courseDayCount - 1);
    }

    /// <summary>Derived timeline position relative to today.</summary>
    public CohortPhase GetPhase(DateOnly today, DateOnly endDate)
    {
        if (today < StartDate)
        {
            return CohortPhase.NotStarted;
        }

        return today > endDate ? CohortPhase.Finished : CohortPhase.Running;
    }

    /// <summary>1-based day number of the program that falls on <paramref name="date"/>.</summary>
    public int GetDayNumber(DateOnly date)
    {
        return date.DayNumber - StartDate.DayNumber + 1;
    }
}
