using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class ZodiacSign
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int SequenceNumber { get; set; }

    public virtual ICollection<Wanderer> Wanderers { get; set; } = new List<Wanderer>();

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
