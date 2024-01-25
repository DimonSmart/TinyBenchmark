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
        var methodExecutionInformation = GetMethodExecutionInformation();
        var classesCount = methodExecutionInformation.Select(m => m.ClassType).Distinct().Count();
        Log($"Run TinyBenchmark for:{classesCount} classes");
        var results = methodExecutionInformation
            .ToDictionary(m => m, v => new List<MethodExecutionNumbers>());
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
                // TODO: Not only method but full execution time
                .Select(t => (long)t.Select(i => i.MethodTime.Ticks).Average())
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
                var thisRunTicks = result.Value.CalculatePercentile(r => r.MethodTime, 50).Ticks;
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

    private void LogCurrentMethod(MethodExecutionInformation method)
    {
        Log($"{method.ClassType.Name}.{method.MethodInfo.Name}({method.Parameter})");
    }

    public static ITinyBenchmarkRunner Create(Action<string>? writeMessage = null)
    {
        return new TinyBenchmarkRunner(writeMessage);
    }

    private static IReadOnlyCollection<MethodExecutionInformation> GetMethodExecutionInformation()
    {
        var executionInformation = new List<MethodExecutionInformation>();
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
                executionInformation.AddRange(GetMethodExecutionInformation(parameters, method, instance, classUnderTestType));
            }
        }

        return executionInformation;
    }


    private static void PrepareRun()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForFullGCComplete();
        GC.Collect();
    }

    internal static MethodExecutionNumbers MeasureExecutionTime(Action action)
    {
        //PrepareRun();
        // GC.TryStartNoGCRegion(100 * 1024 * 1024);
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();
        // GC.EndNoGCRegion();
        var elapsed = stopwatch.Elapsed;
        // TODO: add GC time, mem etc if requested here
        return new MethodExecutionNumbers(elapsed);
    }

    internal static IEnumerable<MethodExecutionInformation> GetMethodExecutionInformation(
        object[]? parameters, MethodInfo methodInfo, object instance, Type classType)
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
                    methodInfo.Invoke(instance, new[] { parameterAttributeValue });
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
}