using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class ExerciseUnit
{
    public byte Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
