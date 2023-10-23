using System;
using System.Diagnostics;
using System.Reflection;
using DimonSmart.TinyBenchmark.Exporters;
using static System.Collections.Specialized.BitVector32;
using static DimonSmart.TinyBenchmark.AttributeUtility;

namespace DimonSmart.TinyBenchmark;

public class TinyBenchmarkRunner : ITinyBenchmarkRunner
{
    private readonly Action<string> _writeMessage;
    private BenchmarkData _data = new();

    private TinyBenchmarkRunner(Action<string> writeMessage)
    {
        _writeMessage = writeMessage;
    }

    public ITinyBenchmarkRunner Reset()
    {
        _data = new BenchmarkData();
        return this;
    }

    public IResultProcessor Run()
    {
        var methodExecutionInfos = GetMethodExecutionInfos();

        var results = methodExecutionInfos
            .ToDictionary(m => m, v => new List<TimeSpan>());
        foreach (var result in results)
        {
            for (var i = 0; i < _data.WarmUpCount; i++)
            {
                result.Value.Add(MeasureExecutionTime(result.Key.Action));
            }
        }

        var total = results.Values
                .Select(t => t.Average(i => i.TotalMicroseconds))
                .Sum();

        var totalLimit = _data.MaxRunExecutionTime;

        foreach (var result in results)
        {
            var thisRunTime = result.Value.Select(s => s.TotalMicroseconds).Average();
            int? calculatedNumberOfExecutions = null;
            if (totalLimit.HasValue)
                calculatedNumberOfExecutions = (int)(totalLimit.Value.TotalMicroseconds / (thisRunTime * results.Count));
            var executionCount = _data.MinFunctionExecutionCount;
            if (calculatedNumberOfExecutions > executionCount)
                executionCount = calculatedNumberOfExecutions.Value;
            if (executionCount > _data.MaxFunctionExecutionCount)
                executionCount = _data.MaxFunctionExecutionCount.Value;

            for (var i = 0; i < executionCount; i++)
                result.Value.Add(MeasureExecutionTime(result.Key.Action));
        }

        _data.Results = results
            .Select(i => new MethodExecutionResults(i.Key, i.Value))
            .ToList();

        return new ResultProcessor(this, _data);
    }

    public static TinyBenchmarkRunner Create(Action<string> writeMessage = null)
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
        // PrepareRun();
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

    public ITinyBenchmarkRunner WithRunCountLimits(int minFunctionExecutionCount, int? maxFunctionExecutionCount = null)
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

    public TinyBenchmarkRunner WithMaxRunExecutionTime(TimeSpan time)
    {
        _data.MaxRunExecutionTime = time;
        return this;
    }

    public TinyBenchmarkRunner WithoutRunExecutionTimeLimit()
    {
        _data.MaxRunExecutionTime = null;
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