using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class Starshepherd
{
    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    public virtual User User { get; set; } = null!;
}
