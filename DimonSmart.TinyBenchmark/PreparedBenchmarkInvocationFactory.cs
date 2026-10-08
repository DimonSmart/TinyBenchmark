using System.Linq.Expressions;
using System.Reflection;

namespace DimonSmart.TinyBenchmark;

internal static class PreparedBenchmarkInvocationFactory
{
    internal static Action Create(MethodInfo method, object instance, object? parameter)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(instance);

        var parameters = method.GetParameters();
        if (method.ContainsGenericParameters ||
            typeof(Task).IsAssignableFrom(method.ReturnType) ||
            method.ReturnType == typeof(ValueTask) ||
            parameters.Length > 1 ||
            parameters.Any(methodParameter => methodParameter.ParameterType.IsByRef || methodParameter.IsOut))
        {
            throw new InvalidOperationException(
                $"Benchmark method '{method.Name}' must be synchronous and have no parameters or one value parameter.");
        }

        if (parameters.Length == 0 && parameter is not null)
        {
            throw new InvalidOperationException($"Benchmark method '{method.Name}' does not accept a parameter.");
        }

        if (parameters.Length == 1 && parameter is null)
        {
            throw new InvalidOperationException($"Benchmark method '{method.Name}' requires a parameter.");
        }

        try
        {
            var target = method.IsStatic
                ? null
                : Expression.Convert(Expression.Constant(instance), method.DeclaringType!);
            var arguments = parameters.Length == 0
                ? Array.Empty<Expression>()
                : new[] { Expression.Convert(Expression.Constant(parameter), parameters[0].ParameterType) };
            var call = Expression.Call(target, method, arguments);
            Expression body = method.ReturnType == typeof(void)
                ? call
                : Expression.Block(call, Expression.Empty());

            return Expression.Lambda<Action>(body).Compile();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new InvalidOperationException($"Benchmark method '{method.Name}' cannot be prepared for invocation.", exception);
        }
    }
}
