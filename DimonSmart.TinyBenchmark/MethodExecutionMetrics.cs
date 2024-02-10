namespace DimonSmart.TinyBenchmark;

public record MethodExecutionMetrics(TimeSpan PureMethodTime, TimeSpan MethodMeasureTime, long MemoryUsed);
