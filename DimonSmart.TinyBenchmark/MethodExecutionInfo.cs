using System.Reflection;

namespace DimonSmart.TinyBenchmark;

public record MethodExecutionInfo(Type ClassTyp, MethodInfo MethodInfo, object? Parameter, Action Action);