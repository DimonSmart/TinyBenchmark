using System.Reflection;

namespace DimonSmart.TinyBenchmark;

public record MethodExecutionInformation(Type ClassType, MethodInfo MethodInfo, object? Parameter, Action Action);