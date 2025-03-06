using BabyKusto.Core;
using BabyKusto.Core.Util;
using Kusto.Language.Symbols;

namespace BabyKusto.SampleCsvServer;

/// <summary>
/// Represents a CSV file as a table source for BabyKusto.
/// The first line of the CSV file must contain column definitions in format: name:type
/// Example: id:long,name:string,timestamp:datetime
/// </summary>
public class CsvTableSource : ITableSource
{
    private readonly string _filePath;
    private readonly (string name, ScalarSymbol type)[] _columnDefs;
    private readonly List<string[]> _rows = new();

    public TableSymbol Type { get; }

    public CsvTableSource(string filePath)
    {
        _filePath = filePath;
        
        // Read and parse the CSV file
        using var reader = new StreamReader(filePath);
        
        // Parse header for schema
        var header = reader.ReadLine();
        if (string.IsNullOrEmpty(header))
            throw new InvalidDataException($"CSV file {filePath} is empty or has no header");

        _columnDefs = ParseHeader(header);
        Type = new TableSymbol(
            Path.GetFileNameWithoutExtension(filePath),
            _columnDefs.Select(c => new ColumnSymbol(c.name, c.type)).ToArray()
        );
        
        // Read data rows without validation (validate in GetData())
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line)) continue;
            
            var values = line.Split(',')
                .Select(v => v.Trim())
                .Select(v => v.StartsWith("\"") && v.EndsWith("\"") ? v[1..^1] : v)
                .ToArray();
            
            _rows.Add(values);
        }
    }

    public IEnumerable<ITableChunk> GetData()
    {
        var builders = new List<ColumnBuilder>();

        // Create builders for each column
        foreach (var (_, type) in _columnDefs)
        {
            builders.Add(CreateBuilder(type));
        }

        // Add data to builders
        foreach (var row in _rows)
        {
            if (row.Length != _columnDefs.Length)
                throw new InvalidDataException($"CSV row has {row.Length} columns but schema defines {_columnDefs.Length} columns");

            for (var i = 0; i < row.Length; i++)
            {
                var value = row[i];
                var type = _columnDefs[i].type;
                builders[i].Add(ParseValue(value, type));
            }
        }

        yield return new TableChunk(this, builders.Select(b => b.ToColumn()).ToArray());
    }

    public IAsyncEnumerable<ITableChunk> GetDataAsync(CancellationToken cancellation = default)
    {
        throw new NotSupportedException();
    }

    private static (string name, ScalarSymbol type)[] ParseHeader(string header)
    {
        var columnDefs = header.Split(',');
        var result = new (string name, ScalarSymbol type)[columnDefs.Length];

        for (var i = 0; i < columnDefs.Length; i++)
        {
            var parts = columnDefs[i].Split(':');
            if (parts.Length != 2)
                throw new InvalidDataException($"Invalid column definition in CSV header: {columnDefs[i]}");

            var name = parts[0].Trim();
            var type = parts[1].Trim().ToLowerInvariant();

            result[i] = (name, MapKustoType(type));
        }

        return result;
    }

    private static object? ParseValue(string value, ScalarSymbol type)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        try
        {
            return type switch
            {
                { Name: "string" } => value,
                { Name: "long" } => long.Parse(value),
                { Name: "int" } => int.Parse(value),
                { Name: "real" or "double" } => double.Parse(value),
                { Name: "datetime" } => DateTime.Parse(value),
                { Name: "bool" } => bool.Parse(value),
                { Name: "timespan" } => TimeSpan.Parse(value),
                _ => throw new NotSupportedException($"Unsupported type: {type}")
            };
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Failed to parse value '{value}' as {type}: {ex.Message}");
        }
    }

    private static ScalarSymbol MapKustoType(string kustoType) => kustoType switch
    {
        "string" => ScalarTypes.String,
        "long" => ScalarTypes.Long,
        "int" => ScalarTypes.Int,
        "double" or "real" => ScalarTypes.Real,
        "datetime" => ScalarTypes.DateTime,
        "bool" => ScalarTypes.Bool,
        "timespan" => ScalarTypes.TimeSpan,
        _ => throw new NotSupportedException($"Unsupported Kusto type: {kustoType}")
    };

    private static ColumnBuilder CreateBuilder(ScalarSymbol type) => type switch
    {
        { Name: "string" } => new ColumnBuilder<string>(ScalarTypes.String),
        { Name: "long" } => new ColumnBuilder<long?>(ScalarTypes.Long),
        { Name: "int" } => new ColumnBuilder<int?>(ScalarTypes.Int),
        { Name: "real" or "double" } => new ColumnBuilder<double?>(ScalarTypes.Real),
        { Name: "datetime" } => new ColumnBuilder<DateTime?>(ScalarTypes.DateTime),
        { Name: "bool" } => new ColumnBuilder<bool?>(ScalarTypes.Bool),
        { Name: "timespan" } => new ColumnBuilder<TimeSpan?>(ScalarTypes.TimeSpan),
        _ => throw new NotSupportedException($"Unsupported type: {type}")
    };
}