namespace DocFlow.Tests;

public sealed class TestClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => _now;
    public DateTime Now => _now.UtcDateTime;
    public void Advance(TimeSpan by) => _now += by;
}
