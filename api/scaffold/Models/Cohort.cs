using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class Cohort
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public DateOnly StartDate { get; set; }

    public int SeatLimit { get; set; }

    public int SeatsTaken { get; set; }

    public byte[]? RowVersion { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
