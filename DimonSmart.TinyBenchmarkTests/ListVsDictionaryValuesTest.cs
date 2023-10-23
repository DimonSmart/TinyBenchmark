using DimonSmart.TinyBenchmark.Attributes;

namespace DimonSmart.TinyBenchmarkTests;

public class ListVsDictionaryValuesTest
{
    private static string[] allNames =
    {
        "Philip J. Fry", "Turanga Leela", "Bender Bending Rodriguez", "Professor Hubert J. Farnsworth", "Amy Wong",
        "Hermes Conrad", "Zoidberg", "Nibbler", "Zapp Brannigan", "Kif Kroker",
        "Morbo", "Lrrr", "Mom", "Walter", "Larry", "Igner", "LaBarbara", "Scruffy", "Donbot", "Clamps",
        "Homer Simpson", "Marge Simpson", "Bart Simpson", "Lisa Simpson", "Maggie Simpson",
        "Ned Flanders", "Moe Szyslak", "Apu Nahasapeemapetilon", "Montgomery Burns", "Waylon Smithers",
        "Principal Seymour Skinner", "Chief Wiggum", "Krusty the Clown", "Milhouse Van Houten", "Ralph Wiggum",
        "Lenny Leonard", "Carl Carlson", "Nelson Muntz", "Sideshow Bob", "Kearney Zzyzwicz",
        "Peppa Pig", "George Pig", "Mummy Pig", "Daddy Pig", "Granny Pig",
        "Grandpa Pig", "Suzy Sheep", "Candy Cat", "Rebecca Rabbit", "Danny Dog",
        "Pedro Pony", "Zoe Zebra", "Emily Elephant", "Wendy Wolf", "Freddy Fox",
        "Mr. Elephant", "Madame Gazelle", "Miss Rabbit", "Mr. Potato", "Doctor Brown Bear"
    };

    private readonly List<string> _list;
    private readonly Dictionary<string, string> _dictionary;

    public ListVsDictionaryValuesTest()
    {
        _list = allNames.ToList();
        _dictionary = allNames.ToDictionary(k => k, v => v);
    }


    [TinyBenchmarkRangeParameter(0, 60, 5)] public int BenchmarkParameter { get; set; }

    [TinyBenchmark]
    public void List(int parameter)
    {
        var result = string.Empty;
        foreach (var s in _list)
        {
            result = result + s;
        }
    }

    [TinyBenchmark]
    public void Dictionary(int parameter)
    {
        var result = string.Empty;
        foreach (var s in _dictionary.Values)
        {
            result = result + s;
        }
    }
}