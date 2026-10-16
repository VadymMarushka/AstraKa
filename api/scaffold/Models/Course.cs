using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class Course
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Promise { get; set; } = null!;

    public decimal Price { get; set; }

    public byte StatusId { get; set; }

    public virtual Starshepherd Author { get; set; } = null!;

    public virtual ICollection<Cohort> Cohorts { get; set; } = new List<Cohort>();

    public virtual ICollection<CourseDay> CourseDays { get; set; } = new List<CourseDay>();

    public virtual CourseStatus Status { get; set; } = null!;

    public virtual ICollection<ZodiacSign> ZodiacSigns { get; set; } = new List<ZodiacSign>();
}
