using System.Diagnostics;
using System.Reflection;
using DimonSmart.TinyBenchmark.Exporters;
using static DimonSmart.TinyBenchmark.AttributeUtility;

namespace DimonSmart.TinyBenchmark;

public class TinyBenchmarkRunner : ITinyBenchmarkRunner
{
    private BenchmarkData _data = new();

    private TinyBenchmarkRunner()
    {
    }

    public ITinyBenchmarkRunner Reset()
    {
        _data = new BenchmarkData();
        return this;
    }

    public IResultProcessor Run()
    {
        var methodExecutionInfos = GetMethodExecutionInfos();

        foreach (var methodExecutionInfo in methodExecutionInfos)
        {
            var times = MeasureAndRunAction(methodExecutionInfo.Action, _data.MaxFunctionRunTImeMilliseconds);
            var result = new MethodExecutionResults(methodExecutionInfo, times);
            _data.Results.Add(result);
        }

        return new ResultProcessor(this, _data);
    }


    public static TinyBenchmarkRunner Create()
    {
        return new TinyBenchmarkRunner();
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

    public List<TimeSpan> MeasureAndRunAction(Action action, int maxTotalMilliseconds)
    {
        var firstRunTime = MeasureExecutionTime(action);
        var iterations = (int)(maxTotalMilliseconds / firstRunTime.TotalMilliseconds);

        iterations = Math.Max(iterations, _data.MinFunctionExecutionCount);
        iterations = Math.Min(iterations, _data.MaxFunctionExecutionCount);
        var executionTimes = new List<TimeSpan>(iterations + 1);
        executionTimes.Add(firstRunTime);

        for (var i = 0; i < iterations; i++) executionTimes.Add(MeasureExecutionTime(action));

        return executionTimes;
    }

    private static void PrepareRun()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForFullGCComplete();
        GC.Collect();
    }

    public static TimeSpan MeasureExecutionTime(Action action)
    {
        PrepareRun();
        GC.TryStartNoGCRegion(100 * 1024 * 1024);
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        GC.EndNoGCRegion();
        return stopwatch.Elapsed;
    }

    private static IEnumerable<MethodExecutionInfo> GetMethodExecutionInfos(
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

    public ITinyBenchmarkRunner WithRunCountLimits(int minFunctionExecutionCount, int maxFunctionExecutionCount)
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

    public ITinyBenchmarkRunner With50PercentileAsResult()
    {
        _data.GetResult = TimeSpanUtils.Get50Percentile;
        return this;
    }

    public ITinyBenchmarkRunner WithMinimumTimeAsResult()
    {
        _data.GetResult = TimeSpanUtils.GetMinResult;
        return this;
    }
}