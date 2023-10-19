namespace DimonSmart.TinyBenchmark;

public record MethodExecutionResults(MethodExecutionInfo Method, IList<TimeSpan> Times);