using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;

if (args.Length == 0 || args.Length != 2 || args[0] != "--filePath")
{
    Console.WriteLine("Provide the file path by using the '--filePath' argument.");
    return;
}

var filePath = args[1];
Console.WriteLine($"Provided file path: {filePath}");

if (!File.Exists(filePath))
{
    Console.WriteLine("File not found.");
    return;
}

var stopwatch = Stopwatch.StartNew();

var culture = CultureInfo.GetCultureInfo("en-US");
var calculations = new Dictionary<string, Calculation>();

// each line will be allocated to a string, which will pressure the GC
await foreach (var line in File.ReadLinesAsync(filePath, Encoding.UTF8, CancellationToken.None))
{
    var measurement = ParseLine(line);
    if (!calculations.ContainsKey(measurement.City))
    {
        calculations.Add(measurement.City, new Calculation
        {
            Count = 1,
            Min = measurement.Value,
            Mean = measurement.Value,
            Max = measurement.Value
        });
    }
    else
    {
        if (calculations.TryGetValue(measurement.City, out var calculation))
        {
            calculation.Count++;
            if (measurement.Value < calculation.Min)
                calculation.Min = measurement.Value;
            if (measurement.Value > calculation.Max)
                calculation.Max = measurement.Value;
            calculation.Mean = Math.Round((calculation.Mean + measurement.Value) / calculation.Count, digits: 1, mode: MidpointRounding.AwayFromZero);
        }
        else
        {
            throw new InvalidOperationException($"Key not found: {measurement.City}");
        }
    }
}

var sorted = calculations.ToImmutableSortedSet(new CalculationComparer(culture));
for (var i = 0; i < sorted.Count; i++)
{
    if (i == 0)
        Console.Write("{");

    Console.Write($"{sorted[i].Key}={sorted[i].Value}");

    if (i == sorted.Count - 1)
    {
        Console.WriteLine("}");
    }
    else
    {
        Console.Write(", ");
    }
}

Console.WriteLine($"Execution finished in {stopwatch.Elapsed}");

Measurement ParseLine(string line)
{
    var span = line.AsSpan();
    var index = span.IndexOf(';');

    // string allocation
    return new Measurement(span[..index].ToString(), double.Parse(span[(index + 1)..], culture));
}

internal readonly struct Measurement
{
    public readonly string City;

    public readonly double Value;

    public Measurement(string city, double value) : this()
    {
        City = city;
        Value = value;
    }
}

[DebuggerDisplay("{Min}/{Mean}/{Max}")]
internal class Calculation
{
    public int Count;

    public double Min;

    public double Mean;

    public double Max;

    public override string ToString()
    {
        return $"{Min}/{Mean}/{Max}";
    }
}

internal class CalculationComparer : IComparer<KeyValuePair<string, Calculation>>
{
    private readonly StringComparer _comparer;

    public CalculationComparer(CultureInfo cultureInfo)
    {
        _comparer = StringComparer.Create(cultureInfo, true);
    }

    public int Compare(KeyValuePair<string, Calculation> x, KeyValuePair<string, Calculation> y)
    {
        return _comparer.Compare(x.Key, y.Key);
    }
}
