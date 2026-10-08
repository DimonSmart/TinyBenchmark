using System.Globalization;
using System.Reflection;
using DimonSmart.TinyBenchmark.Attributes;
using DimonSmart.TinyBenchmark.Exporters;
using DimonSmart.TinyBenchmark.Utils;
using static DimonSmart.TinyBenchmark.AttributeUtility;

namespace DimonSmart.TinyBenchmark;

public class TinyBenchmarkRunner : ITinyBenchmarkRunner
{
    private static readonly TimeSpan AdaptiveFastBatchTarget = TimeSpan.FromMilliseconds(1);
    private const int MaximumAdaptiveCalibrationBatches = 6;
    private const int MaximumAdaptiveBatchSize = 1 << 20;
    private readonly BenchmarkData _data = new();
    private readonly IBenchmarkTimeSource _timeSource;
    private readonly IBenchmarkInvocationExecutor _invocationExecutor;
    private Action<string>? _writeMessage;

    public TinyBenchmarkRunner()
        : this(new StopwatchBenchmarkTimeSource(), new DirectBenchmarkInvocationExecutor())
    {
    }

    public TinyBenchmarkRunner(IBenchmarkTimeSource timeSource, IBenchmarkInvocationExecutor invocationExecutor)
    {
        _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        _invocationExecutor = invocationExecutor ?? throw new ArgumentNullException(nameof(invocationExecutor));
    }

    public ITinyBenchmarkRunner WithLogger(Action<string> writeMessage) { _writeMessage = writeMessage; return this; }
    public ITinyBenchmarkRunner WithResultSubfolders(bool resultSubfolders) { _data.ResultSubfolders = resultSubfolders; return this; }
    public ITinyBenchmarkRunner WithMemoryBenchmarking(bool benchmarkMemory = true) { _data.BenchmarkMemory = benchmarkMemory; return this; }
    public ITinyBenchmarkRunner WithRawSampleRetention(BenchmarkSampleRetentionMode retentionMode) { _data.SampleRetentionMode = retentionMode; return this; }
    public ITinyBenchmarkRunner WithMaxRunExecutionTime(TimeSpan time, int preRunCount)
    {
        if (time < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(time));
        if (preRunCount < 0) throw new ArgumentOutOfRangeException(nameof(preRunCount));
        _data.BenchmarkDurationLimit = time;
        _data.BenchmarkDurationLimitInitIterations = preRunCount;
        return this;
    }
    public ITinyBenchmarkRunner WithoutRunExecutionTimeLimit() { _data.BenchmarkDurationLimit = null; return this; }
    public ITinyBenchmarkRunner WithMinFunctionExecutionCount(int count)
    {
        if (count < 1 || _data.MaxFunctionExecutionCount is { } maximum && count > maximum)
            throw new ArgumentOutOfRangeException(nameof(count));
        _data.MinFunctionExecutionCount = count;
        return this;
    }
    public ITinyBenchmarkRunner WithMaxFunctionExecutionCount(int count)
    {
        if (count < 1 || count < _data.MinFunctionExecutionCount)
            throw new ArgumentOutOfRangeException(nameof(count));
        _data.MaxFunctionExecutionCount = count;
        _data.HasExplicitMaxFunctionExecutionCount = true;
        return this;
    }

