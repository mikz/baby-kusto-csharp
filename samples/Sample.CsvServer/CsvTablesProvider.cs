using BabyKusto.Core;
using BabyKusto.Server.Service;

namespace Sample.CsvServer;

/// <summary>
/// Provides table sources for BabyKusto.Server by loading multiple CSV files.
/// Each CSV file becomes a separate table with the table name derived from the file name.
/// </summary>
public class CsvTablesProvider : ITablesProvider
{
    private readonly Dictionary<string, ITableSource> _tables;

    /// <summary>
    /// Creates a new CSV tables provider by loading multiple CSV files
    /// </summary>
    /// <param name="csvFiles">Collection of paths to CSV files to load</param>
    /// <exception cref="InvalidOperationException">Thrown when no valid CSV tables could be loaded</exception>
    public CsvTablesProvider(IReadOnlyList<string> csvFiles)
    {
        _tables = [];

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
    }

    /// <summary>
    /// Gets all available tables loaded from CSV files
    /// </summary>
    /// <returns>List of table sources representing the loaded CSV files</returns>
    public List<ITableSource> GetTables() => [.. _tables.Values];
}