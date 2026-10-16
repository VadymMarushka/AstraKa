using AstraKa.Domain.Enums;
using AstraKa.Domain.Exceptions;

namespace AstraKa.Domain.Entities;

/// <summary>
/// A program template made of <see cref="CourseDay"/>s, run as <see cref="Cohort"/>s
/// ("Constellations"). "Author or Oracle may edit" is authorization and lives in the Api's
/// resource-based <c>IAuthorizationHandler</c> (lab 3), not here.
/// </summary>
public class Course
{
    public Guid Id { get; private set; }

    /// <summary>FK to <see cref="Starshepherd"/> ("Leads").</summary>
    public Guid AuthorId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>What this course promises, beyond the platform-wide mission of Alignment.</summary>
    public string Promise { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public CourseStatus Status { get; private set; } = CourseStatus.Draft;

    public Starshepherd Author { get; private set; } = null!;

    public ICollection<CourseDay> CourseDays { get; private set; } = new List<CourseDay>();

    public ICollection<CourseSign> CourseSigns { get; private set; } = new List<CourseSign>();

    public ICollection<Cohort> Cohorts { get; private set; } = new List<Cohort>();

    private Course()
    {
    }

    public Course(Guid id, Guid authorId, string name, string description, string promise, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Course name cannot be empty.", nameof(name));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative.");
        }

        Id = id;
        AuthorId = authorId;
        Name = name;
        Description = description;
        Promise = promise;
        Price = price;
    }

    public void Update(string name, string description, string promise, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Course name cannot be empty.", nameof(name));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative.");
        }

        Name = name;
        Description = description;
        Promise = promise;
        Price = price;
    }

    /// <summary>
    /// Diffs against the current set rather than clearing and re-adding - with the composite key
    /// (CourseId, ZodiacSignId), removing and re-adding the same sign in one SaveChanges can be
    /// tracked incorrectly. Only signs that actually change are removed/added.
    /// </summary>
    public void ReplaceSigns(IEnumerable<int> zodiacSignIds)
    {
        var newIds = zodiacSignIds.Distinct().ToHashSet();

        foreach (var existing in CourseSigns.Where(cs => !newIds.Contains(cs.ZodiacSignId)).ToList())
        {
            CourseSigns.Remove(existing);
        }

        var currentIds = CourseSigns.Select(cs => cs.ZodiacSignId).ToHashSet();
        foreach (var zodiacSignId in newIds.Where(id => !currentIds.Contains(id)))
        {
            CourseSigns.Add(new CourseSign(Id, zodiacSignId));
        }
    }

    /// <summary>
    /// Requires <see cref="CourseDays"/> to have been loaded (e.g. via Include) - it's a private-set
    /// collection initialized empty, so an un-included course would otherwise always look day-less.
    /// </summary>
    public void Publish()
    {
        if (Status == CourseStatus.Published)
        {
            throw new DomainException("This course is already published.");
        }

        if (CourseDays.Count == 0)
        {
            throw new DomainException("A course with no days cannot be published.");
        }

        Status = CourseStatus.Published;
    }

    /// <summary>Guard for enrolling: the course must be open for enrollment.</summary>
    public void EnsurePublished()
    {
        if (Status != CourseStatus.Published)
        {
            throw new DomainException("This course is not published yet.");
        }
    }

    /// <summary>No <see cref="CourseSign"/> rows means the course is open to any sign.</summary>
    public bool IsOpenToSign(int zodiacSignId)
    {
        return CourseSigns.Count == 0 || CourseSigns.Any(cs => cs.ZodiacSignId == zodiacSignId);
    }
}
