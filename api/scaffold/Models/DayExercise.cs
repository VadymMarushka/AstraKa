using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class DayExercise
{
    public Guid Id { get; set; }

    public Guid CourseDayId { get; set; }

    public Guid ExerciseId { get; set; }

    public Guid? RevelationId { get; set; }

    public int OrderNumber { get; set; }

    public int Sets { get; set; }

    public int Amount { get; set; }

    public virtual CourseDay CourseDay { get; set; } = null!;

    public virtual Exercise Exercise { get; set; } = null!;

    public virtual ICollection<ExerciseCompletion> ExerciseCompletions { get; set; } = new List<ExerciseCompletion>();

    public virtual Revelation? Revelation { get; set; }
}