    public IResultProcessor Run(params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        var runStart = Timestamp();
        var modern = GetModernCases(types);
        ValidateModernSampleLimits(modern);
        var legacy = GetLegacyCases(types);
        var legacyResults = ExecuteLegacy(legacy, runStart);
        _data.Results = legacyResults.Select(execution => new MethodExecutionResults(execution.Method, execution.Metrics)).ToList();
        var preparation = ElapsedSince(runStart);
        var modernExecution = ExecuteModern(modern, runStart);
        var cases = CreateLegacyResults(legacyResults).Concat(modernExecution.Cases).ToArray();
        var completed = cases.All(@case => @case.ExecutionStatus == BenchmarkExecutionStatus.Completed);
        var executionStatus = cases.Any(@case => @case.ExecutionStatus == BenchmarkExecutionStatus.RequiredExecutionOverrun)
            ? BenchmarkExecutionStatus.RequiredExecutionOverrun
            : completed ? BenchmarkExecutionStatus.Completed : BenchmarkExecutionStatus.BudgetExhausted;
        var elapsed = ElapsedSince(runStart);
        var budgetOverrun = BudgetOverrun(elapsed);
        var runResult = new BenchmarkRunResult(
            new EffectiveMeasurementSettings(_data.SampleRetentionMode ?? BenchmarkSampleRetentionMode.SummaryOnly, _data.BenchmarkDurationLimit, _data.BenchmarkMemory),
            cases, executionStatus,
            new BenchmarkPhaseTiming(preparation, modernExecution.Warmup, modernExecution.Calibration, modernExecution.Measurement),
            new BenchmarkRunDiagnostics(
                executionStatus == BenchmarkExecutionStatus.BudgetExhausted ? "Optional modern cases were incomplete because the run budget was exhausted." : null,
                _data.BenchmarkDurationLimit,
                budgetOverrun)
            {
                OverrunReason = executionStatus == BenchmarkExecutionStatus.RequiredExecutionOverrun
                    ? "A mandatory LongRunning quota completed after the run budget deadline."
                    : null
            });
        return new ResultProcessor(this, SnapshotData(_data), runResult);
    }

    private ModernExecution ExecuteModern(IReadOnlyCollection<ModernCase> cases, TimeSpan runStart)
    {
        var executions = cases.Select(@case => new CaseExecution(@case)).ToArray();
        var warmup = TimeSpan.Zero;
        var calibration = TimeSpan.Zero;
        var measurement = TimeSpan.Zero;

        foreach (var execution in executions.Where(item => item.Case.Strategy == BenchmarkStrategy.Regular))
        {
            for (var index = 0; index < execution.Case.WarmupCount; index++)
            {
                if (BudgetExceeded(runStart)) { execution.OptionalBudgetExhausted = true; break; }
                warmup += Invoke(execution.Case.Invocation);
                execution.PreparationInvocationCount++;
                execution.BudgetOverrun ??= BudgetOverrun(ElapsedSince(runStart));
            }
        }

        foreach (var execution in executions.Where(item => item.Case.Strategy == BenchmarkStrategy.Fast && item.Case.BatchSize is null))
            calibration += CalibrateFastBatch(execution, runStart);

        var optional = executions.Where(item => item.Case.Strategy is BenchmarkStrategy.Fast or BenchmarkStrategy.Regular).ToArray();

        // Give optional cases their first useful sample before mandatory work when the shared
        // budget still allows a sample to begin. Mandatory quotas still run in full afterwards.
        foreach (var execution in optional)
        {
            if (execution.OptionalBudgetExhausted || BudgetExceeded(runStart))
            {
                execution.OptionalBudgetExhausted = true;
                continue;
            }

            measurement += Measure(execution, runStart);
        }

        foreach (var execution in executions.Where(item => item.Case.Strategy == BenchmarkStrategy.LongRunning))
            while (execution.Samples.Count < execution.Case.RequestedSamples)
                measurement += Measure(execution, runStart);

        while (optional.Any(item => !item.OptionalBudgetExhausted && item.Samples.Count < item.Case.RequestedSamples))
        {
            foreach (var execution in optional)
            {
                if (execution.OptionalBudgetExhausted || execution.Samples.Count == execution.Case.RequestedSamples) continue;
                if (BudgetExceeded(runStart)) { execution.OptionalBudgetExhausted = true; continue; }
                measurement += Measure(execution, runStart);
            }
        }

        var results = executions.Select(CreateModernResult).ToArray();
        return new ModernExecution(results, warmup, calibration, measurement);
    }

    private void ValidateModernSampleLimits(IReadOnlyCollection<ModernCase> cases)
    {
        if (!_data.HasExplicitMaxFunctionExecutionCount || _data.MaxFunctionExecutionCount is not { } maximum)
            return;

        var exceedingCase = cases.FirstOrDefault(@case => @case.RequestedSamples > maximum);
        if (exceedingCase is not null)
        {
            throw new InvalidOperationException(
                $"Modern benchmark case '{exceedingCase.Id}' requires {exceedingCase.RequestedSamples} samples, " +
                $"which exceeds the explicitly configured legacy maximum of {maximum}.");
        }
    }

