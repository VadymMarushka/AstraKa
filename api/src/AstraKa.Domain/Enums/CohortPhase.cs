namespace AstraKa.Domain.Enums;

/// <summary>
/// Derived timeline position of a <see cref="Entities.Cohort"/>. Never stored - computed from
/// today's date and the cohort's start/end dates, see <see cref="Entities.Cohort.GetPhase"/>.
/// </summary>
public enum CohortPhase
{
    NotStarted,
    Running,
    Finished,
}
