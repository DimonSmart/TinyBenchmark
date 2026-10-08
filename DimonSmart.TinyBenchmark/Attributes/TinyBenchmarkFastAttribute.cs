namespace DimonSmart.TinyBenchmark.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public sealed class TinyBenchmarkFastAttribute : TinyBenchmarkAttribute
{
    public TinyBenchmarkFastAttribute(int requestedSampleCount)
        : this(requestedSampleCount, null)
    {
    }

    public TinyBenchmarkFastAttribute(int requestedSampleCount, int batchSize)
        : this(requestedSampleCount, (int?)batchSize)
    {
    }

    private TinyBenchmarkFastAttribute(int requestedSampleCount, int? batchSize)
    {
        if (requestedSampleCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedSampleCount), requestedSampleCount,
                "Requested sample count must be positive.");
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize,
                "Batch size must be positive when specified.");
        }

        RequestedSampleCount = requestedSampleCount;
        BatchSize = batchSize;
    }

    public int RequestedSampleCount { get; }

    public int? BatchSize { get; }
}
