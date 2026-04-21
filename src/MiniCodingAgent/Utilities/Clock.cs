namespace MiniCodingAgent.Utilities;

/// <summary>
/// Abstraction over the system clock so tests can pin time deterministically.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow();
}

/// <summary>
/// Default <see cref="IClock"/> backed by <see cref="DateTimeOffset.UtcNow"/>.
/// </summary>
public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    public DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;
}
