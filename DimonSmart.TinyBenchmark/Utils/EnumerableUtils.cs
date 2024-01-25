namespace DimonSmart.TinyBenchmark.Utils;

public static class EnumerableUtils
{
    public static IEnumerable<T> LimitProportionally<T>(this IReadOnlyCollection<T> source, int limitLines = int.MaxValue)
    {
        if (limitLines <= 0)
        {
            throw new ArgumentException("Limit must be positive.");
        }

        if (source == null)
        {
            throw new ArgumentNullException(nameof(source), "Source cannot be null.");
        }

        if (limitLines >= source.Count)
        {
            return source;
        }

        var limitedData = new List<T>();
        var step = (double)source.Count / limitLines;
        var accumulatedError = 0.0;

        for (var i = 0; i < source.Count;)
        {
            limitedData.Add(source.ElementAt(i));
            accumulatedError += step;
            var actualStep = (int)accumulatedError;
            accumulatedError -= actualStep;
            i += actualStep;
        }

        return limitedData;
    }
}