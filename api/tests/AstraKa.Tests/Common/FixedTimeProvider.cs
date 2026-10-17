namespace AstraKa.Tests.Common;

/// <summary>Lets a test pin "now" instead of depending on the wall clock - no new NuGet package,
/// just a subclass of the BCL's own <see cref="TimeProvider"/>.</summary>
public sealed class FixedTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
}
