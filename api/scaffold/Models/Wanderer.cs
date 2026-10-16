using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class Wanderer
{
    public Guid UserId { get; set; }

    public int ZodiacSignId { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual User User { get; set; } = null!;

    public virtual ZodiacSign ZodiacSign { get; set; } = null!;
}
