using System.Text;
using DimonSmart.TinyBenchmark.Attributes;

namespace DimonSmart.TinyBenchmarkTests;

public class StringConcatenationTest
{
    private static readonly string[] allNames =
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

    [TinyBenchmarkRangeParameter(0, 60, 5)]
    public int BenchmarkParameter { get; set; }

    [TinyBenchmark]
    public void StringPlus(int parameter)
    {
        var s = string.Empty;
        for (var i = 0; i < parameter; i++) s = s + allNames[i];
    }

    [TinyBenchmark]
    public void StringBuilder(int parameter)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < parameter; i++) sb.Append(allNames[i]);
        var s = sb.ToString();
    }
}