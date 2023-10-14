using CsvHelper.Configuration.Attributes;

namespace DimonSmart.TinyBenchmark;

public record FlatMethodExecutionResult(string ClassName, string MethodName, [Name("Parameter")] object? Parameter,
    TimeSpan Time);