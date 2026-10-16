using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class ExerciseCompletion
{
    public Guid Id { get; set; }

    public Guid DailyReportId { get; set; }

    public Guid DayExerciseId { get; set; }

    public DateTime CompletedAt { get; set; }

    public virtual DailyReport DailyReport { get; set; } = null!;

    public virtual DayExercise DayExercise { get; set; } = null!;
}
