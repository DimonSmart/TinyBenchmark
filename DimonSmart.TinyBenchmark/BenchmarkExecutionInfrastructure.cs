using System.Diagnostics;

namespace DimonSmart.TinyBenchmark;

/// <summary>Provides monotonically increasing timestamps for a benchmark run.</summary>
public interface IBenchmarkTimeSource
{
    TimeSpan GetTimestamp();
}

/// <summary>Executes a synchronous benchmark invocation.</summary>
public interface IBenchmarkInvocationExecutor
{
    void Invoke(Action invocation);
}

internal sealed class StopwatchBenchmarkTimeSource : IBenchmarkTimeSource
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public TimeSpan GetTimestamp() => _stopwatch.Elapsed;
}

internal sealed class DirectBenchmarkInvocationExecutor : IBenchmarkInvocationExecutor
{
    public void Invoke(Action invocation) => invocation();
}
