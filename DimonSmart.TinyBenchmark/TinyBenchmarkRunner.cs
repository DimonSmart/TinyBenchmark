using System.Diagnostics;
using System.Reflection;
using DimonSmart.TinyBenchmark.Exporters;
using DimonSmart.TinyBenchmark.Utils;
using static DimonSmart.TinyBenchmark.AttributeUtility;

namespace DimonSmart.TinyBenchmark;

public class TinyBenchmarkRunner : ITinyBenchmarkRunner
{
    private readonly BenchmarkData _data = new();
    private readonly Action<string>? _writeMessage;

    private TinyBenchmarkRunner(Action<string>? writeMessage)
    {
        _writeMessage = writeMessage;
    }

    public IResultProcessor Run()
    {
        var methodExecutionInfos = GetMethodExecutionInfos();
        var classesCount = methodExecutionInfos.Select(m => m.ClassType).Distinct().Count();
        _writeMessage?.Invoke($"Run TinyBenchmark for:{classesCount} classes");
        var results = methodExecutionInfos
            .ToDictionary(m => m, v => new List<TimeSpan>());
        long totalTicks = 0;

        // Measure average timing only if total run limit applied
        if (_data.BenchmarkDurationLimit.HasValue)
        {
            _writeMessage?.Invoke("1. Warming UP phase (time pre-calculation)");
            foreach (var result in results)
            {
                LogCurrentMethod(result.Key);
                PrepareRun();
                for (var i = 0; i < _data.BenchmarkDurationLimitInitIterations; i++)
                    result.Value.Add(MeasureExecutionTime(result.Key.Action));
            }

            // Remove
            totalTicks = results.Values
                .Select(t => (long)t.Select(i => i.Ticks).Average())
                .Sum();
            _writeMessage?.Invoke($"One time full run time is:{TimeSpan.FromTicks(totalTicks).FormatTimeSpan()}");
        }

        _writeMessage?.Invoke("2. Measuring phase");
        var totalLimit = _data.BenchmarkDurationLimit;

        foreach (var result in results)
        {
            var executionCount = _data.MinFunctionExecutionCount;
            if (totalLimit.HasValue)
            {
                // var thisRunTicks = (long)result.Value.Select(t => t.Ticks).Average();
                // Percentile50
                var thisRunTicks = result.Value.Percentile50().Ticks;
                var ticksLimit = thisRunTicks * totalLimit.Value.Ticks / (double)totalTicks;
                int? calculatedNumberOfExecutions =
                    (int)(ticksLimit * _data.BenchmarkDurationLimitInitIterations / totalTicks);
                executionCount = calculatedNumberOfExecutions.Value;
                _writeMessage?.Invoke($"Calculated run count:{calculatedNumberOfExecutions}");
            }

            if (executionCount <= _data.MinFunctionExecutionCount)
            {
                executionCount = _data.MinFunctionExecutionCount;
                _writeMessage?.Invoke($"Minimum count limit applied:{executionCount}");
            }

            if (executionCount > _data.MaxFunctionExecutionCount)
            {
                executionCount = _data.MaxFunctionExecutionCount.Value;
                _writeMessage?.Invoke($"Maximum count limit applied:{executionCount}");
            }

            LogCurrentMethod(result.Key);

            // second time warm up
            for (var i = 0; i < 10; i++) MeasureExecutionTime(result.Key.Action);

            for (var i = 0; i < executionCount; i++) result.Value.Add(MeasureExecutionTime(result.Key.Action));
        }

        _data.Results = results
            .Select(i => new MethodExecutionResults(i.Key, i.Value))
            .ToList();

        return new ResultProcessor(this, _data);
    }

    public ITinyBenchmarkRunner WithMaxRunExecutionTime(TimeSpan time, int preRunCount)
    {
        _data.BenchmarkDurationLimit = time;
        _data.BenchmarkDurationLimitInitIterations = preRunCount;
        return this;
    }

    public ITinyBenchmarkRunner WinMinMaxFunctionExecutionCount(int minFunctionExecutionCount,
        int? maxFunctionExecutionCount)
    {
        if (minFunctionExecutionCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minFunctionExecutionCount), minFunctionExecutionCount,
                "Must be greater then 1");
        }

        if (minFunctionExecutionCount > maxFunctionExecutionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFunctionExecutionCount), maxFunctionExecutionCount,
                $"Must be greater then {nameof(minFunctionExecutionCount)}");
        }

        _data.MinFunctionExecutionCount = minFunctionExecutionCount;
        _data.MaxFunctionExecutionCount = maxFunctionExecutionCount;
        return this;
    }

    private void LogCurrentMethod(MethodExecutionInfo method)
    {
        _writeMessage?.Invoke($"{method.ClassType.Name}.{method.MethodInfo.Name}({method.Parameter})");
    }

    public static ITinyBenchmarkRunner Create(Action<string>? writeMessage = null)
    {
        return new TinyBenchmarkRunner(writeMessage);
    }

    private static IReadOnlyCollection<MethodExecutionInfo> GetMethodExecutionInfos()
    {
        var executionInfos = new List<MethodExecutionInfo>();
        var classes = GetClassesUnderTest();
        foreach (var classUnderTestType in classes)
        {
            var propertyInfo = FindClassUnderTestParameterProperty(classUnderTestType);
            var parameters = GetParametersFromAttribute(propertyInfo);
            var methods = GetMethodsWithTinyBenchmarkAttribute(classUnderTestType);
            var instance = Activator.CreateInstance(classUnderTestType) ??
                           throw new Exception("Can't create class instance");

            foreach (var method in methods)
            {
                executionInfos.AddRange(GetMethodExecutionInfos(parameters, method, instance, classUnderTestType));
            }
        }

        return executionInfos;
    }

    private static void PrepareRun()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForFullGCComplete();
        GC.Collect();
    }

    internal static TimeSpan MeasureExecutionTime(Action action)
    {
        //PrepareRun();
        // GC.TryStartNoGCRegion(100 * 1024 * 1024);
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        // GC.EndNoGCRegion();
        return stopwatch.Elapsed;
    }

    internal static IEnumerable<MethodExecutionInfo> GetMethodExecutionInfos(
        object[]? parameters, MethodInfo methodInfo, object instance, Type classType)
    {
        var methodExecutionInfos = new List<MethodExecutionInfo>();
        if (parameters != null)
        {
            foreach (var parameterAttributeValue in parameters)
            {
                var methodExecutionInfo =
                    new MethodExecutionInfo(classType, methodInfo, parameterAttributeValue, Action);
                methodExecutionInfos.Add(methodExecutionInfo);
                continue;

                void Action()
                {
                    methodInfo.Invoke(instance, new[] { parameterAttributeValue });
                }
            }
        }
        else
        {
            var methodExecutionInfo = new MethodExecutionInfo(classType, methodInfo, null, Action);
            methodExecutionInfos.Add(methodExecutionInfo);

            void Action()
            {
                methodInfo.Invoke(instance, null);
            }
        }

        return methodExecutionInfos;
    }

    public TinyBenchmarkRunner WithoutRunExecutionTimeLimit()
    {
        _data.BenchmarkDurationLimit = null;
        return this;
    }

    public ITinyBenchmarkRunner WithPercentile50AsResult()
    {
        _data.GetResult = TimeSpanUtils.Percentile50;
        return this;
    }

    public ITinyBenchmarkRunner WithBestTimeAsResult()
    {
        _data.GetResult = TimeSpanUtils.BestResult;
        return this;
    }
}