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

    public ITinyBenchmarkRunner WithLogger(Action<string> writeMessage)
    {
        _writeMessage = writeMessage;
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
        var results = methods.ToDictionary(m => m, _ => new List<MethodExecutionNumbers>(1000000));

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
                _data.MinFunctionExecutionCount, _data.MaxFunctionExecutionCount, _data.BenchmarkDurationLimit);

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
        IReadOnlyDictionary<MethodExecutionInformation, List<MethodExecutionNumbers>> results,
        TimeSpan methodTimeLimit,
        int minFunctionExecutionCount,
        int? maxFunctionExecutionCount,
        TimeSpan? timeLimit)
    {
        var iteration = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            var executionTime = MeasureExecutionTime(method.Action);
            results[method].Add(executionTime);
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

    private void WarmUpCalculation(Dictionary<MethodExecutionInformation, List<MethodExecutionNumbers>> results)
    {
        foreach (var result in results)
        {
            var (_, _) =
                MeasureFunctionCycle(result.Key, results, TimeSpan.Zero, _data.MinFunctionExecutionCount, null,
                    TimeSpan.MaxValue);
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

    internal static MethodExecutionNumbers MeasureExecutionTime(Action action)
    {
        var methodMeasureStopwatch = Stopwatch.StartNew();
        //PrepareRun();
        // GC.TryStartNoGCRegion(100 * 1024 * 1024);
        var methodStopwatch = Stopwatch.StartNew();
        action();
        methodStopwatch.Stop();
        methodMeasureStopwatch.Stop();
        // GC.EndNoGCRegion();
        // TODO: add GC time, mem etc if requested here
        return new MethodExecutionNumbers(methodStopwatch.Elapsed, methodMeasureStopwatch.Elapsed);
    }

    internal static IEnumerable<MethodExecutionInformation> GetMethodExecutionInformation(
        object[]? parameters, MethodInfo methodInfo, object instance, Type classType, int batchSize)
    {
        var methodExecutionInformation = new List<MethodExecutionInformation>();
        if (parameters != null)
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
        else
        {
            methodExecutionInformation.Add(new MethodExecutionInformation(classType, methodInfo, null, Action));

            void Action()
            {
                methodInfo.Invoke(instance, null);
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