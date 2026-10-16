using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class Enrollment
{
    public Guid WandererId { get; set; }

    public Guid CohortId { get; set; }

    public DateTime EnrolledAt { get; set; }

    public byte StatusId { get; set; }

    public decimal PaidPrice { get; set; }

    public virtual Cohort Cohort { get; set; } = null!;

    public virtual ICollection<DailyReport> DailyReports { get; set; } = new List<DailyReport>();

    public virtual ICollection<Revelation> Revelations { get; set; } = new List<Revelation>();

    public virtual EnrollmentStatus Status { get; set; } = null!;

    public virtual Wanderer Wanderer { get; set; } = null!;
}
