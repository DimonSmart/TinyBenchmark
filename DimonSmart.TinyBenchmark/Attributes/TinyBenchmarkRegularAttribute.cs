namespace DimonSmart.TinyBenchmark.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public sealed class TinyBenchmarkRegularAttribute : TinyBenchmarkAttribute
{
    /// <summary>A small, bounded default that prepares regular benchmarks without affecting useful samples.</summary>
    public const int DefaultWarmupCount = 1;

    public TinyBenchmarkRegularAttribute(int requestedSampleCount)
        : this(requestedSampleCount, DefaultWarmupCount)
    {
    }

    /// <summary>Uses an explicit warm-up count; pass zero to opt out.</summary>
    public TinyBenchmarkRegularAttribute(int requestedSampleCount, int warmupCount)
    {
        if (requestedSampleCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedSampleCount), requestedSampleCount,
                "Requested sample count must be positive.");
        }

        if (warmupCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(warmupCount), warmupCount,
                "Warmup count cannot be negative.");
        }

        RequestedSampleCount = requestedSampleCount;
        WarmupCount = warmupCount;
    }

    /// <summary>Number of useful single-invocation samples to collect.</summary>
    public int RequestedSampleCount { get; }

    /// <summary>Preparation invocations performed before useful samples; zero explicitly disables warm-up.</summary>
    public int WarmupCount { get; }
}
