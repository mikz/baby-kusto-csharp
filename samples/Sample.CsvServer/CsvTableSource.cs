using System.Data;
using BabyKusto.Core;
using BabyKusto.Core.DataSource;

namespace BabyKusto.SampleCsvServer;

/// <summary>
/// Represents a CSV file as a table source for BabyKusto.
/// The first line of the CSV file must contain column definitions in format: name:type
/// Example: id:long,name:string,timestamp:datetime
/// </summary>
public class CsvTableSource : ITableSource
{
    private readonly string _filePath;
    private readonly string _tableName;
    private readonly Column[] _columns;
    private readonly List<object?[]> _rows;

    public CsvTableSource(string filePath)
    {
        _filePath = filePath;
        _tableName = Path.GetFileNameWithoutExtension(filePath);
        
        // Read and parse the CSV file
        using var reader = new StreamReader(filePath);
        
        // Parse header for schema
        var header = reader.ReadLine();
        if (string.IsNullOrEmpty(header))
            throw new InvalidDataException($"CSV file {filePath} is empty or has no header");

        _columns = ParseHeader(header);
        _rows = new List<object?[]>();
        
        // Read data rows
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line)) continue;
            
            var values = ParseDataRow(line);
            _rows.Add(values);
        }
    }

    public string Name => _tableName;

    public IReadOnlyList<Column> Columns => _columns;

    public IEnumerable<ITableChunk> GetData()
    {
        yield return new TableChunk(_columns, _rows);
    }

    private Column[] ParseHeader(string header)
    {
        var columnDefs = header.Split(',');
        var columns = new Column[columnDefs.Length];

        for (var i = 0; i < columnDefs.Length; i++)
        {
            var parts = columnDefs[i].Split(':');
            if (parts.Length != 2)
                throw new InvalidDataException($"Invalid column definition in CSV header: {columnDefs[i]}");

            var name = parts[0].Trim();
            var type = parts[1].Trim().ToLowerInvariant();

            columns[i] = new Column(name, MapKustoType(type));
        }

        return columns;
    }

    private object?[] ParseDataRow(string line)
    {
        var values = line.Split(',');
        if (values.Length != _columns.Length)
            throw new InvalidDataException($"CSV row has {values.Length} columns but schema defines {_columns.Length} columns");

        var result = new object?[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i].Trim();
            if (string.IsNullOrEmpty(value))
            {
                result[i] = null;
                continue;
            }

            // Remove quotes if present
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value[1..^1];

            result[i] = ParseValue(value, _columns[i].Type);
        }

        return result;
    }

    private static object? ParseValue(string value, Type type)
    {
        try
        {
            if (type == typeof(string)) return value;
            if (type == typeof(long)) return long.Parse(value);
            if (type == typeof(int)) return int.Parse(value);
            if (type == typeof(double)) return double.Parse(value);
            if (type == typeof(DateTime)) return DateTime.Parse(value);
            if (type == typeof(bool)) return bool.Parse(value);
            if (type == typeof(TimeSpan)) return TimeSpan.Parse(value);

            throw new NotSupportedException($"Unsupported type: {type}");
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Failed to parse value '{value}' as {type}: {ex.Message}");
        }
    }

    private static Type MapKustoType(string kustoType)
    {
        return kustoType switch
        {
            "string" => typeof(string),
            "long" => typeof(long),
            "int" => typeof(int),
            "double" => typeof(double),
            "datetime" => typeof(DateTime),
            "bool" => typeof(bool),
            "timespan" => typeof(TimeSpan),
            _ => throw new NotSupportedException($"Unsupported Kusto type: {kustoType}")
        };
    }
}