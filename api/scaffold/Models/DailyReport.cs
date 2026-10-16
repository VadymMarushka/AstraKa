using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class DailyReport
{
    public Guid Id { get; set; }

    public Guid WandererId { get; set; }

    public Guid CohortId { get; set; }

    public DateOnly Date { get; set; }

    public byte HarmonyLevel { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual ICollection<ExerciseCompletion> ExerciseCompletions { get; set; } = new List<ExerciseCompletion>();
}
