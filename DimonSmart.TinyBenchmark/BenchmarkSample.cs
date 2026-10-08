namespace DimonSmart.TinyBenchmark;

/// <summary>A single useful measurement, optionally representing a batch of operations.</summary>
public sealed class BenchmarkSample
{
    public BenchmarkSample(long sequence, TimeSpan elapsed, long operationCount, string caseId, bool isColdStart, long? allocatedBytes = null)
    {
        if (sequence < 0)
            throw new ArgumentOutOfRangeException(nameof(sequence));
        if (elapsed < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        if (operationCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(operationCount));
        if (string.IsNullOrWhiteSpace(caseId))
            throw new ArgumentException("A case identity is required.", nameof(caseId));
        if (allocatedBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(allocatedBytes));

        Sequence = sequence;
        Elapsed = elapsed;
        OperationCount = operationCount;
        CaseId = caseId;
        IsColdStart = isColdStart;
        AllocatedBytes = allocatedBytes;
    }

    public long Sequence { get; }
    public TimeSpan Elapsed { get; }
    public long OperationCount { get; }
    public string CaseId { get; }
    public bool IsColdStart { get; }
    /// <summary>Bytes allocated by the current thread during this useful invocation, when available.</summary>
    public long? AllocatedBytes { get; }

    public double PerOperationNanoseconds => Elapsed.TotalNanoseconds / OperationCount;
}
