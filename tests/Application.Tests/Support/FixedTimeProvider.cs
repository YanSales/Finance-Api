namespace Application.Tests.Support;

/// <summary>Relogio controlado pelo teste.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
