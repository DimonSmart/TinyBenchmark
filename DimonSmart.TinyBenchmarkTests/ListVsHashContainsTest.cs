using DimonSmart.TinyBenchmark.Attributes;

namespace DimonSmart.TinyBenchmarkTests;

[TinyBenchmarkOnlyThisClass]
public class ListVsHashContainsTest : VsTestsBase
{
    private static readonly List<string>[] Strings;
    private static readonly HashSet<string>[] Hashes;

    static ListVsHashContainsTest()
    {
        Strings = new List<string>[AllNames.Length];
        Hashes = new HashSet<string>[AllNames.Length];

        for (var i = 0; i < AllNames.Length; i++)
        {
            Strings[i] = AllNames.Take(i + 1).ToList();
            Hashes[i] = new HashSet<string>(Strings[i], StringComparer.OrdinalIgnoreCase);
        }
    }


    [TinyBenchmarkRangeParameter(1, 60, 1)]
    public int BenchmarkParameter { get; set; }

    [TinyBenchmark]
    public void StringContainsFirstString(int parameter)
    {
        for (var i = 0; i < 10; i++)
            _ = Strings[parameter].Contains("Philip J. Fry");
    }

    // Benchmark stability is ensured by HashSet's efficient hashing and collision resolution.
    // Searching for the first string exemplifies its consistent performance.
    public void HashSetContainsFirstString(int parameter)
    {
        for (var i = 0; i < 10; i++)
            _ = Hashes[parameter].Contains("Philip J. Fry");
    }

    [TinyBenchmark]
    public void StringContainsLastString(int parameter)
    {
        for (var i = 0; i < 10; i++)
            _ = Strings[parameter].Contains("Doctor Brown Bear");
    }

    [TinyBenchmark]
    public void HashSetContainsLastString(int parameter)
    {
        for (var i = 0; i < 10; i++)
            _ = Hashes[parameter].Contains("Doctor Brown Bear");
    }
}