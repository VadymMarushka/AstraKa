using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class CourseStatus
{
    public byte Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
