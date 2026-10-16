using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class Exercise
{
    public Guid Id { get; set; }

    public Guid? AuthorId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public byte UnitId { get; set; }

    public string? VideoUrl { get; set; }

    public string Instructions { get; set; } = null!;

    public virtual Starshepherd? Author { get; set; }

    public virtual ICollection<DayExercise> DayExercises { get; set; } = new List<DayExercise>();

    public virtual ExerciseUnit Unit { get; set; } = null!;
}