    private BenchmarkCaseResult CreateModernResult(CaseExecution execution)
    {
        var @case = execution.Case;
        var isMandatoryOverrun = @case.Strategy == BenchmarkStrategy.LongRunning && execution.MandatoryBudgetOverrun.GetValueOrDefault() > TimeSpan.Zero;
        var status = isMandatoryOverrun ? BenchmarkExecutionStatus.RequiredExecutionOverrun
            : execution.OptionalBudgetExhausted || execution.Samples.Count < @case.RequestedSamples ? BenchmarkExecutionStatus.BudgetExhausted
            : BenchmarkExecutionStatus.Completed;
        var incompleteReason = status == BenchmarkExecutionStatus.BudgetExhausted ? "The run budget was exhausted before this optional case completed." : null;
        var overrunReason = status == BenchmarkExecutionStatus.RequiredExecutionOverrun ? "The mandatory LongRunning quota completed after the run budget deadline." : null;
        return new BenchmarkCaseResult(@case.Id, PublicStrategy(@case.Strategy), @case.RequestedSamples, execution.Samples.Count,
            execution.Samples.Sum(sample => sample.OperationCount), execution.PreparationInvocationCount, status, Quality(execution.Samples.Count),
            _data.SampleRetentionMode ?? BenchmarkSampleRetentionMode.SummaryOnly, execution.Samples,
            new BenchmarkRunDiagnostics(incompleteReason, _data.BenchmarkDurationLimit, execution.BudgetOverrun)
            {
                OverrunReason = overrunReason
            }, @case.Identity);
    }

    private TimeSpan Measure(CaseExecution execution, TimeSpan runStart)
    {
        var @case = execution.Case;
        var before = TryGetAllocatedBytes();
        var started = Timestamp();
        for (var index = 0; index < execution.BatchSize; index++) _invocationExecutor.Invoke(@case.Invocation);
        var elapsed = ElapsedSince(started);
        var after = TryGetAllocatedBytes();
        long? allocatedBytes = before.HasValue && after.HasValue ? Math.Max(after.Value - before.Value, 0) : null;
        execution.Samples.Add(new BenchmarkSample(execution.Samples.Count, elapsed, execution.BatchSize, @case.Id,
            @case.Strategy == BenchmarkStrategy.LongRunning && execution.Samples.Count == 0, allocatedBytes));
        var budgetOverrun = BudgetOverrun(ElapsedSince(runStart));
        if (budgetOverrun.HasValue)
            execution.BudgetOverrun = budgetOverrun;
        if (@case.Strategy == BenchmarkStrategy.LongRunning)
            execution.MandatoryBudgetOverrun = budgetOverrun;
        return elapsed;
    }

    private TimeSpan CalibrateFastBatch(CaseExecution execution, TimeSpan runStart)
    {
        var batchSize = 1;
        var elapsedTotal = TimeSpan.Zero;
        for (var attempt = 0; attempt < MaximumAdaptiveCalibrationBatches; attempt++)
        {
            if (BudgetExceeded(runStart))
            {
                execution.OptionalBudgetExhausted = true;
                break;
            }

            var started = Timestamp();
            for (var index = 0; index < batchSize; index++) _invocationExecutor.Invoke(execution.Case.Invocation);
            var elapsed = ElapsedSince(started);
            elapsedTotal += elapsed;
            execution.PreparationInvocationCount += batchSize;
            execution.BatchSize = batchSize;
            execution.BudgetOverrun ??= BudgetOverrun(ElapsedSince(runStart));
            if (elapsed >= AdaptiveFastBatchTarget || batchSize >= MaximumAdaptiveBatchSize)
                break;
            batchSize = Math.Min(batchSize * 2, MaximumAdaptiveBatchSize);
        }
        return elapsedTotal;
    }

