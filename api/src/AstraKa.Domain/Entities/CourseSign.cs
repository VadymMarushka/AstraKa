namespace AstraKa.Domain.Entities;

/// <summary>
/// Explicit M:N join between <see cref="Course"/> and <see cref="ZodiacSign"/> ("Open to").
/// No rows for a course means it is open to any sign - see <see cref="Course.IsOpenToSign"/>.
/// </summary>
public class CourseSign
{
    public Guid CourseId { get; private set; }

    public int ZodiacSignId { get; private set; }

    public Course Course { get; set; } = null!;

    public ZodiacSign ZodiacSign { get; set; } = null!;

    private CourseSign()
    {
    }

    public CourseSign(Guid courseId, int zodiacSignId)
    {
        CourseId = courseId;
        ZodiacSignId = zodiacSignId;
    }
}
