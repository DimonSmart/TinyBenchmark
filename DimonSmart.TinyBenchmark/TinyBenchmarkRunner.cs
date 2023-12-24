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
        var resultFolderPath = Path.Combine(Directory.GetCurrentDirectory(), ExporterBaseClass.ResultsFolder);
        var folderUri = new Uri(resultFolderPath).AbsoluteUri;
        Log($"Result folder:{folderUri}");
        var methodExecutionInfos = GetMethodExecutionInfos();
        var classesCount = methodExecutionInfos.Select(m => m.ClassType).Distinct().Count();
        Log($"Run TinyBenchmark for:{classesCount} classes");
        var results = methodExecutionInfos
            .ToDictionary(m => m, v => new List<TimeSpan>());
        long totalTicks = 0;

        if (_data.BenchmarkDurationLimit.HasValue)
        {
            Log("1. Warming UP phase (time pre-calculation)");
            foreach (var result in results)
            {
                LogCurrentMethod(result.Key);
                PrepareRun();
                for (var i = 0; i < _data.BenchmarkDurationLimitInitIterations; i++)
                    result.Value.Add(MeasureExecutionTime(result.Key.Action));
            }

            totalTicks = results.Values
                .Select(t => (long)t.Select(i => i.Ticks).Average())
                .Sum();
            Log($"One time full run time is:{TimeSpan.FromTicks(totalTicks).FormatTimeSpan()}");
        }

        Log("2. Measuring phase");
        var totalLimit = _data.BenchmarkDurationLimit;

        foreach (var result in results)
        {
            var executionCount = _data.MinFunctionExecutionCount;
            if (totalLimit.HasValue)
            {
                var thisRunTicks = result.Value.CalculatePercentile(50).Ticks;
                var ticksLimit = thisRunTicks * totalLimit.Value.Ticks / (double)totalTicks;
                int? calculatedNumberOfExecutions =
                    (int)(ticksLimit * _data.BenchmarkDurationLimitInitIterations / totalTicks);
                executionCount = calculatedNumberOfExecutions.Value;
                Log($"Calculated run count:{calculatedNumberOfExecutions}");
            }

            if (executionCount <= _data.MinFunctionExecutionCount)
            {
                executionCount = _data.MinFunctionExecutionCount;
                Log($"Minimum count limit applied:{executionCount}");
            }

            if (executionCount > _data.MaxFunctionExecutionCount)
            {
                executionCount = _data.MaxFunctionExecutionCount.Value;
                Log($"Maximum count limit applied:{executionCount}");
            }

            LogCurrentMethod(result.Key);

            // Pre measure warm up
            for (var i = 0; i < 10; i++)
            {
                MeasureExecutionTime(result.Key.Action);
            }

            for (var i = 0; i < executionCount; i++)
            {
                result.Value.Add(MeasureExecutionTime(result.Key.Action));
            }
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
        Log($"{method.ClassType.Name}.{method.MethodInfo.Name}({method.Parameter})");
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

    private void Log(string message)
    {
        _writeMessage?.Invoke(message);
    }
}