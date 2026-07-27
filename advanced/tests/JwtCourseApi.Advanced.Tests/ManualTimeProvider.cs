namespace JwtCourseApi.Advanced.Tests;

public sealed class ManualTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private readonly Lock _lock = new();
    private DateTimeOffset _utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_lock)
        {
            return _utcNow;
        }
    }

    public void Advance(TimeSpan amount)
    {
        lock (_lock)
        {
            _utcNow = _utcNow.Add(amount);
        }
    }
}
