using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class EnrollmentStatus
{
    public byte Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
