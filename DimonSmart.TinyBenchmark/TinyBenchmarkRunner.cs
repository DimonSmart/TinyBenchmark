using System.Diagnostics;
using System.Reflection;
using DimonSmart.TinyBenchmark.Exporters;
using DimonSmart.TinyBenchmark.Utils;
using static DimonSmart.TinyBenchmark.AttributeUtility;

namespace DimonSmart.TinyBenchmark;

public class TinyBenchmarkRunner : ITinyBenchmarkRunner
{
    private readonly BenchmarkData _data = new();
    private Action<string>? _writeMessage;

    public ITinyBenchmarkRunner WithBatchSize(int batchSize = 5)
    {
        if (batchSize < 1)
           throw new ArgumentException("Batch size must be greater than or equal to 1.", nameof(batchSize));

        _data.BatchSize = batchSize;
        return this;
    }

    public ITinyBenchmarkRunner WithLogger(Action<string> writeMessage)
    {
        _writeMessage = writeMessage;
        return this;
    }

    public ITinyBenchmarkRunner WithResultSubfolders(bool resultSubfolders)
    {
        _data.ResultSubfolders = resultSubfolders;
        return this;
    }

    public ITinyBenchmarkRunner WithMemoryBenchmarking(bool benchmarkMemory = true)
    {
        _data.BenchmarkMemory = benchmarkMemory;
        return this;
    }

    public IResultProcessor Run(params Type[] types)
    {
        var resultFolderPath = Path.Combine(Directory.GetCurrentDirectory(), ExporterBaseClass.ResultsFolder);
        var folderUri = new Uri(resultFolderPath).AbsoluteUri;
        Log($"Result folder:{folderUri}");
        var methods = GetMethodExecutionInformation(types);
        var classesCount = methods.Select(m => m.ClassType).Distinct().Count();
        Log($"Run TinyBenchmark for:{classesCount} classes");
        var results = methods.ToDictionary(m => m, _ => new List<MethodExecutionMetrics>(1000000));

        GcFull();

        Log("1. Warming UP phase (time pre-calculation)");
        long measuredTotalTime = 0;
        if (_data.BenchmarkDurationLimit.HasValue)
        {
            WarmUpCalculation(results);
            measuredTotalTime = results.Sum(kvp => kvp.Value.CalculatePercentile(i => i.MethodMeasureTime, 50).Ticks);
        }

        Log("2. Measuring phase");

        Dictionary<Type, (MethodExecutionInformation Method, TimeSpan ExecutionTime)> overtimeExecutions = new();
        foreach (var method in methods)
        {
            Log(GetUserFriendlyMethodName(method));

            var methodTimeLimit = TimeSpan.Zero;

            if (_data.BenchmarkDurationLimit.HasValue)
            {
                var currentMethodTime = results[method].CalculatePercentile(i => i.MethodMeasureTime, 50).Ticks;
                var methodTimeLimitTicks = (long)((double)_data.BenchmarkDurationLimit.Value.Ticks * currentMethodTime / measuredTotalTime);
                methodTimeLimit = TimeSpan.FromTicks(methodTimeLimitTicks);
            }

            var (timeLimitReached, measureTime) = MeasureFunctionCycle(method, results, methodTimeLimit,
                _data.MinFunctionExecutionCount, _data.MaxFunctionExecutionCount, _data.BenchmarkDurationLimit, _data.BenchmarkMemory);

            if (timeLimitReached)
            {
                if (!overtimeExecutions.TryGetValue(method.ClassType, out var value) ||
                    value.ExecutionTime < measureTime)
                {
                    value = (method, measureTime);
                    overtimeExecutions[method.ClassType] = value;
                }
            }
        }

        LogOvertimeExecutions(overtimeExecutions);

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

    public ITinyBenchmarkRunner WithMinFunctionExecutionCount(int minFunctionExecutionCount)
    {
        if (minFunctionExecutionCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minFunctionExecutionCount), minFunctionExecutionCount,
                "Must be greater then 1");
        }

