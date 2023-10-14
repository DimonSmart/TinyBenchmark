namespace DimonSmart.TinyBenchmark;

public record FlatMethodExecutionResult(
    string ClassName,
    string MethodName,
    object? Parameter,
    TimeSpan Time);