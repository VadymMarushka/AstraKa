using System;
using System.Collections.Generic;

namespace AstraKa.Infrastructure;

public partial class CourseDay
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public int DayNumber { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? MeetLink { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<DayExercise> DayExercises { get; set; } = new List<DayExercise>();

    public virtual ICollection<Revelation> Revelations { get; set; } = new List<Revelation>();
}
