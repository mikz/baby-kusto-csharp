using BabyKusto.Core;
using BabyKusto.Server.Service;

namespace BabyKusto.SampleCsvServer;

public class CsvTablesProvider : ITablesProvider
{
    private readonly List<ITableSource> _tables;

    public CsvTablesProvider(IReadOnlyList<string> csvFiles)
    {
        _tables = new List<ITableSource>();

        foreach (var file in csvFiles)
        {
            try
            {
                var table = new CsvTableSource(file);
                _tables.Add(table);
                Console.WriteLine($"Loaded CSV table '{table.Name}' with {table.Columns.Count} columns from {file}");
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

    public List<ITableSource> GetTables() => _tables;
}