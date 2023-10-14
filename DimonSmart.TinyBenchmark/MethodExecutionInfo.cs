using System.Reflection;

namespace DimonSmart.TinyBenchmark;

public record MethodExecutionInfo(Type ClassType, MethodInfo MethodInfo, object? Parameter, Action Action);