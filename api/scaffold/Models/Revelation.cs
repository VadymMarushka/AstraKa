using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class Revelation
{
    public Guid Id { get; set; }

    public Guid WandererId { get; set; }

    public Guid CohortId { get; set; }

    public Guid CourseDayId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual CourseDay CourseDay { get; set; } = null!;

    public virtual ICollection<DayExercise> DayExercises { get; set; } = new List<DayExercise>();

    public virtual Enrollment Enrollment { get; set; } = null!;
}
