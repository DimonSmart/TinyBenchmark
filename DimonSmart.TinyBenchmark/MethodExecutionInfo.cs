using System.Reflection;

namespace DimonSmart.TinyBenchmark;

public class MethodExecutionInfo
{
    public Type ClassType { get; init; }
    public MethodInfo MethodInfo { get; init; }
    public object? Parameter { get; init; }
    public Action Action { get; init; }
}