    private long? TryGetAllocatedBytes()
    {
        if (!_data.BenchmarkMemory) return null;
        try { return GC.GetAllocatedBytesForCurrentThread(); }
        catch (PlatformNotSupportedException) { return null; }
    }

    private TimeSpan Invoke(Action invocation)
    {
        var before = Timestamp();
        _invocationExecutor.Invoke(invocation);
        return ElapsedSince(before);
    }

    private IReadOnlyCollection<ModernCase> GetModernCases(IReadOnlyCollection<Type> requestedTypes)
    {
        var result = new List<ModernCase>();
        foreach (var type in requestedTypes.Any() ? requestedTypes : GetClassesUnderTest())
        {
            var values = GetParametersFromAttribute(FindClassUnderTestParameterProperty(type));
            var instance = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Can't create benchmark instance.");
            foreach (var declaration in GetBenchmarkMethodDeclarations(type).Where(item => item.Strategy.HasValue))
            {
                var parameters = declaration.Method.GetParameters().Length == 0 ? new object?[] { null } : values!;
                var fast = declaration.Method.GetCustomAttribute<TinyBenchmarkFastAttribute>(true);
                var regular = declaration.Method.GetCustomAttribute<TinyBenchmarkRegularAttribute>(true);
                foreach (var parameter in parameters)
                    result.Add(new ModernCase(CaseId(type, declaration.Method, parameter), CreateIdentity(type, declaration.Method, parameter), declaration.Strategy!.Value, declaration.RequestedSampleCount!.Value,
                        fast?.BatchSize, regular?.WarmupCount ?? 0,
                        PreparedBenchmarkInvocationFactory.Create(declaration.Method, instance, parameter)));
            }
        }
        return result;
    }

    private IReadOnlyCollection<MethodExecutionInformation> GetLegacyCases(IReadOnlyCollection<Type> requestedTypes)
    {
        var result = new List<MethodExecutionInformation>();
        foreach (var type in requestedTypes.Any() ? requestedTypes : GetClassesUnderTest())
        {
            var parameters = GetParametersFromAttribute(FindClassUnderTestParameterProperty(type));
            var instance = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Can't create benchmark instance.");
            foreach (var declaration in GetBenchmarkMethodDeclarations(type).Where(item => !item.Strategy.HasValue))
                result.AddRange(GetMethodExecutionInformation(parameters, declaration.Method, instance, type, _data.BatchSize));
        }
        return result;
    }

    private IReadOnlyList<LegacyExecution> ExecuteLegacy(IReadOnlyCollection<MethodExecutionInformation> methods, TimeSpan runStart)
    {
        var executions = methods.Select(method => new LegacyExecution(method)).ToArray();
        if (executions.Length == 0) return executions;
        GcFull();

        if (_data.BenchmarkDurationLimit.HasValue)
        {
            foreach (var execution in executions)
            {
                for (var index = 0; index < _data.BenchmarkDurationLimitInitIterations; index++)
                {
                    if (BudgetExceeded(runStart))
                    {
                        execution.BudgetExhausted = true;
                        break;
                    }
                    MeasureLegacy(execution.Method.Action);
                    execution.PreparationInvocationCount += OperationsPerSample(execution.Method);
                    UpdateLegacyBudgetOutcome(execution, runStart);
                }
            }
        }

        foreach (var execution in executions)
        {
            var maximum = _data.MaxFunctionExecutionCount ?? (_data.BenchmarkDurationLimit.HasValue ? int.MaxValue : _data.MinFunctionExecutionCount);
            while (execution.Metrics.Count < maximum)
            {
                if (BudgetExceeded(runStart))
                {
                    execution.BudgetExhausted = true;
                    break;
                }

                execution.Metrics.Add(MeasureLegacy(execution.Method.Action));
                UpdateLegacyBudgetOutcome(execution, runStart);

                if (!_data.BenchmarkDurationLimit.HasValue && execution.Metrics.Count >= _data.MinFunctionExecutionCount)
                    break;
                if (execution.BudgetExhausted)
                    break;
            }
        }

        return executions;
    }

