using BabyKusto.Core;
using BabyKusto.Server.Service;

namespace BabyKusto.SampleCsvServer;

public class CsvTablesProvider : ITablesProvider
{
    private readonly Dictionary<string, ITableSource> _tables;

    public CsvTablesProvider(IReadOnlyList<string> csvFiles)
    {
        _tables = new Dictionary<string, ITableSource>();

        foreach (var file in csvFiles)
        {
            try
            {
                var table = new CsvTableSource(file);
                _tables[table.Type.Name] = table; // Replace if exists
                Console.WriteLine($"Loaded CSV table '{table.Type.Name}' with {table.Type.Columns.Count} columns from {file}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load CSV file {file}: {ex.Message}");
            }
        }

        if (_tables.Count == 0)
        {
            throw new InvalidOperationException("No valid CSV tables were loaded");
        }
    }

    public List<ITableSource> GetTables() => _tables.Values.ToList();
}