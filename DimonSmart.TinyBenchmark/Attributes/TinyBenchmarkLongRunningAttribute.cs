namespace DimonSmart.TinyBenchmark.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public sealed class TinyBenchmarkLongRunningAttribute : TinyBenchmarkAttribute
{
    public TinyBenchmarkLongRunningAttribute(int requestedSampleCount)
    {
        if (requestedSampleCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedSampleCount), requestedSampleCount,
                "Requested sample count must be positive.");
        }

        RequestedSampleCount = requestedSampleCount;
    }

    public int RequestedSampleCount { get; }
}
