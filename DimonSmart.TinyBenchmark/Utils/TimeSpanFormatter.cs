namespace DimonSmart.TinyBenchmark.Utils;

public static class TimeSpanFormatter
{
    public static string FormatTimeSpan(this TimeSpan timeSpan)
    {
        var days = (int)timeSpan.TotalDays;
        var hours = (int)(timeSpan.TotalHours % 24);
        var minutes = (int)(timeSpan.TotalMinutes % 60);
        var seconds = (int)(timeSpan.TotalSeconds % 60);
        var milliseconds = (int)(timeSpan.TotalMilliseconds % 1000);
        var nanoseconds = (int)timeSpan.TotalNanoseconds;

        var result = new List<string>();
        var segmentsAdded = 0;

        if (days > 0)
        {
            result.Add($"{days} day{(days == 1 ? "" : "s")}");
            segmentsAdded++;
        }

        if (hours > 0)
        {
            result.Add($"{hours} hour{(hours == 1 ? "" : "s")}");
            segmentsAdded++;
        }

        if (minutes > 0 && segmentsAdded < 2)
        {
            result.Add($"{minutes} minute{(minutes == 1 ? "" : "s")}");
            segmentsAdded++;
        }

        if (seconds > 0 && segmentsAdded < 2)
        {
            result.Add($"{seconds} second{(seconds == 1 ? "" : "s")}");
            segmentsAdded++;
        }

        if (milliseconds > 0 && segmentsAdded < 2)
        {
            result.Add($"{milliseconds} millisecond{(milliseconds == 1 ? "" : "s")}");
            segmentsAdded++;
        }

        if (nanoseconds > 0 && segmentsAdded < 2)
        {
            result.Add($"{nanoseconds} nanosecond{(nanoseconds == 1 ? "" : "s")}");
        }

        return string.Join(" ", result);
    }
}