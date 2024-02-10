namespace DimonSmart.TinyBenchmark;

public record MethodExecutionResults(MethodExecutionInformation Method, IList<MethodExecutionMetrics> Numbers);