        _data.MinFunctionExecutionCount = minFunctionExecutionCount;
        return this;
    }

    public ITinyBenchmarkRunner WithMaxFunctionExecutionCount(int maxFunctionExecutionCount)
    {
        if (_data.MinFunctionExecutionCount > maxFunctionExecutionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFunctionExecutionCount), maxFunctionExecutionCount,
                $"Must be greater then {nameof(_data.MinFunctionExecutionCount)}");
        }

        _data.MaxFunctionExecutionCount = maxFunctionExecutionCount;
        return this;
    }

    private void LogOvertimeExecutions(
        Dictionary<Type, (MethodExecutionInformation Method, TimeSpan ExecutionTime)> overtimeExecutions)
    {
        if (!overtimeExecutions.Any())
        {
            return;
        }

        foreach (var (_, (method, executionTime)) in overtimeExecutions)

        {
            Log($"{GetUserFriendlyMethodName(method)} timeout exceeded. {executionTime} ms.");
        }

        Log("Please increase MaxRunExecutionTime parameter or decrease MinFunctionExecutionCount");
    }

    private static (bool TimeLimitReached, TimeSpan measureTime) MeasureFunctionCycle(MethodExecutionInformation method,
        IReadOnlyDictionary<MethodExecutionInformation, List<MethodExecutionMetrics>> results,
        TimeSpan methodTimeLimit,
        int minFunctionExecutionCount,
        int? maxFunctionExecutionCount,
        TimeSpan? timeLimit,
        bool benchmarkMemory)
    {
        var iteration = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            var executionMetrics = MeasureFunctionMetrics(method.Action, benchmarkMemory);
            results[method].Add(executionMetrics);
            iteration++;

            if (iteration >= minFunctionExecutionCount)
            {
                if (!timeLimit.HasValue || stopwatch.Elapsed > methodTimeLimit || iteration >= maxFunctionExecutionCount)
                {
                    break;
                }
            }
        }

        stopwatch.Stop();

        var timeLimitReached = !timeLimit.HasValue && stopwatch.Elapsed > methodTimeLimit;

        return (timeLimitReached, stopwatch.Elapsed);
    }


    private static string GetUserFriendlyMethodName(MethodExecutionInformation methodInfo)
    {
        return $"{methodInfo.ClassType.Name}.{methodInfo.MethodInfo.Name}({methodInfo.Parameter})";
    }

    private void WarmUpCalculation(Dictionary<MethodExecutionInformation, List<MethodExecutionMetrics>> results)
    {
        foreach (var result in results)
        {
            var (_, _) =
                MeasureFunctionCycle(result.Key, results, TimeSpan.Zero, _data.MinFunctionExecutionCount, null,
                    TimeSpan.MaxValue, _data.BenchmarkMemory);
        }
    }

    public static ITinyBenchmarkRunner Create()
    {
        return new TinyBenchmarkRunner();
    }

    private IReadOnlyCollection<MethodExecutionInformation> GetMethodExecutionInformation(IReadOnlyCollection<Type> types)
    {
        var executionInformation = new List<MethodExecutionInformation>();
        var classes = types.Any() ? types : GetClassesUnderTest();
        foreach (var classUnderTestType in classes)
        {
            var propertyInfo = FindClassUnderTestParameterProperty(classUnderTestType);
            var parameters = GetParametersFromAttribute(propertyInfo);
            var methods = GetMethodsWithTinyBenchmarkAttribute(classUnderTestType);
            var instance = Activator.CreateInstance(classUnderTestType) ??
                           throw new Exception("Can't create class instance");

            foreach (var method in methods)
            {
                executionInformation.AddRange(GetMethodExecutionInformation(parameters, method, instance,
                    classUnderTestType, _data.BatchSize));
            }
        }

        return executionInformation;
    }

    private static void GcFull()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForFullGCComplete();
        GC.Collect();
    }

    internal static MethodExecutionMetrics MeasureFunctionMetrics(Action action, bool benchmarkMemory)
    {
        var methodMeasureStopwatch = Stopwatch.StartNew();
        long memoryBefore = 0;
        long memoryAfter = 0;
        if (benchmarkMemory)
        {
            GC.Collect(0, GCCollectionMode.Forced, true, false);
            // memoryBefore = GC.GetTotalMemory(false);
            memoryBefore = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
        }

        var methodStopwatch = Stopwatch.StartNew();
        action();
        methodStopwatch.Stop();

        if (benchmarkMemory)
        {
            // memoryAfter = GC.GetTotalMemory(false);
            memoryAfter = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
        }
        methodMeasureStopwatch.Stop();
        return new MethodExecutionMetrics(
            PureMethodTime: methodStopwatch.Elapsed,
            MethodMeasureTime: methodMeasureStopwatch.Elapsed,
            MemoryUsed: Math.Max(memoryAfter - memoryBefore, 0));
    }

    internal static IEnumerable<MethodExecutionInformation> GetMethodExecutionInformation(
        object[]? parameters, MethodInfo methodInfo, object instance, Type classType, int batchSize)
    {
        var methodExecutionInformation = new List<MethodExecutionInformation>();
        if (parameters != null && methodInfo.GetParameters().Length > 0)
        {
            foreach (var parameterAttributeValue in parameters)
            {
                var methodExecutionInfo =
                    new MethodExecutionInformation(classType, methodInfo, parameterAttributeValue, Action);
                methodExecutionInformation.Add(methodExecutionInfo);
                continue;

                void Action()
                {
                    for (var i = 0; i < batchSize; i++) methodInfo.Invoke(instance, new[] { parameterAttributeValue });
                }
            }
        }
        else if (methodInfo.GetParameters().Length == 0)
        {
            var methodExecutionInfo = new MethodExecutionInformation(classType, methodInfo, null, Action);
            methodExecutionInformation.Add(methodExecutionInfo);

            void Action()
            {
                for (var i = 0; i < batchSize; i++) methodInfo.Invoke(instance, null);
            }
        }

        return methodExecutionInformation;
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