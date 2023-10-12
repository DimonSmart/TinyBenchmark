namespace DimonSmart.TinyBenchmark;

public record MethodExecutionResults(MethodExecutionInfo Method, IReadOnlyCollection<TimeSpan> Times);