    private void UpdateLegacyBudgetOutcome(LegacyExecution execution, TimeSpan runStart)
    {
        if (!_data.BenchmarkDurationLimit.HasValue) return;
        var elapsed = ElapsedSince(runStart);
        execution.BudgetExhausted |= elapsed >= _data.BenchmarkDurationLimit.Value;
        var overrun = BudgetOverrun(elapsed);
        if (overrun.HasValue) execution.BudgetOverrun = overrun;
    }

    private MethodExecutionMetrics MeasureLegacy(Action action)
    {
        var measurementStarted = Timestamp();
        var before = _data.BenchmarkMemory ? GC.GetAllocatedBytesForCurrentThread() : 0L;
        var invocationStarted = Timestamp();
        _invocationExecutor.Invoke(action);
        var invocationElapsed = ElapsedSince(invocationStarted);
        var memoryUsed = _data.BenchmarkMemory ? Math.Max(GC.GetAllocatedBytesForCurrentThread() - before, 0) : 0L;
        return new MethodExecutionMetrics(invocationElapsed, ElapsedSince(measurementStarted), memoryUsed);
    }

    private IEnumerable<BenchmarkCaseResult> CreateLegacyResults(IEnumerable<LegacyExecution> executions)
    {
        var retention = _data.SampleRetentionMode ?? BenchmarkSampleRetentionMode.AllRaw;
        foreach (var execution in executions)
        {
            var method = execution.Method;
            var operations = OperationsPerSample(method);
            var id = CaseId(method.ClassType, method.MethodInfo, method.Parameter);
            var samples = execution.Metrics.Select((metric, sequence) => new BenchmarkSample(sequence, metric.PureMethodTime, operations, id, false)).ToArray();
            var requested = _data.BenchmarkDurationLimit.HasValue ? _data.MaxFunctionExecutionCount ?? int.MaxValue : _data.MinFunctionExecutionCount;
            var status = execution.BudgetExhausted ? BenchmarkExecutionStatus.BudgetExhausted : BenchmarkExecutionStatus.Completed;
            var incompleteReason = status == BenchmarkExecutionStatus.BudgetExhausted
                ? "The run budget was exhausted before this legacy case completed."
                : null;
            yield return new BenchmarkCaseResult(id, BenchmarkMeasurementStrategy.Regular, requested, samples.Length, (long)samples.Length * operations, execution.PreparationInvocationCount,
                status, Quality(samples.Length), retention, samples, new BenchmarkRunDiagnostics(incompleteReason, _data.BenchmarkDurationLimit, execution.BudgetOverrun)
                {
                    OverrunReason = execution.BudgetOverrun.HasValue ? "A legacy sample completed after the run budget deadline." : null
                }, CreateIdentity(method.ClassType, method.MethodInfo, method.Parameter));
        }
    }

