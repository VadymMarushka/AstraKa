using AstraKa.Domain.Enums;
using AstraKa.Domain.Exceptions;

namespace AstraKa.Domain.Entities;

/// <summary>
/// Explicit M:N join between <see cref="Wanderer"/> and <see cref="Cohort"/> ("Joins" / "In"), keyed
/// by the pair itself rather than a surrogate id. Money is never refunded on <see cref="Leave"/>.
/// </summary>
public class Enrollment
{
    public Guid WandererId { get; private set; }

    public Guid CohortId { get; private set; }

    public DateTime EnrolledAt { get; private set; }

    public EnrollmentStatus Status { get; private set; } = EnrollmentStatus.Active;

    /// <summary>Snapshot of <see cref="Course.Price"/> at enrollment time - a separate fact, not a duplicate.</summary>
    public decimal PaidPrice { get; private set; }

    public Wanderer Wanderer { get; private set; } = null!;

    public Cohort Cohort { get; private set; } = null!;

    public ICollection<Revelation> Revelations { get; private set; } = new List<Revelation>();

    public ICollection<DailyReport> DailyReports { get; private set; } = new List<DailyReport>();

    private Enrollment()
    {
    }

    public Enrollment(Guid wandererId, Guid cohortId, decimal paidPrice, DateTime enrolledAt)
    {
        if (paidPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(paidPrice), paidPrice, "Paid price cannot be negative.");
        }

        WandererId = wandererId;
        CohortId = cohortId;
        PaidPrice = paidPrice;
        EnrolledAt = enrolledAt;
    }

    /// <summary>Active -> Left. Money already paid is never refunded.</summary>
    public void Leave()
    {
        if (Status == EnrollmentStatus.Left)
        {
            throw new DomainException("This enrollment has already been left.");
        }

        Status = EnrollmentStatus.Left;
    }

    /// <summary>"Completed" is computed, not stored: still active when the cohort finished.</summary>
    public bool IsCompleted(CohortPhase cohortPhase)
    {
        return Status == EnrollmentStatus.Active && cohortPhase == CohortPhase.Finished;
    }
}