    private TimeSpan Timestamp() => _timeSource.GetTimestamp();
    private TimeSpan ElapsedSince(TimeSpan start)
    {
        var elapsed = Timestamp() - start;
        if (elapsed < TimeSpan.Zero)
            throw new InvalidOperationException("The benchmark time source must be monotonic.");
        return elapsed;
    }
    private bool BudgetExceeded(TimeSpan runStart) => _data.BenchmarkDurationLimit.HasValue && ElapsedSince(runStart) >= _data.BenchmarkDurationLimit.Value;
    private TimeSpan? BudgetOverrun(TimeSpan elapsed) => _data.BenchmarkDurationLimit.HasValue && elapsed > _data.BenchmarkDurationLimit.Value
        ? elapsed - _data.BenchmarkDurationLimit.Value
        : null;
    private static BenchmarkMeasurementStrategy PublicStrategy(BenchmarkStrategy strategy) => strategy switch { BenchmarkStrategy.Fast => BenchmarkMeasurementStrategy.Fast, BenchmarkStrategy.Regular => BenchmarkMeasurementStrategy.Regular, _ => BenchmarkMeasurementStrategy.LongRunning };
    private static BenchmarkStatisticalQualityStatus Quality(int count) => count switch { 0 => BenchmarkStatisticalQualityStatus.NoData, 1 => BenchmarkStatisticalQualityStatus.SingleSample, _ => BenchmarkStatisticalQualityStatus.Measured };
    private static string CaseId(Type type, MethodInfo method, object? parameter) => $"{type.FullName}.{method.Name}({Convert.ToString(parameter, CultureInfo.InvariantCulture) ?? string.Empty})";
    private static BenchmarkCaseIdentity CreateIdentity(Type type, MethodInfo method, object? parameter)
    {
        var parameterTypeNames = string.Join(",", method.GetParameters().Select(item => item.ParameterType.AssemblyQualifiedName));
        var methodSignature = $"{method.Name}({parameterTypeNames})";
        var parameterType = parameter?.GetType().AssemblyQualifiedName ?? "<null>";
        var parameterDisplay = Convert.ToString(parameter, CultureInfo.InvariantCulture) ?? "null";
        return new BenchmarkCaseIdentity(type.FullName ?? type.Name, method.Name, methodSignature,
            $"{parameterType}:{parameterDisplay}", parameterDisplay);
    }
    private static BenchmarkData SnapshotData(BenchmarkData data) => new() { BenchmarkDurationLimitInitIterations = data.BenchmarkDurationLimitInitIterations, BenchmarkDurationLimit = data.BenchmarkDurationLimit, MinFunctionExecutionCount = data.MinFunctionExecutionCount, MaxFunctionExecutionCount = data.MaxFunctionExecutionCount, HasExplicitMaxFunctionExecutionCount = data.HasExplicitMaxFunctionExecutionCount, BatchSize = data.BatchSize, ResultSubfolders = data.ResultSubfolders, BenchmarkMemory = data.BenchmarkMemory, SampleRetentionMode = data.SampleRetentionMode, Results = data.Results.Select(item => new MethodExecutionResults(item.Method, item.Numbers.ToList())).ToList() };

    private static void GcFull() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForFullGCComplete(); GC.Collect(); }
    private int OperationsPerSample(MethodExecutionInformation method) => method.Parameter is null ? 1 : _data.BatchSize;
    internal static IEnumerable<MethodExecutionInformation> GetMethodExecutionInformation(object[]? parameters, MethodInfo method, object instance, Type type, int batchSize)
    {
        if (parameters is null) { var invocation = PreparedBenchmarkInvocationFactory.Create(method, instance, null); yield return new MethodExecutionInformation(type, method, null, invocation); yield break; }
        foreach (var parameter in parameters) { var invocation = PreparedBenchmarkInvocationFactory.Create(method, instance, parameter); yield return new MethodExecutionInformation(type, method, parameter, () => { for (var i = 0; i < batchSize; i++) invocation(); }); }
    }
    public static ITinyBenchmarkRunner Create() => new TinyBenchmarkRunner();
    private sealed record ModernCase(string Id, BenchmarkCaseIdentity Identity, BenchmarkStrategy Strategy, int RequestedSamples, int? BatchSize, int WarmupCount, Action Invocation);
    private sealed class CaseExecution
    {
        public CaseExecution(ModernCase @case)
        {
            Case = @case;
            BatchSize = @case.BatchSize ?? 1;
        }
        public ModernCase Case { get; }
        public int BatchSize { get; set; }
        public List<BenchmarkSample> Samples { get; } = new();
        public long PreparationInvocationCount { get; set; }
        public bool OptionalBudgetExhausted { get; set; }
        public TimeSpan? BudgetOverrun { get; set; }
        public TimeSpan? MandatoryBudgetOverrun { get; set; }
    }
    private sealed class LegacyExecution
    {
        public LegacyExecution(MethodExecutionInformation method) => Method = method;
        public MethodExecutionInformation Method { get; }
        public List<MethodExecutionMetrics> Metrics { get; } = new();
        public long PreparationInvocationCount { get; set; }
        public bool BudgetExhausted { get; set; }
        public TimeSpan? BudgetOverrun { get; set; }
    }
    private sealed record ModernExecution(IReadOnlyList<BenchmarkCaseResult> Cases, TimeSpan Warmup, TimeSpan Calibration, TimeSpan Measurement);
}
